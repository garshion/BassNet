using System;
using System.Linq;

namespace Bass.Net
{


    /// <summary>
    /// 네트워크 패킷 클래스 <br/>
    /// [Size 2byte][Option 1byte][Sequence 1byte][Protocol 4byte]
    /// </summary>
    public class Packet
    {
        #region Packet Const Defines

        public const int MAX_PACKET_BINARY_SIZE = 1440;         // MSS (MTU 1500 - TCP Header with Optional size 60 byte)

        public const int PACKET_SIZE_OFFSET = 0;
        public const int PACKET_SIZE_SIZE = sizeof(short);
        public const int PACKET_OPTION_OFFSET = PACKET_SIZE_OFFSET + PACKET_SIZE_SIZE;
        public const int PACKET_OPTION_SIZE = sizeof(byte);
        public const int PACKET_SEQUENCE_OFFSET = PACKET_OPTION_OFFSET + PACKET_OPTION_SIZE;
        public const int PACKET_SEQUENCE_SIZE = sizeof(byte);
        public const int PACKET_PROTOCOL_OFFSET = PACKET_SEQUENCE_OFFSET + PACKET_SEQUENCE_SIZE;
        public const int PACKET_PROTOCOL_SIZE = sizeof(int);


        public const int PACKET_HEADER_SIZE = PACKET_SIZE_SIZE + PACKET_OPTION_SIZE + PACKET_SEQUENCE_SIZE + PACKET_PROTOCOL_SIZE;
        public const int PACKET_DATA_OFFSET = PACKET_HEADER_SIZE;
        public const int MAX_PACKET_DATA_SIZE = MAX_PACKET_BINARY_SIZE - PACKET_HEADER_SIZE;

        #endregion


        public int SenderIndex { get; set; } = 0;
        public byte[] Binary { get; } = new byte[MAX_PACKET_BINARY_SIZE];


        /// <summary>
        /// 패킷 전체 바이너리 크기
        /// </summary>
        public short Size
        {
            get
            {
                var size = BitConverter.ToInt16(Binary, PACKET_SIZE_OFFSET);

                if (BitConverter.IsLittleEndian)
                {
                    var data = BitConverter.GetBytes(size);
                    data.Reverse();
                    size = BitConverter.ToInt16(data, 0);
                }

                return size;
            }
            set
            {
                if (value < 0
                    || value > MAX_PACKET_BINARY_SIZE)
                    return;

                var temp = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    temp.Reverse();

                Buffer.BlockCopy(temp, 0, Binary, PACKET_SIZE_OFFSET, PACKET_SIZE_SIZE);
            }
        }

        /// <summary>
        /// 패킷 옵션
        /// </summary>
        public byte Option
        {
            get
            {
                return Binary[PACKET_OPTION_OFFSET];
            }
            set
            {
                Binary[PACKET_OPTION_OFFSET] = value;
            }
        }

        /// <summary>
        /// 패킷 일렬번호
        /// </summary>
        public byte Sequence
        {
            get
            {
                return Binary[PACKET_SEQUENCE_OFFSET];
            }
            set
            {
                Binary[PACKET_SEQUENCE_OFFSET] = value;
            }
        }

        /// <summary>
        /// 패킷 메시지 구분 프로토콜 타입
        /// </summary>
        public int Protocol
        {
            get
            {
                var protocol = BitConverter.ToInt32(Binary, PACKET_PROTOCOL_OFFSET);

                if (BitConverter.IsLittleEndian)
                {
                    var data = BitConverter.GetBytes(protocol);
                    data.Reverse();
                    protocol = BitConverter.ToInt32(data, 0);
                }

                return protocol;
            }
            set
            {
                var temp = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    temp.Reverse();

                Buffer.BlockCopy(temp, 0, Binary, PACKET_PROTOCOL_OFFSET, PACKET_PROTOCOL_SIZE);
            }
        }

        public int DataSize => Math.Max(Size - PACKET_HEADER_SIZE, 0);




        public Packet() => Set(0);

        public Packet(int protocol) => Set(protocol);

        public Packet(int protocol, byte[] data, int dataSize) => Set(protocol, data, 0, dataSize);

        public Packet(int senderIndex, byte[] recvBuffer, int recvOffset, int recvSize)
        {
            FromReceiveData(senderIndex, recvBuffer, recvOffset, recvSize);
        }

        public Packet(byte[] buffer, int bufferSize)
        {
            if (null == buffer
                || buffer.Length < bufferSize
                || bufferSize > MAX_PACKET_BINARY_SIZE)
                return;

            FromReceiveData(buffer, 0, bufferSize);
        }


        /// <summary>
        /// 프로토콜을 포함하여 패킷 데이터를 체웁니다.
        /// </summary>
        public bool Set(int protocol, byte[] data = null, int dataOffset = 0, int dataSize = 0)
        {
            Protocol = protocol;
            if (null == data)
            {
                Size = PACKET_HEADER_SIZE;
                return true;
            }
            else
            {
                return SetData(data, dataOffset, dataSize);
            }
        }


        /// <summary>
        /// 패킷 데이터를 체웁니다.
        /// </summary>
        public bool SetData(byte[] data, int dataOffset, int dataSize)
        {
            if (null == data)
                return false;

            if (dataOffset < 0)
                return false;

            if (dataSize <= 0
                || dataSize > MAX_PACKET_DATA_SIZE)
                return false;   // 복사할 크기가 최대 패킷 데이터 크기보다 큼

            if (data.Length - dataOffset < dataSize)
                return false;   // 복사하고자 하는 데이터 크기가 부족함

            Buffer.BlockCopy(data, dataOffset, Binary, PACKET_DATA_OFFSET, dataSize);
            Size = (short)(PACKET_HEADER_SIZE + dataSize);    // 크기 설정

            return true;
        }




        /// <summary>
        /// 네트워크 수신 버퍼로부터 패킷 데이터를 체웁니다.
        /// </summary>
        public bool FromReceiveData(byte[] recvData, int dataOffset, int recvSize)
        {
            if (null == recvData)
                return false;   // Invalid Input Data

            if (dataOffset < 0)
                return false;   // Invalid data Offset

            if (recvSize < 0)
                return false;   // Invalid data Size

            if (recvData.Length - dataOffset < recvSize)
                return false;   // 복사하고자 하는 데이터 크기가 부족함

            if (recvSize - dataOffset < PACKET_HEADER_SIZE)
                return false;   // 수신 메시지가 패킷 최소 크기보다 작음

            if (recvSize - dataOffset > MAX_PACKET_BINARY_SIZE)
                return false;   // 수신 메시지가 최대 패킷 크기보다 큼

            Buffer.BlockCopy(recvData, dataOffset, Binary, 0, recvSize);
            return true;
        }

        /// <summary>
        /// 네트워크 수신 버퍼로부터 SessionIndex와 패킷 데이터를 체웁니다.
        /// </summary>
        public bool FromReceiveData(int senderIndex, byte[] recvData, int dataOffset, int recvSize)
        {
            if (!FromReceiveData(recvData, dataOffset, recvSize))
                return false;

            SenderIndex = senderIndex;
            return true;
        }

        /// <summary>
        /// 패킷을 재사용하기 위하여 초기화합니다.
        /// </summary>
        public void Reset()
        {
            SenderIndex = 0;
            Array.Clear(Binary, 0, PACKET_HEADER_SIZE);  // 헤더 부분만 초기화
        }

        /// <summary>
        /// 패킷 데이터를 복제합니다.
        /// </summary>
        public Packet Copy() => new Packet(SenderIndex, Binary, 0, Binary.Length);






    }
}
