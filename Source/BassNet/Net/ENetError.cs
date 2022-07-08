namespace Bass.Net
{
    /// <summary>
    /// Network Library Error Codes
    /// </summary>
    public enum ENetError : int
    {
        Success = 0,							// 정상 처리

        // Packet
        Packet_InvalidDataSize,					// 패킷 데이터 사이즈 범위를 벗어남
        Packet_DataSizeIsTooLarge,				// 패킷에 담으려는 데이터가 너무 큼
        Packet_DataIsNull,						// 패킷에 담을 데이터가 없음

        // ListenerSocket
        Listener_AlreadyLietening,				// 이미 Listener로 접속 받고있는중
        Listener_InvalidPortRange,				// 잘못된 포트 번호 (1~65535 사이가 아님)
        Listener_SocketListenFail,				// 이미 사용중인 포트 등의 이유로 Listen 실패

        // Session
        Session_SendPacketIsNull,				// 보낼 패킷이 null

        // Server
        Server_ListenSocketAlreadyListening,	// 이미 서버소켓이 동작중
		Server_SendPacketIsNull,				// 보냇 패킷이 null
		Server_SessionNotExists,				// 해당 클라이언트가 존재하지 않음
		Server_SessionIsFull,					// 수용 가능한 클라이언트 수를 넘어섬

		// ClientSocket
		Client_SocketAlreadyUsed,				// 이미 소켓이 사용중
		Client_InvalidPortRange,                // 잘못된 포트 번호 (1~65535 사이가 아님)
		Client_InvalidHost,						// 접속 주소가 잘못됨



		Max,
    }
}
