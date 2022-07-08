
namespace Bass.Net
{
    public class Define
    {
        // Packet
        public const int MaxPacketBinaryLength = 8000;

        public const int PacketProtocolLength = sizeof(int);
        public const int PacketSizeLength = sizeof(int);
        public const int PacketHeaderLength = PacketProtocolLength + PacketSizeLength;

        public const int PacketProtocolOffset = 0;
        public const int PacketSizeOffset = PacketProtocolLength;
        public const int PacketDataOffset = PacketHeaderLength;

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
