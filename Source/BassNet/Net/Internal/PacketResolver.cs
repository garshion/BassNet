using System;

namespace Bass.Net.Internal
{
    internal class PacketResolver
    {
        //private int mMessageSize = 0;
        //private byte[] mBuffer = new byte[Define.SocketBufferSize];
        private int mCurrentPosition = 0;
        private int mPositionToRead = 0;
        private int mRemainSize = 0;

        private Packet mRecvBuffer = new Packet();

        private int mMessageSize => mRecvBuffer.Size;
        private byte[] mBuffer => mRecvBuffer.Binary;



        public PacketResolver()
        {
        }

        public void ResolveProcess(ref byte[] buffer, int offset, int transfered, Action<byte[], int> _messageResolveComplete)
        {
            mRemainSize = transfered;
            int srcPosition = offset;
            while (mRemainSize > 0)
            {
                if (mCurrentPosition < Packet.PACKET_HEADER_SIZE)
                {
                    mPositionToRead = Packet.PACKET_HEADER_SIZE;

                    if (false == _ReadUntil(buffer, ref srcPosition, offset, transfered))
                        return;

                    //mMessageSize = _GetMessageSize();
                    mPositionToRead = mMessageSize;
                }

                if (mMessageSize == Packet.PACKET_HEADER_SIZE
                    || true == _ReadUntil(buffer, ref srcPosition, offset, transfered))
                {
                    _messageResolveComplete(mBuffer, mMessageSize);
                    _ClearBuffer();
                }

            }
        }


        #region Private functions

        private bool _ReadUntil(byte[] buffer, ref int refSourceOffset, int offset, int transffered)
        {
            // 이미 다 읽은 경우
            if (mCurrentPosition >= offset + transffered)
                if (mPositionToRead <= mCurrentPosition)
                    return false;

            int copySize = mPositionToRead - mCurrentPosition;

            // 가능한만큼만 복사처리.
            if (mRemainSize < copySize)
                copySize = mRemainSize;

            if (copySize <= 0)
                return false;

            // 복사할 크기 검사를 바로 위에서 안전하게 처리했으므로 try~catch를 사용하지 않음.
            Buffer.BlockCopy(buffer, refSourceOffset, mBuffer, mCurrentPosition, copySize);

            refSourceOffset += copySize;

            mCurrentPosition += copySize;
            mRemainSize -= copySize;

            if (mCurrentPosition < mPositionToRead)
                return false;

            return true;
        }


        private void _ClearBuffer()
        {
            Array.Clear(mBuffer, 0, mBuffer.Length);
            mCurrentPosition = 0;
            //mMessageSize = 0;
        }

        //private int _GetMessageSize()
        //{
        //    return BitConverter.ToInt32(mBuffer, Define.PacketProtocolLength);
        //}


        #endregion
    }
}
