using Bass.Net;
using Bass.Net.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test.Client
{
	public partial class frmMain : Form
	{

		private ClientSocket mSocket = new ClientSocket();

		private bool bConnected = false;
		private bool bLogined = false;
		private string myNick = "";

		public frmMain()
		{
			InitializeComponent();
			mSocket.OnConnectedCallback += _OnConnected;
			mSocket.OnDisconnectedCallback += _OnDisconnected;
			mSocket.OnPacketReceivedCallback += _OnPacketProcess;

			CheckForIllegalCrossThreadCalls = false;

		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			listChat.Items.Clear();

		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			if (true == bConnected)
				return;

			mSocket.Connect(SampleInfo.ConnectionHost, SampleInfo.ConnectionPort);

		}

		private void button1_Click(object sender, EventArgs e)
		{
			mSocket.Disconnect();

		}

		private void btnLogin_Click(object sender, EventArgs e)
		{
			if (true == bLogined)
				return;

			CSLoginReq req = new CSLoginReq();
			req.NickName = txtNick.Text.Trim();
			if (req.NickName.Length == 0)
			{
				_AddMessage("*** 닉네임을 입력하세요 ***");
				return;
			}

			_SendPacket(EProtocol.CS_LOGIN_REQ, req);

		}

		private void btnSend_Click(object sender, EventArgs e)
		{
			if (false == bLogined)
				return;

			CSChatReq req = new CSChatReq();
			req.ChatMessage = txtChat.Text.Trim();

			if (true == string.IsNullOrWhiteSpace(req.ChatMessage))
				return;

			_SendPacket(EProtocol.CS_CHAT_REQ, req);
			txtChat.Text = "";

		}





		private void _AddMessage(string msg)
		{
			listChat.Items.Add(msg);
			listChat.SelectedIndex = listChat.Items.Count - 1;
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

		private bool _SendPacket<T>(EProtocol protocol, T msg)
		{
			if (null == msg)
				return false;

			if (false == bConnected)
				return false;

			Packet packet = new Packet((int)protocol);
			using (MemoryStream ms = new MemoryStream(packet.Binary, Define.PacketDataOffset, Define.MaxPacketDataBinaryLength))
			{
				BinaryFormatter bf = new BinaryFormatter();
				try
				{
					bf.Serialize(ms, msg);
					packet.Size = Define.PacketHeaderLength + (int)ms.Position;
					mSocket.SendPacket(packet);
				}
				catch (Exception)
				{
					return false;
				}
			}

			return true;
		}

		private bool _OnPacketProcess(Packet packet)
		{
			if (null == packet)
				return false;

			EProtocol protocol = (EProtocol)packet.Protocol;
			switch (protocol)
			{
				case EProtocol.SC_LOGIN_RES:
					{
						SCLoginRes res;
						if (true == _GetMessage(packet, out res))
						{
							if (res.LoginResult)
							{
								_AddMessage(string.Format("== 다음으로 로그인됨 [{0}] ==", res.NickName));
								myNick = res.NickName;
								bLogined = true;
								txtNick.Enabled = false;

							}
							else
							{
								_AddMessage(string.Format("== 같은 닉네임이 존재 [{0}] ==", res.NickName));
							}
						}
					}
					break;
				case EProtocol.SC_CHAT_RES:
					{
						SCChatRes res;
						if (true == _GetMessage(packet, out res))
						{
							if (true == myNick.Equals(res.NickName))
							{
								_AddMessage(string.Format("> {0} : {1}", res.NickName, res.ChatMessage));
							}
							else
							{
								_AddMessage(string.Format("{0} : {1}", res.NickName, res.ChatMessage));
							}
						}
					}
					break;
				default:
					return false;
			}
			return true;
		}

		private void _OnDisconnected()
		{
			bConnected = false;
			bLogined = false;
			txtNick.Enabled = true;

			btnConnect.Enabled = true;
			_AddMessage("*** 접속이 끊어졌습니다. ***");
		}

		private void _OnConnected()
		{
			bConnected = true;
			btnConnect.Enabled = false;
			_AddMessage("*** 서버에 접속되었습니다. ***");
		}

	}
}
