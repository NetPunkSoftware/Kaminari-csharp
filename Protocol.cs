using System;
using System.Collections;
using System.Collections.Generic;

namespace Kaminari
{
	public class Protocol<PQ> : IProtocol<PQ> where PQ : IProtocolQueues
	{
		private class ResolvedBlock
		{
			public byte loopCounter;
			public List<byte> packetCounters;

			public ResolvedBlock(byte loopCounter)
			{
				this.loopCounter = loopCounter;
				this.packetCounters = new List<byte>();
			}
		}

		private ushort bufferSize;
		private ushort sinceLastPing;
		private ushort sinceLastRecv;
		private ushort lastBlockIdRead;
		private ushort expectedBlockId;
		private bool serverBasedSync;
		private ushort lastServerID;
		private int serverTimeDiff;
		private byte loopCounter;
		private ulong timestamp;
		private ushort timestampBlockId;
		private Dictionary<ushort, ResolvedBlock> alreadyResolved;
		private ServerPhaseSync phaseSync;

		public Protocol()
		{
			phaseSync = new ServerPhaseSync();
			Reset();
		}

		public ushort getLastBlockIdRead()
		{
			return lastBlockIdRead;
		}

		public ushort getExpectedBlockId()
		{
			return expectedBlockId;
		}
		public ushort getLastReadID()
		{
			return lastBlockIdRead;
		}
		public ServerPhaseSync getPhaseSync()
		{
			return phaseSync;
		}

		public byte getLoopCounter()
		{
			return loopCounter;
		}

		public ushort getLastSentSuperPacketSize(SuperPacket<PQ> superpacket)
		{
			return (ushort)superpacket.getBuffer().getPosition();
		}
		public ushort getLastRecvSuperPacketSize(IBaseClient client)
		{
			return client.lastSuperPacketSize();
		}

		public void setBufferSize(ushort size)
		{
			bufferSize = size;
		}

		public bool isExpected(ushort id)
		{
			return expectedBlockId == 0 || Overflow.le(id, expectedBlockId);
		}

		public void setTimestamp(ulong timestamp, ushort blockId)
		{
			this.timestamp = timestamp;
			this.timestampBlockId = blockId;
		}

		public ulong blockTimestamp(ushort blockId)
		{
			if (Overflow.ge(blockId, timestampBlockId))
			{
				return timestamp + (ulong)(blockId - timestampBlockId) * Constants.WorldHeartBeat;
			}

			return timestamp - (ulong)(timestampBlockId - blockId) * Constants.WorldHeartBeat;
		}

		public void Reset()
		{
			bufferSize = 0;
			sinceLastPing = 0;
			sinceLastRecv = 0;
			lastBlockIdRead = 0;
			serverBasedSync = true;
			lastServerID = 0;
			expectedBlockId = 0;
			loopCounter = 0;
			timestamp = now();
			timestampBlockId = 0;
			alreadyResolved = new Dictionary<ushort, ResolvedBlock>();
		}

		public ulong now()
		{
			return (ulong)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds; // TODO(gpascualg): Real time without leap seconds
		}

		public void InitiateHandshake(SuperPacket<PQ> superpacket)
		{
			superpacket.SetFlag(SuperPacketFlags.Handshake);
			superpacket.SetInternalFlag(SuperPacketInternalFlags.WaitFirst);
		}

		public Buffer update(IBaseClient client, SuperPacket<PQ> superpacket)
		{
			++sinceLastPing;

			// TODO(gpascualg): Lock superpacket
			if (superpacket.finish() || needsPing())
			{
				if (needsPing())
				{
					sinceLastPing = 0;
				}

				return new Buffer(superpacket.getBuffer());
			}

			return null;
		}

		private bool needsPing()
		{
			return sinceLastPing >= 20;
		}

		public void clientHasNewPacket(IBaseClient client, SuperPacket<PQ> superpacket)
		{
			lastServerID = client.lastSuperPacketId();
			phaseSync.ServerPacket(lastServerID, (float)serverTimeDiff);

			if (!serverBasedSync)
			{
				return;
			}

			// Setup current expected ID and superpacket ID
			// expectedBlockId = Math.Max(expectedBlockId, (ushort)(lastServerID + 1));
			// superpacket.serverUpdatedId(lastServerID);
			serverTimeDiff = superpacket.getID() - lastServerID;

			// serverTimeDiff = Math.Max(0, superpacket.getID() - lastServerID);
		}

