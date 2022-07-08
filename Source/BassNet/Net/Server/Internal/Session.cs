using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Bass.Internal;
using Bass.Net.Internal;

namespace Bass.Net.Server.Internal
{
	internal delegate bool SendPacketHandler(int SocketIndex, Packet msg);

	internal class Session : IDisposable
    {
        public Socket ClientSocket { get; private set; } = null;
        public SocketAsyncEventArgs RecvEventArgs { get; private set; } = new SocketAsyncEventArgs();
        public SocketAsyncEventArgs SendEventArgs { get; private set; } = new SocketAsyncEventArgs();

        public int SessionIndex { get; set; } = 0;
        public string ClientIPAddress { get; private set; } = "";

        public SendPacketHandler SendPacketCallback { get; set; } = null;
        public PacketHandler SendPacketAllCallback { get; set; } = null;
        public PacketHandler PacketProcessCallback { get; set; } = null;



        private PacketResolver mPacketResolver = new PacketResolver();
        private Queue<Packet> mSendPacketQueue = new Queue<Packet>();
        private object mSendLock = new object();

        private DateTime mLastRecvTime;
        private object mSocketLock = new object();

        private byte[] mRecvBuffer = new byte[Define.SocketBufferSize];
        private byte[] mSendBuffer = new byte[Define.SocketBufferSize];


        public Session()
        {
            RecvEventArgs.SetBuffer(mRecvBuffer, 0, Define.SocketBufferSize);
            SendEventArgs.SetBuffer(mSendBuffer, 0, Define.SocketBufferSize);

            RecvEventArgs.UserToken = this;
            SendEventArgs.UserToken = this;

            Reset();
        }


        // 재사용 가능하게.
        public void Reset()
        {
            mSendPacketQueue.Clear();

            SendPacketAllCallback = null;
            SendPacketCallback = null;
            PacketProcessCallback = null;

            lock (mSocketLock)
            {
                ClientSocket = null;
                ClientIPAddress = "";
                SessionIndex = 0;
            }
            mLastRecvTime = DateTime.Now;
        }


        public void SetClientSocket(Socket socket)
        {
            if (null == socket)
                return;

            lock (mSocketLock)
            {
                ClientSocket = socket;
                ClientIPAddress = socket.RemoteEndPoint.ToString();
            }
            mLastRecvTime = DateTime.Now;
        }

        public ENetError SendPacket(Packet msg)
        {
            if (null == msg)
                return ENetError.Session_SendPacketIsNull;

            lock (mSendLock)
            {
                int nCurrentCount = mSendPacketQueue.Count;
                mSendPacketQueue.Enqueue(msg);

                if (nCurrentCount <= 0)
                    _StartSendAsync();
            }
            return ENetError.Success;
        }

        public bool IsConnected()
        {
            lock (mSocketLock)
				return ClientSocket?.Connected ?? false;
        }

        public void Disconnect()
        {
            lock (mSocketLock)
            {
                if (null != ClientSocket)
                {
					try
					{
						ClientSocket.Shutdown(SocketShutdown.Send);
					}
					catch (Exception e)
					{
						ExceptionLogger.Trace(e);
					}
					finally
					{
						ClientSocket.Close();
					}
                }
            }
            Reset();
        }

        public void SendToClient(int sessionIndex, Packet msg)
        {
            if (null == msg)
                return;

            lock (mSocketLock)
                SendPacketCallback?.Invoke(sessionIndex, msg);
        }

        public void SendToAll(Packet msg)
        {
            if (null == msg)
                return;

            lock (mSocketLock)
                SendPacketAllCallback?.Invoke(msg);
        }

        private void _StartSendAsync()
        {
            if (null == mSendLock)
                return;

            if (null == SendEventArgs)
                return;

            lock (mSendLock)
            {
                if (mSendPacketQueue.Count <= 0)
                    return;

                Packet msg;
                try
                {
                    msg = mSendPacketQueue.Peek();
                }
                catch (InvalidOperationException e)
                {
                    ExceptionLogger.Trace(e);
                    return;
                }

                SendEventArgs.SetBuffer(SendEventArgs.Offset, msg.Size);
                try
                {
                    Buffer.BlockCopy(msg.Binary, 0, SendEventArgs.Buffer, SendEventArgs.Offset, msg.Size);
                }
                catch (Exception)
                {
                    // 복사가 잘못되면 패킷 데이터가 이상 있는 것이다.
                    mSendPacketQueue.Dequeue();
                    if (0 < mSendPacketQueue.Count)
                        _StartSendAsync();
                    return;
                }

                if (false == ClientSocket.SendAsync(SendEventArgs))
                    SendProcess(SendEventArgs);
            }
        }

        public void SendProcess(SocketAsyncEventArgs e)
        {
            if (null == e)
                return;

            if (e.BytesTransferred <= 0
                || e.SocketError != SocketError.Success)
                return;

            if (null == mSendLock)
                return;

            lock (mSendLock)
            {
                if (0 >= mSendPacketQueue.Count)
                    return;

                int nSendSize = mSendPacketQueue.Peek().Size;
                if (e.BytesTransferred != nSendSize)
                    return;

                mSendPacketQueue.Dequeue();

                if (0 < mSendPacketQueue.Count)
                    _StartSendAsync();
            }
        }

        public int GetLastRecvTimePassed()
        {
            return (int)(DateTime.Now - mLastRecvTime).TotalMilliseconds;
        }

        public void OnReceive(byte[] buffer, int offset, int transfered)
        {
            mPacketResolver.ResolveProcess(ref buffer, offset, transfered, _MessageResolveComplete);
        }

		public void OnDisconnected() => Reset();

        private void _MessageResolveComplete(byte[] buffer, int bufferSize)
        {
            if (null == buffer
                || null == PacketProcessCallback)
                return;

            Packet recvPacket = new Packet(buffer, bufferSize);
            recvPacket.SenderIndex = SessionIndex;

            PacketProcessCallback(recvPacket);

            mLastRecvTime = DateTime.Now;
        }

        public void Dispose()
        {
            lock (mSendLock)
            {
                mSendPacketQueue?.Clear();
                mSendPacketQueue = null;
            }

            mSendLock = null;
            mSocketLock = null;
            mPacketResolver = null;
            SendPacketAllCallback = null;
            SendPacketCallback = null;
            PacketProcessCallback = null;
            ClientSocket = null;

            ClientIPAddress = "";
            SessionIndex = 0;
        }
    }
}
