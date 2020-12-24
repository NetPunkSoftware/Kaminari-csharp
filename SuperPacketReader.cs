using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public class SuperPacketReader<PQ> where PQ : IProtocolQueues
	{
		private Buffer _buffer;
		private uint _ackEnd;
		private bool _hasAcks;

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

		public ushort timestamp()
		{
			return _buffer.readUshort(4);
		}

		public List<ushort> getAcks()
		{
			List<ushort> acks = new List<ushort>();
			_ackEnd = sizeof(ushort) * 2;
			int numAcks = (int)_buffer.readByte((int)_ackEnd);
			_hasAcks = numAcks != 0;
			_ackEnd += sizeof(byte);

			for (int i = 0; i < numAcks; ++i)
			{
				ushort ack = _buffer.readUshort((int)_ackEnd);
				acks.Add(ack);
				_ackEnd += sizeof(ushort);
			}

			return acks;
		}

		public bool hasData()
		{
			return _buffer.readByte((int)_ackEnd) != 0;
		}

		public bool isPingPacket()
		{
			return !_hasAcks && !hasData();
		}

		public void handlePackets<T>(Protocol<PQ> protocol, IHandlePacket handler, T client) where T : IBaseClient
		{
			int numBlocks = (int)_buffer.readByte((int)_ackEnd);
			int blockPos = (int)_ackEnd + sizeof(byte);

			int remaining = 500 - blockPos;
			for (int i = 0; i < numBlocks; ++i)
			{
				ushort blockId = _buffer.readUshort(blockPos);
				int numPackets = (int)_buffer.readByte(blockPos + sizeof(ushort));
				if (numPackets == 0)
				{
					return;
				}

				blockPos += sizeof(ushort) + sizeof(byte);
				remaining -= sizeof(ushort) + sizeof(byte);

				for (int j = 0; j < numPackets && remaining > 0; ++j)
				{
					PacketReader packet = new PacketReader(new Buffer(_buffer, blockPos, Packet.dataStart));
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
