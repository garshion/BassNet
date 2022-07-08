using System;

namespace Bass.Net
{
    public class Packet
    {

        public int SenderIndex { get; set; } = 0;
        public byte[] Binary { get; private set; } = new byte[Define.MaxPacketBinaryLength];

        public int Protocol
        { 
            get
            {
                return BitConverter.ToInt32(Binary, Define.PacketProtocolOffset);
            } 
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Binary, Define.PacketProtocolOffset, Define.PacketProtocolLength);
            }
        }

        public int Size
        {
            get
            {
                return BitConverter.ToInt32(Binary, Define.PacketSizeOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Binary, Define.PacketSizeOffset, Define.PacketSizeLength);
            }
        }

        public int DataSize
        {
            get
            {
                int nRet = Size - Define.PacketHeaderLength;
                if (nRet < 0
                    || nRet > Define.MaxPacketDataBinaryLength)
                    nRet = 0;
                return nRet;
            }
        }


        public Packet() => SetData(0, null, 0);

        public Packet(int protocol) => SetData(protocol, null, 0);

        public Packet(int protocol, byte[] data, int dataSize) => SetData(protocol, data, dataSize);

        public Packet(byte[] recvBuffer, int nBufferSize)
        {
            // from RecvBuffer
            if(null == recvBuffer
                || nBufferSize < Define.PacketHeaderLength
                || nBufferSize > Define.MaxPacketBinaryLength)
            {
                SetData(0, null, 0);
                return;
            }

            Buffer.BlockCopy(recvBuffer, 0, Binary, 0, nBufferSize);
        }

        public ENetError SetData(int protocol, byte[] data = null, int dataSize = 0)
        {
            Protocol = protocol;
            Size = Define.PacketHeaderLength;
            return SetData(data, dataSize);
        }

        public ENetError SetData(byte[] data, int dataSize)
        {
            if (dataSize < 0)
                return ENetError.Packet_InvalidDataSize;

            if (dataSize > Define.MaxPacketDataBinaryLength)
                return ENetError.Packet_DataSizeIsTooLarge;

            if (dataSize > 0)
            {
                if (null == data)
                    return ENetError.Packet_DataIsNull;

                Buffer.BlockCopy(data, 0, Binary, Define.PacketDataOffset, dataSize);
            }

            Size = Define.PacketHeaderLength + dataSize;

            return ENetError.Success;
        }

        public ENetError CopyRecvData(byte[] recvData, int dataSize)
        {
            if (null == recvData)
                return ENetError.Packet_DataIsNull;

            if (dataSize < Define.PacketHeaderLength)
                return ENetError.Packet_InvalidDataSize;

            if (dataSize > Define.MaxPacketBinaryLength)
                return ENetError.Packet_DataSizeIsTooLarge;

            Buffer.BlockCopy(recvData, 0, Binary, 0, dataSize);

            return ENetError.Success;
        }

        public void Reset()
        {
            SenderIndex = 0;
            Array.Clear(Binary, 0, Define.PacketHeaderLength);  // 헤더 부분만 초기화
        }
    }
}
