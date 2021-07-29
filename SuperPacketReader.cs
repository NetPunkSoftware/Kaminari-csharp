using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class SuperPacketReader
	{
		private Buffer _buffer;

		public SuperPacketReader(byte[] data)
		{
			_buffer = new Buffer(data);
		}

		public ushort length()
		{
			return _buffer.readUshort(0);
		}

		public ushort id()
		{
			return _buffer.readUshort(2);
		}

		public bool HasFlag(SuperPacketFlags flag)
		{
			return (_buffer.readByte(4) & (byte)flag) != 0x00;
		}

		public List<ushort> getAcks()
		{
			List<ushort> acksList = new List<ushort>();
			int offset = sizeof(ushort) * 2 + sizeof(byte);
			ushort ackBase = _buffer.readUshort(offset);
			uint acks = _buffer.readUint(offset + sizeof(ushort));

			if (acks > 0)
			{
				// Check previous IDs
				for (ushort i = 0; i < 32; ++i)
				{
					if ((acks & (uint)(1 << i)) > 0)
					{
						acksList.Add((ushort)(ackBase - i));
					}
				}
			}

			return acksList;
		}

		public bool hasData()
		{
        	int offset = sizeof(ushort) * 2 + sizeof(byte) + sizeof(ushort) + sizeof(uint);
			return _buffer.readByte(offset) != 0;
		}

		public bool isPingPacket()
		{
			return HasFlag(SuperPacketFlags.Ping) && !HasFlag(SuperPacketFlags.Ack);
		}

		public void handlePackets<PQ, T>(Protocol<PQ> protocol, IHandlePacket handler, T client) where PQ : IProtocolQueues where T : IBaseClient
		{
			int offset = sizeof(ushort) * 2 + sizeof(byte) + sizeof(ushort) + sizeof(uint);
			int numBlocks = (int)_buffer.readByte((int)offset);
			int blockPos = (int)offset + sizeof(byte);

			int remaining = 500 - blockPos;
			for (int i = 0; i < numBlocks; ++i)
			{
				ushort blockId = _buffer.readUshort(blockPos);
				int numPackets = (int)_buffer.readByte(blockPos + sizeof(ushort));
				if (numPackets == 0)
				{
					return;
				}

            	ulong blockTimestamp = protocol.blockTimestamp(blockId);
				blockPos += sizeof(ushort) + sizeof(byte);
				remaining -= sizeof(ushort) + sizeof(byte);

				for (int j = 0; j < numPackets && remaining > 0; ++j)
				{
					PacketReader packet = new PacketReader(new Buffer(_buffer, blockPos, Packet.dataStart), blockTimestamp);
					int length = packet.getLength();
					blockPos += length;
					remaining -= length;

					if (length < Packet.dataStart || remaining < 0)
					{
						return;
					}

					if (protocol.resolve(packet, blockId))
					{
						if (!handler.handlePacket(packet, client))
						{
							client.handlingError();
						}
					}
				}
			}
		}
	}
}
