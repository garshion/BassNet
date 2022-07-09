using Bass.Net;
using Bass.Net.Server;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Test.Server.User;

namespace Test.Server
{
	internal class SimpleChatServer
	{
		private ServerSocket mServer = new ServerSocket();
		private bool mIsStart = false;
		
		public SimpleChatServer()
		{
			UserManager.GetInstance().SendPacketCallback = mServer.SendToClient;
		}

		public void Start()
		{
			if (true == mIsStart)
				return;

			mServer.OnConnected = _OnConnected;
			mServer.OnDisconnected = _OnDisonnected;
			mServer.OnPacketReceived = _PacketProcess;

			mServer.StartServer(SampleInfo.ConnectionPort);
			mIsStart = true;

			Thread th = new Thread(_Run);
			th.Start();
		}

		private bool _PacketProcess(Packet msg)
		{
			if (null == msg)
				return false;

			EProtocol protocol = (EProtocol)(msg.Protocol);
			switch (protocol)
			{
				case EProtocol.CS_PING:
					{
						mServer.SendToClient(msg.SenderIndex, new Packet((int)EProtocol.SC_PONG));
					}
					break;
				case EProtocol.CS_LOGIN_REQ:
					{

						if(true == UserManager.GetInstance().IsConnectedUser(msg.SenderIndex))
						{
							Console.WriteLine(string.Format("Client({0}) Already Login.", msg.SenderIndex));
						}
						else
						{
							CSLoginReq req;
							if (false == _GetMessage(msg, out req))
							{
								//mServer.DisconnectClient(msg.SenderIndex);
								return false;
							}

							SCLoginRes res = new SCLoginRes();
							res.NickName = req.NickName;
							UserManager.GetInstance().AddUser(msg.SenderIndex, req.NickName);
							res.LoginResult = true;

							return _SendPacket(msg.SenderIndex, EProtocol.SC_LOGIN_RES, res);
						}
					}
					break;
				case EProtocol.CS_CHAT_REQ:
					{
						if(false == UserManager.GetInstance().IsConnectedUser(msg.SenderIndex))
						{
							Console.WriteLine(string.Format("Client({0}) Not Login.", msg.SenderIndex));
							mServer.DisconnectClient(msg.SenderIndex);
							return false;
						}

						
						CSChatReq req;
						if (false == _GetMessage(msg, out req))
						{
							mServer.DisconnectClient(msg.SenderIndex);
							return false;
						}

						SCChatRes res = new SCChatRes();
						res.ChatMessage = req.ChatMessage;
						res.NickName = UserManager.GetInstance().GetNickname(msg.SenderIndex);

						var packet = _MakePacket(EProtocol.SC_CHAT_RES, res);
						UserManager.GetInstance().SendPacketAll(packet);
					}
					break;
				default:
					{
						Console.WriteLine("Invalid Packet!! Protocol ({0}) Size ({1}) SenderIndex ({2})",
							msg.Protocol, msg.Size, msg.SenderIndex);
					}
					break;
			}

			return true;
		}

		private bool _OnDisonnected(int sessionIndex, string peerIP)
		{
			string nick = UserManager.GetInstance().GetNickname(sessionIndex);
			if (string.IsNullOrEmpty(nick))
			{
				Console.WriteLine(string.Format("Client ({0}) [{1}] Disconnected.", sessionIndex, peerIP));
			}
			else
			{
				Console.WriteLine(string.Format("User [{0}] ({1}) [{2}] Disconnected.", nick, sessionIndex, peerIP));
				UserManager.GetInstance().RemoveUser(sessionIndex);
			}

			return true;
		}

		private bool _OnConnected(int sessionIndex, string peerIP)
		{
			Console.Write(string.Format("Client ({0}) [{1}] Connected.", sessionIndex, peerIP));
			return true;
		}

		private void _Run()
		{
			while(true)
			{
				Thread.Sleep(1000);
			}
		}




		// Helper Func
		private bool _GetMessage<T>(Packet msg, out T o_val)
		{
			o_val = default(T);
			if (null == msg)
				return false;

			using (MemoryStream ms = new MemoryStream(msg.Binary, Define.PacketDataOffset, msg.DataSize))
			{
				BinaryFormatter bf = new BinaryFormatter();
				try
				{
					o_val = (T)bf.Deserialize(ms);
				}
				catch (Exception)
				{
					return false;
				}
			}

			return true;
		}


		private Packet _MakePacket<T>(EProtocol protocol, T msg)
		{
			Packet packet = new Packet((int)protocol);

			if(null != msg)
			{
				using (MemoryStream ms = new MemoryStream(packet.Binary, Define.PacketDataOffset, Define.MaxPacketDataBinaryLength))
				{
					BinaryFormatter bf = new BinaryFormatter();
					try
					{
						bf.Serialize(ms, msg);
						packet.Size = Define.PacketHeaderLength + (int)ms.Position;
					}
					catch (Exception)
					{
						return null;
					}
				}
			}

			return packet;
		}

		private bool _SendPacket<T>(int sessionIndex, EProtocol protocol, T msg)
		{
			if (null == msg)
				return false;

			Packet packet = _MakePacket(protocol, msg);
			if (null == packet)
				return false;

			using (MemoryStream ms = new MemoryStream(packet.Binary, Define.PacketDataOffset, Define.MaxPacketDataBinaryLength))
			{
				BinaryFormatter bf = new BinaryFormatter();
				try
				{
					bf.Serialize(ms, msg);
					packet.Size = Define.PacketHeaderLength + (int)ms.Position;
					mServer.SendToClient(sessionIndex, packet);
				}
				catch (Exception)
				{
					return false;
				}
			}

			return true;
		}


	}
}
