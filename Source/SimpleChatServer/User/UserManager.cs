using System.Collections.Generic;
using Bass.Net;

namespace Test.Server.User
{
	public class UserManager
	{
		private static UserManager mInstance = new UserManager();
		private Dictionary<int, UserData> mUserList = new Dictionary<int, UserData>();

		public delegate bool SendPacketHandler(int socketIndex, Packet msg);

		public SendPacketHandler SendPacketCallback { get; set; } = null;


		private UserManager()
		{
		}

		public static UserManager GetInstance() => mInstance;

		public bool IsConnectedUser(int socketIndex) => mUserList.ContainsKey(socketIndex);

		public bool AddUser(int socketIndex, string nickName)
		{
			if (true == IsConnectedUser(socketIndex))
				return false;

			mUserList.Add(socketIndex, new UserData() { SocketIndex = socketIndex, UserName = nickName });
			return true;
		}

		public void RemoveUser(int socketIndex)
		{
			mUserList.Remove(socketIndex);
		}

		public string GetNickname(int socketIndex)
		{
			if (false == IsConnectedUser(socketIndex))
				return @"";

			return mUserList[socketIndex].UserName;
		}

		public void SendPacketAll(Packet msg)
		{
			if (null == SendPacketCallback)
				return;

			foreach(var it in mUserList)
			{
				SendPacketCallback(it.Key, msg);
			}
		}


	}
}
