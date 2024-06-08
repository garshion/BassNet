namespace Bass.Net.Client
{
    public enum EClientSocketState
    {
        None = 0,               // 초기화 생타

        Connecting,             // 접속중
        ConnectFailed,          // 접속 실패
        Connected,              // 접속됨
        Disconnected,           // 연결 끊김

        Max,
    }
}
