using Bass.Internal;
using Bass.Net.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bass.Net.Client
{
    public class UnityClientSocket
    {
        private Socket mSocket = null;
        private SocketAsyncEventArgs mRecvEvent = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs mSendEvent = new SocketAsyncEventArgs();

        private byte[] mRecvBuffer = new byte[Define.SocketBufferSize];
        private byte[] mSendBuffer = new byte[Define.SocketBufferSize];

        private object mSendLock = new object();    // for SendQueue
        private Queue<Packet> mSendQueue = new Queue<Packet>();

        private object mRecvLock = new object();
        private Queue<Packet> mRecvQueue = new Queue<Packet>();

        private PacketResolver mPacketResolver = new PacketResolver();


        public EClientSocketState State { get; private set; } = EClientSocketState.None;
        public bool mReceiveStarted = false;



        public UnityClientSocket()
        {
            mRecvEvent.SetBuffer(mRecvBuffer, 0, Define.SocketBufferSize);
            mRecvEvent.Completed += new EventHandler<SocketAsyncEventArgs>(_RecvCompleted);

            mSendEvent.SetBuffer(mSendBuffer, 0, Define.SocketBufferSize);
            mSendEvent.Completed += new EventHandler<SocketAsyncEventArgs>(_SendCompleted);
        }

        /// <summary>
        /// 서버에 연결되었는지 여부를 확인합니다.
        /// </summary>
        /// <returns>서버 연결 여부</returns>
        public bool IsConnected()
        {
            return mSocket?.Connected ?? false;
        }



        /// <summary>
        /// 서버에 접속합니다.
        /// </summary>
        /// <param name="host">접속할 서버 주소</param>
        /// <param name="port">접속할 서버 포트</param>
        /// <returns>서버 접속 처리 결과</returns>
        public ENetError Connect(string host, int port)
        {
            switch (State)
            {
                case EClientSocketState.Connecting:
                case EClientSocketState.Connected:
                    return ENetError.Client_SocketAlreadyUsed;
                default:
                    break;
            }

            State = EClientSocketState.Connecting;

            if (port <= ushort.MinValue
                || port > ushort.MaxValue)
            {
                State = EClientSocketState.ConnectFailed;
                return ENetError.Client_InvalidPortRange;
            }

            IPAddress address;

            try
            {
                var ip = Dns.GetHostAddresses(host);
                if (ip.Length == 0)
                {
                    State = EClientSocketState.ConnectFailed;
                    return ENetError.Client_InvalidHost;
                }
                address = ip[0];
            }
            catch (Exception e)
            {
                ExceptionLogger.Trace(e);
                State = EClientSocketState.ConnectFailed;
                return ENetError.Client_InvalidHost;
            }

            IPEndPoint endPoint = new IPEndPoint(address, port);
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs _connectEvent = new SocketAsyncEventArgs();
            _connectEvent.RemoteEndPoint = endPoint;
            _connectEvent.Completed += new EventHandler<SocketAsyncEventArgs>(_ConnectCompleted);

            if (false == mSocket.ConnectAsync(_connectEvent))
                _ConnectCompleted(null, _connectEvent);

            return ENetError.Success;
        }

        /// <summary>
        /// 서버와의 접속을 해제합니다.
        /// </summary>
        public void Disconnect(bool bNotify = true)
        {
            if (null != mSocket)
            {
                try
                {
                    mSocket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception e)
                {
                    ExceptionLogger.Trace(e);
                }
                finally
                {
                    if (bNotify)
                        State = EClientSocketState.Disconnected;
                    mSocket.Close();
                    mSocket = null;
                }
            }

            lock (mSendLock)
                mSendQueue.Clear();

            lock (mRecvLock)
                mRecvQueue.Clear();
        }

        /// <summary>
        /// 서버로 메시지를 전송합니다.
        /// </summary>
        /// <param name="msg">전송할 메시지</param>
        public void SendPacket(Packet msg)
        {
            if (null == msg)
                return;

            if (State != EClientSocketState.Connected)
                return;

            lock (mSendLock)
            {
                bool bWorking = mSendQueue.Count > 0;
                mSendQueue.Enqueue(msg);

                if (false == bWorking)
                    _StartSendAsync();
            }
        }

        #region Private functions

        private void _StartSendAsync()
        {
            if (null == mSocket)
                return;

            lock (mSendLock)
            {
                // mSendQueue 에 데이터가 있어야 이 함수를 호출한다.
                // 하지만 만일을 대비하여 체크.
                if (mSendQueue.Count == 0)
                    return;

                Packet msg = mSendQueue.Peek();
                int nOffset = mSendEvent.Offset;
                int nPacketSize = msg.Size;
                mSendEvent.SetBuffer(nOffset, nPacketSize);
                Buffer.BlockCopy(msg.Binary, 0, mSendEvent.Buffer, mSendEvent.Offset, msg.Size);
            }

            if (false == mSocket.SendAsync(mSendEvent))
                _SendProcess(mSendEvent);

        }

        private void _ConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Disconnect(false);
                State = EClientSocketState.ConnectFailed;
                return;
            }


            if (null != mSocket)
            {
                mReceiveStarted = false;
                State = EClientSocketState.Connected;

            }
            else
            {
                State = EClientSocketState.ConnectFailed;
            }
        }


        private void _RecvCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
                _RecvProcess(e, 0);
        }

        private void _RecvProcess(SocketAsyncEventArgs e, int pendingFalseCount)
        {
            if (e.BytesTransferred <= 0
                || e.SocketError != SocketError.Success)
            {
                Disconnect();
                return;
            }

            _OnReceive(e.Buffer, e.Offset, e.BytesTransferred);

            if (null != mSocket)
            {
                try
                {
                    if (false == mSocket.ReceiveAsync(e))
                        _RecvProcess(e, ++pendingFalseCount);
                }
                catch (ObjectDisposedException er)
                {
                    ExceptionLogger.Trace(er);  // 이미 소켓이 끊어짐
                }
                catch (Exception er)
                {
                    ExceptionLogger.Trace(er);
                    Disconnect();
                }
            }

        }

        private void _OnReceive(byte[] buffer, int offset, int bytesTransferred)
        {
            mPacketResolver.ResolveProcess(ref buffer, offset, bytesTransferred, _MessageResolveComplete);
        }

        private void _MessageResolveComplete(byte[] buffer, int bufferSize)
        {
            if (null == buffer)
                return;

            Packet recvPacket = new Packet(buffer, bufferSize);
            lock (mRecvLock)
                mRecvQueue.Enqueue(recvPacket);
        }


        private void _SendCompleted(object sender, SocketAsyncEventArgs e) => _SendProcess(e);

        private void _SendProcess(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0
                || e.SocketError != SocketError.Success)
                return;

            lock (mSendLock)
            {
                if (mSendQueue.Count <= 0)
                    return;

                int nSendSize = mSendQueue.Peek().Size;
                if (e.BytesTransferred != nSendSize)
                    return;

                mSendQueue.Dequeue();
                if (mSendQueue.Count > 0)
                    _StartSendAsync();
            }
        }

        #endregion

        public bool StartReceive()
        {
            if (State != EClientSocketState.Connected)
                return false;

            if (mReceiveStarted)
                return false;
            mReceiveStarted = true;

            if (false == mSocket.ReceiveAsync(mRecvEvent))
                _RecvCompleted(null, mRecvEvent);

            return true;
        }

        public List<Packet> GetReceivePackets()
        {
            if (State != EClientSocketState.Connected)
                return null;

            const int RECV_PACKET_AT_ONCE = 200;

            lock (mRecvLock)
            {
                if (mRecvQueue.Count == 0)
                    return null;

                List<Packet> retList = new List<Packet>();
                while (mRecvQueue.Count > 0)
                {
                    retList.Add(mRecvQueue.Dequeue());
                    if (retList.Count >= RECV_PACKET_AT_ONCE)
                        return retList;
                }

                return retList;
            }
        }



        public void Reset()
        {
            Disconnect(false);
            State = EClientSocketState.None;
        }

        public void Update()
        {
            _UpdateState();
        }



        private void _UpdateState()
        {
            switch (State)
            {
                case EClientSocketState.Connected:
                    {
                        if (null == mSocket)
                        {
                            State = EClientSocketState.Disconnected;
                        }
                        else
                        {
                            if (!mSocket.Connected)
                                State = EClientSocketState.Disconnected;
                        }
                    }
                    break;
                default:
                    break;
            }


        }


    }
}
