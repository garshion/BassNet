using Bass.Internal;
using System;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bass.Net.Server.Internal
{
	internal delegate void SocketConnectedHandler(Socket so, object token);

	internal class ListenerSocket
    {
		// Listener
		internal const int DefaultBacklogSize = 2;

		private SocketAsyncEventArgs mAcceptArgs = new SocketAsyncEventArgs();
        private Socket mListenSocket = null;
        private AutoResetEvent mAcceptFlowEvent = new AutoResetEvent(false);

        private Thread mListenThread = null;
        private bool mListening = false;

        public SocketConnectedHandler SocketConnectedCallback { get; set; } = null;

        public ListenerSocket()
        {
            mAcceptArgs.Completed += _OnAcceptCompleted;
        }

        public ENetError Listen(int port, int backlog = DefaultBacklogSize)
        {
            if (null != mListenSocket)
                return ENetError.Listener_AlreadyLietening;

            if (port <= ushort.MinValue
                || port >= ushort.MaxValue)
                return ENetError.Listener_InvalidPortRange;

            if (backlog <= 0)
                backlog = DefaultBacklogSize;

            mListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

            try
            {
                mListenSocket.Bind(endPoint);
                mListenSocket.Listen(backlog);

                mListening = true;

                if (null == mListenThread)
                    mListenThread = new Thread(_DoListen);

                mListenThread.IsBackground = true;
                mListenThread.Start();
            }
            catch (Exception)
            {
                Destroy();
                return ENetError.Listener_SocketListenFail;
            }

            return ENetError.Success;
        }

        private void _DoListen()
        {
            mAcceptFlowEvent.Reset();
            while (true == mListening)
            {
                if (null == mListenSocket)
                {
                    mListening = false;
                    continue;
                }

                mAcceptArgs.AcceptSocket = null;

                try
                {
                    if (false == mListenSocket.AcceptAsync(mAcceptArgs))
                        _OnAcceptCompleted(null, mAcceptArgs);
                }
                catch (Exception)
                {
                    continue;
                }
                mAcceptFlowEvent.WaitOne();
            }
            Destroy();
        }

        private void _OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket so = e.AcceptSocket;
                mAcceptFlowEvent.Set();
                SocketConnectedCallback?.Invoke(so, e.UserToken);
            }
            else
            {
                mAcceptFlowEvent.Set();
            }
        }

        public void Destroy()
        {
            mListening = false;

            if (null != mListenThread)
            {
                try
                {
                    mListenThread?.Abort();
                }
                catch (Exception e)
                {
                    ExceptionLogger.Trace(e);
                }
                finally
                {
                    mListenThread = null;
                }
            }

            if (null != mListenSocket)
            {
                try
                {
                    mListenSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    ExceptionLogger.Trace(e);
                }
                finally
                {
                    mListenSocket.Close();
                    mListenSocket = null;
                }
            }

            mAcceptFlowEvent.Reset();
            SocketConnectedCallback = null;
        }


    }
}
