
namespace Bass.Net
{
    public class Define
    {
        // ClientSocket
        public const int SocketBufferSize = Packet.MAX_PACKET_BINARY_SIZE * 2;


        // Server
        public const int DefaultSessionCount = 2000;        // 1 Listener의 기본 최대 동접수
        public const int DefaultSessionTimeoutMS = 60000;   // 기본 세션 유지 시간 (ms)
        public const int MinSessionTimeoutMS = 10000;       // 세션 유지 최소 시간 (ms)

    }

    public delegate bool PacketHandler(Packet msg);
    public delegate bool ConnectEventHandler(int SocketIndex, string peerIP);
    public delegate void SocketEventHandler();

}
