using System;

namespace Bass.Net.Server
{
    /// <summary>
    /// 네트워크 통계를 얻어오기 위한 클래스.
    /// </summary>
    public class NetworkStatistic
    {
        // Connection
        public int TotalConnection { get; private set; } = 0;				// 누적 연결수
        public int CurrentConnection { get; private set; } = 0;				// 현재 연결수
        public int MaxConnection { get; private set; } = 0;					// 최대 동시 연결수

        // RecvCounter
        public int TotalRecvCount { get; private set; } = 0;				// 총 패킷 수신수
        public double RecvPacketPerSecond { get; private set; } = 0.0;		// 초당 패킷 수신수
        public long TotalRecvBytes { get; private set; } = 0;				// 총 패킷 수신 바이트
        public long RecvBytesPerSecond { get; private set; } = 0;			// 초당 패킷 수신 바이트

        private int CheckRecvCount = 0;
        private long CheckRecvBytes = 0;


        // SendCounter
        public int TotalSendCount { get; private set; } = 0;				// 총 패킷 전송수
        public double SendPacketPerSecond { get; private set; } = 0.0;		// 초당 패킷 전송수
        public long TotalSendBytes { get; private set; } = 0;				// 총 패킷 전송 바이트
        public long SendBytesPerSecond { get; private set; } = 0;			// 초당 패킷 전송 바이트

        private int CheckSendCount = 0;
        private long CheckSendBytes = 0;


        // UpdateTime
        private DateTime mLastUpdateTime;

		/// <summary>
		/// 클라이언트 접속시 관련 카운터를 처리합니다.
		/// </summary>
        public void Connect()
        {
            ++TotalConnection;
            ++CurrentConnection;
			MaxConnection = Math.Max(MaxConnection, CurrentConnection);
        }

		/// <summary>
		/// 클라이언트 접속 해제시 관련 카운터를 처리합니다.
		/// </summary>
        public void Disconnect()
        {
            --CurrentConnection;
        }

		/// <summary>
		/// 패킷 전송시 관련 카운터를 처리합니다.
		/// </summary>
		/// <param name="nSize">전송한 패킷 크기</param>
        public void Send(int nSize)
        {
            ++TotalSendCount;
            ++CheckSendCount;
            TotalSendBytes += nSize;
            CheckSendBytes += nSize;
        }

		/// <summary>
		/// 패킷 수신시 관련 카운터를 처리합니다.
		/// </summary>
		/// <param name="nSize">수신한 패킷 크기</param>
        public void Recv(int nSize)
        {
            ++TotalRecvCount;
            ++CheckRecvCount;
            TotalRecvBytes += nSize;
            CheckRecvBytes += nSize;
        }

		/// <summary>
		/// 모든 카운터를 초기화합니다.
		/// </summary>
        public void Reset()
        {
            // Connection
            TotalConnection = 0;
            CurrentConnection = 0;
            MaxConnection = 0;

            // RecvCounter
            TotalRecvCount = 0;
            RecvPacketPerSecond = 0.0;
            TotalRecvBytes = 0;
            RecvBytesPerSecond = 0;
            CheckRecvCount = 0;
            CheckRecvBytes = 0;

            // SendCounter
            TotalSendCount = 0;
            SendPacketPerSecond = 0.0;
            TotalSendBytes = 0;
            SendBytesPerSecond = 0;
            CheckSendCount = 0;
            CheckSendBytes = 0;

            mLastUpdateTime = DateTime.Now;
        }


		/// <summary>
		/// 초당 카운터를 계산해서 갱신합니다.
		/// </summary>
        public void Update()
        {
            double timeGap = (DateTime.Now - mLastUpdateTime).TotalMilliseconds * 0.001;
            mLastUpdateTime = DateTime.Now;

            if (timeGap <= 0.0)
                return;

            RecvPacketPerSecond = CheckRecvCount / timeGap;
            RecvBytesPerSecond = (long)(CheckRecvBytes / timeGap);
            CheckRecvCount = 0;
            CheckRecvBytes = 0;

            SendPacketPerSecond = CheckSendCount / timeGap;
            SendBytesPerSecond = (long)(CheckSendBytes / timeGap);
            CheckSendCount = 0;
            CheckSendBytes = 0;
        }



    }
}