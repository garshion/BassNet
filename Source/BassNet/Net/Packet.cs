using System;

namespace Bass.Net
{

	/// <summary>
	/// 패킷 구조
	/// [ Protocol 4byte ][ option 2byte ][ size 2byte ]
	/// </summary>

	internal enum EPacketOption : ushort
	{
		None = 0,
		LZ4Compressed = 1 << 0,     // LZ4를 이용한 압축 (FAST)
		XORApplied = 1 << 1,        // XOR 적용

		IsSystemPacket = 1 << 15,   // 시스템 패킷인지 여부. (라이브러리 내부 패킷)
	}



	public class Packet
	{
		private const int PacketCompressStartSize = 60; // 패킷 압축을 할 데이터의 크기






		public int SenderIndex { get; set; } = 0;
		public byte[] Binary { get; private set; } = new byte[Define.MaxPacketBinaryLength];

		/// <summary>
		/// 패킷의 프로토콜(종류) 를 나타냅니다.
		/// </summary>
		public int Protocol
		{
			get => BitConverter.ToInt32(Binary, Define.PacketProtocolOffset);
			private set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Binary, Define.PacketProtocolOffset, Define.PacketProtocolLength);
		}

		/// <summary>
		/// 패킷의 전체 크기를 나타냅니다.
		/// </summary>
		public int Size
		{
			get => BitConverter.ToInt16(Binary, Define.PacketSizeOffset);
			private set
			{
				if (value <= 0
					|| value >= Define.MaxPacketBinaryLength)
					return;

				short setVal = (short)value;
				Buffer.BlockCopy(BitConverter.GetBytes(setVal), 0, Binary, Define.PacketSizeOffset, Define.PacketSizeLength);
			}
		}

		/// <summary>
		/// 패킷에서 데이터 부분의 크기를 나타냅니다.
		/// </summary>
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



		public ushort Option
		{
			get => BitConverter.ToUInt16(Binary, Define.PacketOptionOffset);
			private set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Binary, Define.PacketOptionOffset, Define.PacketOptionLength);
		}

		#region Option Value

		/// <summary>
		/// 패킷 데이터가 압축되었는지 여부를 나타냅니다.
		/// </summary>
		public bool Compressed
		{
			get => _GetOption(EPacketOption.LZ4Compressed);
			private set => _SetOption(EPacketOption.LZ4Compressed, value);
		}

		/// <summary>
		/// 패킷 데이터가 XOR 연산이 되었는지 여부를 나타냅니다.
		/// </summary>
		public bool XORApplied
		{
			get => _GetOption(EPacketOption.XORApplied);
			private set => _SetOption(EPacketOption.XORApplied, value);
		}

		/// <summary>
		/// 이 패킷이 시스템(라이브러리 내부) 패킷인지 여부를 나타냅니다.
		/// </summary>
		public bool IsSystemPacket
		{
			get => _GetOption(EPacketOption.IsSystemPacket);
			set => _SetOption(EPacketOption.IsSystemPacket, value);
		}

		#endregion




		public Packet() => SetData(0, null, 0);

		public Packet(int protocol) => SetData(protocol, null, 0);

		public Packet(int protocol, byte[] data, int dataSize) => SetData(protocol, data, dataSize);

		public Packet(byte[] recvBuffer, int nBufferSize)
		{
			// from RecvBuffer
			if (null == recvBuffer
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

		/// <summary>
		/// 수신 받은 데이터를 패킷 데이터로 만듭니다.
		/// </summary>
		/// <param name="recvData"></param>
		/// <param name="dataSize"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 패킷 데이터를 재사용하기 위해 초기화합니다.
		/// </summary>
		public void Reset()
		{
			SenderIndex = 0;
			Array.Clear(Binary, 0, Define.PacketHeaderLength);  // 헤더 부분만 초기화
		}





		private void _SetOption(EPacketOption option, bool onoff)
		{
			if (onoff)
				Option |= (ushort)option;
			else
				Option &= ((ushort)~option);
		}

		private bool _GetOption(EPacketOption option)
		{
			return (Option & (ushort)option) != 0;
		}


	}
}
