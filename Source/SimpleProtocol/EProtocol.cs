using System;

namespace Test
{
	public class SampleInfo
	{
		public const string ConnectionHost = "127.0.0.1";
		public const int ConnectionPort = 20210;
	}

	public enum EProtocol : int
	{
		None,

		CS_PING,			// Header Only
		SC_PONG,			// Header Only

		CS_LOGIN_REQ,
		SC_LOGIN_RES,

		CS_CHAT_REQ,
		SC_CHAT_RES,
	}


	[Serializable]
	public class CSLoginReq
	{
		public string NickName = "";
	}

	[Serializable]
	public class SCLoginRes
	{
		public string NickName = "";
		public bool LoginResult = false;
	}

	[Serializable]
	public class CSChatReq
	{
		public string ChatMessage = "";
	}

	[Serializable]
	public class SCChatRes
	{
		public string NickName = "";
		public string ChatMessage = "";
	}
}
