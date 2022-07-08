using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Bass.Internal;
using Bass.Net.Server.Internal;

namespace Bass.Net.Server
{
	public class ServerSocket
	{
		/// <summary>
		/// 패킷 수신시 호출
		/// </summary>
		public PacketHandler OnPacketReceived { get; set; }

		/// <summary>
		/// 클라이언트 접속시 호출
		/// </summary>
		public ConnectEventHandler OnConnected { get; set; }

		/// <summary>
		/// 클라이언트 접속 해제시 호출
		/// </summary>
		public ConnectEventHandler OnDisconnected { get; set; }

		/// <summary>
		/// 네트워크 통계
		/// </summary>
		public NetworkStatistic Statistics { get; private set; } = new NetworkStatistic();




		#region Private Member

		private ListenerSocket mListenSocket = null;
		private Dictionary<int, Session> mSessionList = new Dictionary<int, Session>();

		// 환경설정
		private int mSessionTimeout = Define.DefaultSessionTimeoutMS;
		private int mSessionIndexCounter = 0;

		private Queue<Session> mSessionPool = new Queue<Session>();
		private object mSessionLock = new object();	// for SessionPool

		List<int> mDisconnectList = new List<int>();    // 세션 정리시 사용하는 임시변수


		ENetError mLastError = ENetError.Success;

		#endregion


		public ServerSocket()
		{
		}


		/// <summary>
		/// 서버
		/// </summary>
		/// <param name="port"></param>
		/// <param name="maxConnectionCount"></param>
		/// <param name="backlog"></param>
		/// <returns></returns>
		public ENetError StartServer(int port, int maxConnectionCount = Define.DefaultSessionCount, int backlog = ListenerSocket.DefaultBacklogSize)
		{
			if (null != mListenSocket)
				return ENetError.Server_ListenSocketAlreadyListening;

			if (maxConnectionCount <= 0)
				maxConnectionCount = Define.DefaultSessionCount;

			lock (mSessionLock)
			{
				for (int i = 0; i < maxConnectionCount; i++)
				{
					Session token = new Session();
					token.RecvEventArgs.Completed += _RecvCompleted;
					token.SendEventArgs.Completed += _SendCompleted;
					mSessionPool.Enqueue(token);
				}
			}

			Statistics.Reset();
			mSessionIndexCounter = 0;

			mListenSocket = new ListenerSocket();
			mListenSocket.SocketConnectedCallback += _OnSessionConnected;

			mLastError = mListenSocket.Listen(port, backlog);

			if (mLastError != ENetError.Success)
				Destroy();

			return mLastError;
		}

		/// <summary>
		/// 서버 가동을 중지하고 리소스를 정리합니다.
		/// </summary>
		public void Destroy()
		{
			for (int i = 0; i < mSessionList.Count; i++)
			{
				Session ct = mSessionList.ElementAt(i).Value;
				ct?.Dispose();
			}
			mSessionList.Clear();

			lock (mSessionLock)
			{
				while (mSessionPool.Count > 0)
					mSessionPool.Dequeue().Dispose();
			}
			mListenSocket?.Destroy();
			mListenSocket = null;

		}

		/// <summary>
		/// 무응답 클라이언트의 접속 유지시간을 설정합니다.
		/// </summary>
		/// <param name="nTimeMS">설정할 접속 유지 시간 (밀리초)</param>
		public void SetSessionTimeout(int nTimeMS)
		{
			mSessionTimeout = Math.Max(nTimeMS, Define.MinSessionTimeoutMS);
		}

		/// <summary>
		/// 특정 클라이언트와의 연결을 종료합니다.
		/// </summary>
		/// <param name="sessionIndex">종료할 클라이언트 번호</param>
		public void DisconnectClient(int sessionIndex)
		{
			if (false == mSessionList.ContainsKey(sessionIndex))
			{
				_SetLastError(ENetError.Server_SessionNotExists);
				return;
			}
			_ClientDisconnect(mSessionList[sessionIndex]);
		}


		/// <summary>
		/// 오랫동안 패킷 응답이 없던 클라이언트들과의 접속을 종료합니다.
		/// </summary>
		public void ArrangeClients()
		{
			for (int i = 0; i < mSessionList.Count; i++)
			{
				Session session = mSessionList.ElementAt(i).Value;
				int nCheckTime = session.GetLastRecvTimePassed();

				if (nCheckTime > mSessionTimeout)
					mDisconnectList.Add(session.SessionIndex);
			}

			for (int i = 0; i < mDisconnectList.Count; i++)
				DisconnectClient(mDisconnectList[i]);

			mDisconnectList.Clear();
		}

