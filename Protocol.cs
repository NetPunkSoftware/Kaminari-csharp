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
		private float estimatedRTT;
		private ushort sinceLastRecv;
		private ushort lastBlockIdRead;
		private ushort expectedBlockId;
		private bool serverBasedSync;
		private ushort lastServerID;
		private int serverTimeDiff;
		private byte loopCounter;
		private ulong timestamp;
		private ushort timestampBlockId;
		private float recvKbpsEstimate;
		private float recvKbpsEstimateAcc;
		private ulong recvKbpsEstimateTime;
		private float sendKbpsEstimate;
		private float sendKbpsEstimateAcc;
		private ulong sendKbpsEstimateTime;
		private Dictionary<ushort, ResolvedBlock> alreadyResolved;
		private ServerPhaseSync<PQ> phaseSync;
		private Dictionary<ushort, ulong> packetTimes;

		public Protocol()
		{
			phaseSync = new ServerPhaseSync<PQ>(this);
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
		public ServerPhaseSync<PQ> getPhaseSync()
		{
			return phaseSync;
		}

		public byte getLoopCounter()
		{
			return loopCounter;
		}

		public float getEstimatedRTT()
		{
			return estimatedRTT;
		}

		public ushort getLastSentSuperPacketSize(SuperPacket<PQ> superpacket)
		{
			return (ushort)superpacket.getBuffer().getPosition();
		}
		public ushort getLastRecvSuperPacketSize(IBaseClient client)
		{
			return client.lastSuperPacketSize();
		}

		public float RecvKbpsEstimate()
		{
			if (DateTimeExtensions.now() - recvKbpsEstimateTime > 1000.0f)
			{
				recvKbpsEstimate += recvKbpsEstimateAcc / ((DateTimeExtensions.now() - recvKbpsEstimateTime) / 1000.0f);
				recvKbpsEstimate /= 2.0f;
				recvKbpsEstimateAcc = 0;
				recvKbpsEstimateTime = DateTimeExtensions.now();
			}

			return recvKbpsEstimate / 1024;
		}

		public float SendKbpsEstimate()
		{
			if (DateTimeExtensions.now() - sendKbpsEstimateTime > 1000.0f)
			{
				sendKbpsEstimate += sendKbpsEstimateAcc / ((DateTimeExtensions.now() - sendKbpsEstimateTime) / 1000.0f);
				sendKbpsEstimate /= 2.0f;
				sendKbpsEstimateAcc = 0;
				sendKbpsEstimateTime = DateTimeExtensions.now();
			}

			return sendKbpsEstimate / 1024;
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
			estimatedRTT = 50;
			sinceLastRecv = 0;
			lastBlockIdRead = 0;
			serverBasedSync = true;
			lastServerID = 0;
			expectedBlockId = 0;
			loopCounter = 0;
			timestamp = DateTimeExtensions.now();
			timestampBlockId = 0;
			recvKbpsEstimate = 0;
			recvKbpsEstimateAcc = 0;
			recvKbpsEstimateTime = DateTimeExtensions.now();
			sendKbpsEstimate = 0;
			sendKbpsEstimateAcc = 0;
			sendKbpsEstimateTime = DateTimeExtensions.now();
			alreadyResolved = new Dictionary<ushort, ResolvedBlock>();
			packetTimes = new Dictionary<ushort, ulong>();
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

				Buffer buffer = new Buffer(superpacket.getBuffer());

				// Register time for ping purposes
				if (!superpacket.HasFlag(SuperPacketFlags.Handshake) && 
					!superpacket.HasInternalFlag(SuperPacketInternalFlags.WaitFirst))
				{
					packetTimes.Add(buffer.readUshort(2), DateTimeExtensions.now());
				}

				// Update estimate
				sendKbpsEstimateAcc += buffer.getPosition();

				return buffer;
			}

			return null;
		}

		private bool needsPing()
		{
			return sinceLastPing >= 20;
		}

		public void clientHasNewPacket(IBaseClient client, SuperPacket<PQ> superpacket)
		{
			// Update PLL
			lastServerID = client.lastSuperPacketId();
			phaseSync.ServerPacket(lastServerID);

			// Save estimate
			recvKbpsEstimateAcc += client.lastSuperPacketSize();

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
			timestamp = DateTimeExtensions.now();

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
				
				if (!superpacket.HasFlag(SuperPacketFlags.Handshake) && 
					!superpacket.HasInternalFlag(SuperPacketInternalFlags.WaitFirst) && 
					packetTimes.ContainsKey(ack))
				{
					const float w = 0.9f;
					ulong diff = DateTimeExtensions.now() - packetTimes[ack];
					packetTimes.Remove(ack);
					estimatedRTT = estimatedRTT * w + diff * (1.0f - w);
				}
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
				timestamp = DateTimeExtensions.now();
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
