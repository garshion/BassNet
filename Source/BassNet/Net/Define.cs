
namespace Bass.Net
{
    public class Define
    {
        // Packet
        public const int MaxPacketBinaryLength = 8000;

        public const int PacketProtocolLength = sizeof(int);
		public const int PacketOptionLength = sizeof(ushort);
		public const int PacketSizeLength = sizeof(short);
		public const int PacketHeaderLength = PacketProtocolLength + PacketOptionLength + PacketSizeLength;

        public const int PacketProtocolOffset = 0;
		public const int PacketOptionOffset = PacketProtocolOffset + PacketProtocolLength;
        public const int PacketSizeOffset = PacketOptionOffset + PacketOptionLength;
        public const int PacketDataOffset = PacketSizeOffset + PacketSizeLength;

        public const int MaxPacketDataBinaryLength = MaxPacketBinaryLength - PacketHeaderLength;

        // ClientSocket
        public const int SocketBufferSize = MaxPacketBinaryLength * 2;


        // Server
        public const int DefaultSessionCount = 2000;        // 1 Listener의 기본 최대 동접수
        public const int DefaultSessionTimeoutMS = 60000;   // 기본 세션 유지 시간 (ms)
        public const int MinSessionTimeoutMS = 10000;       // 세션 유지 최소 시간 (ms)

    }

    public delegate bool PacketHandler(Packet msg);
    public delegate bool ConnectEventHandler(int SocketIndex, string peerIP);
    public delegate void SocketEventHandler();

}