		public ushort getLastServerID()
		{
			return lastServerID;
		}

		public int getServerTimeDiff()
		{
			return serverTimeDiff;
		}

		public bool read(IBaseClient client, SuperPacket<PQ> superpacket, IHandlePacket handler)
		{
			timestampBlockId = expectedBlockId;
			timestamp = now();

			if (!client.hasPendingSuperPackets())
			{
				expectedBlockId = (ushort)(expectedBlockId + 1);

				if (++sinceLastRecv >= Constants.MaxBlocksUntilDisconnection)
				{
					client.disconnect();
				}

				return false;
			}

			sinceLastRecv = 0;
			ushort expectedId = Overflow.sub(expectedBlockId, bufferSize);

			while (client.hasPendingSuperPackets() &&
					!Overflow.geq(client.firstSuperPacketId(), expectedId))
			{
				read_impl(client, superpacket, handler);
			}

			expectedBlockId = Overflow.inc(expectedBlockId);
			return true;
		}

		private void handleAcks(SuperPacketReader<PQ> reader, SuperPacket<PQ> superpacket)
		{
			lastBlockIdRead = reader.id();

			foreach (ushort ack in reader.getAcks())
			{
				superpacket.Ack(ack);
				// TODO(gpascualg): Lag compensation
			}

			if (reader.hasData() || reader.isPingPacket())
			{
				superpacket.scheduleAck(lastBlockIdRead);
			}
		}

		public void read_impl(IBaseClient client, SuperPacket<PQ> superpacket, IHandlePacket handler)
		{
			SuperPacketReader<PQ> reader = new SuperPacketReader<PQ>(client.popPendingSuperPacket());

			// Handshake process skips all procedures, including order
			if (reader.HasFlag(SuperPacketFlags.Handshake))
			{
				// Make sure we are ready for the next valid block
				expectedBlockId = Overflow.inc(reader.id());

				// Reset all variables related to packet parsing
				timestampBlockId = expectedBlockId;
				timestamp = now();
				loopCounter = 0;
				alreadyResolved.Clear();
				
				// Acks have no implication for us, but non-acks mean we have to ack
				if (!reader.HasFlag(SuperPacketFlags.Ack))
				{
					superpacket.SetFlag(SuperPacketFlags.Ack);
					superpacket.SetFlag(SuperPacketFlags.Handshake);
				}

				// Either case, skip all processing except acks
				handleAcks(reader, superpacket);
				return;
			}
			else
			{
				superpacket.ClearInternalFlag(SuperPacketInternalFlags.WaitFirst);
			}

			if (Overflow.le(reader.id(), lastBlockIdRead))
			{
				return;
			}

			if (Overflow.sub(expectedBlockId, reader.id()) > Constants.MaximumBlocksUntilResync)
			{
				superpacket.SetFlag(SuperPacketFlags.Handshake);
			}

			if (lastBlockIdRead > reader.id())
			{
				loopCounter = (byte)(loopCounter + 1);
			}

			handleAcks(reader, superpacket);
			reader.handlePackets(this, handler, client);
		}

		public bool resolve(PacketReader packet, ushort blockId)
		{
			byte id = packet.getId();

			if (alreadyResolved.ContainsKey(blockId))
			{
				ResolvedBlock info = alreadyResolved[blockId];

				if (info.loopCounter != loopCounter)
				{
					if (Overflow.sub(lastBlockIdRead, blockId) > Constants.MaximumBlocksUntilResync)
					{
						info.loopCounter = loopCounter;
						info.packetCounters.Clear();
					}
				}
				else if (Overflow.sub(lastBlockIdRead, blockId) > Constants.MaximumBlocksUntilResync)
				{
					// TODO(gpascualg): Flag resync
					return false;
				}

				if (info.packetCounters.Contains(id))
				{
					return false;
				}

				info.packetCounters.Add(id);
			}
			else
			{
				ResolvedBlock info = new ResolvedBlock(loopCounter);
				info.packetCounters.Add(id);
				alreadyResolved.Add(blockId, info);
			}

			return true;
		}
	}
}
