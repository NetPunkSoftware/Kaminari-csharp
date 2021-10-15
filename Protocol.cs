using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;


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
		private float serverTimeDiff;
		private byte loopCounter;
		private ulong timestamp;
		private ushort timestampBlockId;
		private float recvKbpsEstimate;
		private float recvKbpsEstimateAcc;
		private ulong recvKbpsEstimateTime;
		private float sendKbpsEstimate;
		private float sendKbpsEstimateAcc;
		private ulong sendKbpsEstimateTime;
		private ushort lastRecvSize;
		private ushort lastSendSize;
		private Dictionary<ushort, ResolvedBlock> alreadyResolved;
		private ServerPhaseSync<PQ> phaseSync;
		private ConcurrentDictionary<ushort, ulong> packetTimes;

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
			return lastSendSize;
		}
		public ushort getLastRecvSuperPacketSize(IBaseClient client)
		{
			return lastRecvSize;
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
			packetTimes = new ConcurrentDictionary<ushort, ulong>();
		}

		public void InitiateHandshake(SuperPacket<PQ> superpacket)
		{
			superpacket.SetFlag(SuperPacketFlags.Handshake);
			superpacket.SetInternalFlag(SuperPacketInternalFlags.WaitFirst);
		}

		public Buffer update(IBaseClient client, SuperPacket<PQ> superpacket)
		{
			++sinceLastPing;
			if (needsPing())
			{
				sinceLastPing = 0;
				superpacket.SetFlag(SuperPacketFlags.Ping);
			}

			// TODO(gpascualg): Lock superpacket
			if (superpacket.finish())
			{
				Buffer buffer = new Buffer(superpacket.getBuffer());

				// Register time for ping purposes
				if (!superpacket.HasFlag(SuperPacketFlags.Handshake) && 
					!superpacket.HasInternalFlag(SuperPacketInternalFlags.WaitFirst))
				{
					packetTimes.AddOrUpdate(buffer.readUshort(2), DateTimeExtensions.now(), (key, old) => DateTimeExtensions.now());
				}

				// Update estimate
				sendKbpsEstimateAcc += buffer.getPosition();
				lastSendSize = (ushort)buffer.getPosition();

				return buffer;
			}

			lastSendSize = 0;
			return null;
		}

		private bool needsPing()
		{
			return sinceLastPing >= 20;
		}

		public ushort getLastServerID()
		{
			return lastServerID;
		}

		public float getServerTimeDiff()
		{
			return serverTimeDiff;
		}

		private void increaseExpectedBlock(SuperPacket<PQ> superpacket)
		{
			if (!superpacket.HasFlag(SuperPacketFlags.Handshake) && !superpacket.HasInternalFlag(SuperPacketInternalFlags.WaitFirst))
			{
				expectedBlockId = Overflow.inc(expectedBlockId);
			}
		}

		public bool read(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal marshal)
		{
			timestampBlockId = expectedBlockId;
			timestamp = DateTimeExtensions.now();

			if (!client.hasPendingSuperPackets())
			{
				increaseExpectedBlock(superpacket);

				if (++sinceLastRecv >= Constants.MaxBlocksUntilDisconnection)
				{
					client.disconnect();
				}

				lastRecvSize = 0;
				marshal.Update(client, expectedBlockId);
				return false;
			}

			sinceLastRecv = 0;
			ushort expectedId = expectedBlockId;
			if (!Constants.UseKumoQueues)
			{
				expectedBlockId = Overflow.sub(expectedBlockId, bufferSize);
			}

			while (client.hasPendingSuperPackets() &&
					!Overflow.ge(client.firstSuperPacketId(), expectedId))
			{
				read_impl(client, superpacket, marshal);
			}

			increaseExpectedBlock(superpacket);
			marshal.Update(client, expectedBlockId);
			return true;
		}

		public void HandleServerTick(SuperPacketReader reader, SuperPacket<PQ> superpacket)
		{
			if (!superpacket.HasFlag(SuperPacketFlags.Handshake) && 
				!superpacket.HasInternalFlag(SuperPacketInternalFlags.WaitFirst))
			{
				// Update lag estimation
				foreach (ushort ack in reader.getAcks())
				{
					if (packetTimes.TryRemove(ack, out var timestamp))
					{
						const float w = 0.99f;
						ulong diff = DateTimeExtensions.now() - timestamp;
						estimatedRTT = estimatedRTT * w + diff * (1.0f - w);
					}
				}
			}

			// Update PLL
			lastServerID = Math.Max(lastServerID, reader.id());
			phaseSync.ServerPacket(lastServerID);

			// Save estimate
			recvKbpsEstimateAcc += reader.length();
			lastRecvSize = reader.length();

			if (!serverBasedSync)
			{
				return;
			}

			// Setup current expected ID and superpacket ID
			// expectedBlockId = Math.Max(expectedBlockId, (ushort)(lastServerID + 1));
			// superpacket.serverUpdatedId(lastServerID);

			// serverTimeDiff = expectedBlockId - lastServerID + superpacket.getID() - lastServerID;
			// serverTimeDiff = superpacket.getID() - lastServerID;
			// serverTimeDiff = Math.Max(0, superpacket.getID() - lastServerID);

			expectedBlockId = lastServerID;
			serverTimeDiff = superpacket.getID() - lastServerID - (estimatedRTT / 50.0f + 1); //- (int)(estimatedRTT / 2.0f);
		}

		public void HandleAcks(SuperPacketReader reader, SuperPacket<PQ> superpacket)
		{
			// Handle flags already
			bool is_handshake = reader.HasFlag(SuperPacketFlags.Handshake);
			if (is_handshake)
			{
				if (!reader.HasFlag(SuperPacketFlags.Ack))
				{
					superpacket.SetFlag(SuperPacketFlags.Ack);
					superpacket.SetFlag(SuperPacketFlags.Handshake);
					superpacket.SetInternalFlag(SuperPacketInternalFlags.WaitFirst);
				}
			}
			else
			{
				superpacket.ClearInternalFlag(SuperPacketInternalFlags.WaitFirst);
			}

			// Ack packets
			foreach (ushort ack in reader.getAcks())
			{
				superpacket.Ack(ack);
			}

			// Schedule ack if necessary
			if (is_handshake || reader.hasData() || reader.isPingPacket())
			{
				superpacket.scheduleAck(reader.id());
			}
		}

		private void read_impl(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal marshal)
		{
			SuperPacketReader reader = client.popPendingSuperPacket();

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

				// Either case, skip all processing
				lastBlockIdRead = reader.id();
				return;
			}

			Debug.Assert(!IsOutOfOrder(reader.id()), "Should never have out of order packets");
			
			if (Overflow.sub(expectedBlockId, reader.id()) > Constants.MaximumBlocksUntilResync)
			{
				superpacket.SetFlag(SuperPacketFlags.Handshake);
			}

			if (lastBlockIdRead > reader.id())
			{
				loopCounter = (byte)(loopCounter + 1);
			}

			lastBlockIdRead = reader.id();
			reader.handlePackets<PQ, IBaseClient>(this, marshal, client);
		}

		public bool IsOutOfOrder(ushort id)
		{
			if (Constants.UseKumoQueues)
			{
				return Overflow.le(id, Overflow.sub(lastBlockIdRead, bufferSize));
			}

			return Overflow.le(id, lastBlockIdRead);
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