		/// <summary>
		/// 접속한 클라이언트 전부에게 패킷을 전송합니다.
		/// </summary>
		/// <param name="msg">보낼 패킷</param>
		/// <returns>패킷 전송 성공 여부</returns>
		public bool SendToAll(Packet msg)
		{
			if (null == msg)
				return _SetLastError(ENetError.Server_SendPacketIsNull);

			int nSuccessCount = 0;

			for (int i = 0; i < mSessionList.Count; i++)
			{
				Session session = mSessionList.ElementAt(i).Value;
				if (null == session)
					continue;

				if (ENetError.Success == session.SendPacket(msg))
				{
					Statistics.Send(msg.Size);
					nSuccessCount++;
				}
			}

			return nSuccessCount > 0;
		}

		/// <summary>
		/// 특정 클라이언트로 패킷을 전송합니다.
		/// </summary>
		/// <param name="sessionIndex">보낼 클라이언트 번호</param>
		/// <param name="msg">보낼 패킷</param>
		/// <returns>패킷 전송 성공 여부</returns>
		public bool SendToClient(int sessionIndex, Packet msg)
		{
			if (null == msg)
				return _SetLastError(ENetError.Server_SendPacketIsNull);

			if (false == mSessionList.ContainsKey(sessionIndex))
				return _SetLastError(ENetError.Server_SessionNotExists);

			if (false == _SetLastError(mSessionList[sessionIndex].SendPacket(msg)))
				return false;

			Statistics.Send(msg.Size);
			return true;
		}

		public ENetError GetLastError() => mLastError;

		public void ClearLastError()
		{
			mLastError = ENetError.Success;
		}


		#region Private

		private void _SendCompleted(object sender, SocketAsyncEventArgs e)
		{
			Session token = e.UserToken as Session;
			token?.SendProcess(e);
		}

		private void _RecvCompleted(object sender, SocketAsyncEventArgs e)
		{
			if (e.LastOperation == SocketAsyncOperation.Receive)
				_RecvProcess(e);
		}

		private void _OnSessionConnected(Socket cs, object token)
		{
			if (null == cs)
				return;

			Session client;
			lock (mSessionLock)
			{
				if (0 == mSessionPool.Count)
				{
					_SetLastError(ENetError.Server_SessionIsFull);	// MAX Connections
					cs.Close();
					return;
				}

				client = mSessionPool.Dequeue();
			}

			Statistics.Connect();

			client.SessionIndex = Interlocked.Increment(ref mSessionIndexCounter);
			client.SendPacketCallback += SendToClient;
			client.SendPacketAllCallback += SendToAll;
			client.PacketProcessCallback += _RecvPacket;

			client.SetClientSocket(cs);

			mSessionList.Add(client.SessionIndex, client);
			OnConnected?.Invoke(client.SessionIndex, client.ClientIPAddress);

			_StartReceive(cs, client.RecvEventArgs);
		}

		private bool _RecvPacket(Packet msg)
		{
			if (null == msg)
				return false;

			Statistics.Recv(msg.Size);
			OnPacketReceived?.Invoke(msg);
			return true;
		}


		private void _StartReceive(Socket cs, SocketAsyncEventArgs recvArgs)
		{
			if (false == cs.ReceiveAsync(recvArgs))
				_RecvProcess(recvArgs);
		}

		private void _RecvProcess(SocketAsyncEventArgs e, int pendingFalseCount = 0)
		{
			// pending 무한루프 방지
			if (pendingFalseCount >= 5)
			{
				return;
			}

			Session session = e.UserToken as Session;

			if (null == session)
			{
				_SetLastError(ENetError.Server_SessionNotExists);
				return;
			}

			if (e.BytesTransferred <= 0
				|| e.SocketError != SocketError.Success)
			{
				_ClientDisconnect(session);
				return;
			}

			session.OnReceive(e.Buffer, e.Offset, e.BytesTransferred);

			try
			{
				if (false == session.ClientSocket.ReceiveAsync(e))
					_RecvProcess(e, ++pendingFalseCount);
			}
			catch (ObjectDisposedException er)
			{
				ExceptionLogger.Trace(er);  // 이미 소켓이 끊어짐
			}
			catch (Exception er)
			{
				ExceptionLogger.Trace(er);
				_ClientDisconnect(session);
			}
		}

		private void _ClientDisconnect(Session session)
		{
			if (null == session)
				return;

			int removeIndex = session.SessionIndex;

			OnDisconnected?.Invoke(session.SessionIndex, session.ClientIPAddress);

			session.Disconnect();
			session.PacketProcessCallback = null;

			// 중복 호출 제거
			if (false == mSessionList.Remove(removeIndex))
				return;

			session.OnDisconnected();
			lock (mSessionLock)
				mSessionPool.Enqueue(session);

			Statistics.Disconnect();
		}

		private bool _SetLastError(ENetError err)
		{
			mLastError = err;
			return mLastError == ENetError.Success;
		}


		#endregion

	}
}
