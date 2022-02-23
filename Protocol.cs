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
            public HashSet<uint> packetCounters;

            public ResolvedBlock(byte loopCounter)
            {
                this.loopCounter = loopCounter;
                this.packetCounters = new HashSet<uint>();
            }
        }

        public ushort ExpectedTickId { get; private set; }
        public ushort LastServerId { get; private set; }
        public ushort LastTickIdRead { get;private set; }
        public float ServerTimeDiff { get; private set; }

        private ushort bufferSize;
        private ushort sinceLastPing;
        private float estimatedRTT;
        private ushort sinceLastRecv;
        private bool serverBasedSync;
        private byte loopCounter;
        private ulong timestamp;
        private ushort timestampBlockId;
        private float recvKbpsEstimate;
        private float recvKbpsEstimateAcc;
        private ulong recvKbpsEstimateTime;
        private float sendKbpsEstimate;
        private float sendKbpsEstimateAcc;
        private ulong sendKbpsEstimateTime;
        private uint lastRecvSize;
        private ConcurrentDictionary<ushort, uint> perTickSize;
        private uint lastSendSize;
        private ServerPhaseSync<PQ> phaseSync;

        // Resolution
        const ushort ResolutionTableSize = 200 * 4;
        const ushort ResolutionTableDiff = ResolutionTableSize - 1;
        private ulong[] resolutionTable;
        private ushort oldestResolutionBlockId;
        private ushort oldestResolutionPosition;

        private ulong[] timestamps;
        private ushort timestampsHeadPosition;
        private ushort timestampsHeadId;
        private ushort lastConfirmedTimestampId;

        public Protocol()
        {
            phaseSync = new ServerPhaseSync<PQ>(this);
            Reset();
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

        public uint getLastSentSuperPacketSize(SuperPacket<PQ> superpacket)
        {
            return lastSendSize;
        }

        public uint getLastRecvSuperPacketSize()
        {
            var size = lastRecvSize;
            lastRecvSize = 0;
            return size;
        }

        public float getPerTickSize()
        {
            float size = 0.0f;
            var count = perTickSize.Count;
            if (count == 0)
            {
                return 0.0f;
            }

            foreach (var pair in perTickSize)
            {
                size += pair.Value;
            }

            perTickSize.Clear();
            return size / count;
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
            LastTickIdRead = 0;
            ExpectedTickId = 0;
            serverBasedSync = true;
            LastServerId = 0;
            loopCounter = 0;
            timestamp = DateTimeExtensions.now();
            timestampBlockId = 0;
            recvKbpsEstimate = 0;
            recvKbpsEstimateAcc = 0;
            recvKbpsEstimateTime = DateTimeExtensions.now();
            sendKbpsEstimate = 0;
            sendKbpsEstimateAcc = 0;
            sendKbpsEstimateTime = DateTimeExtensions.now();
            perTickSize = new ConcurrentDictionary<ushort, uint>();

            resolutionTable = new ulong[ResolutionTableSize];
            oldestResolutionBlockId = 0;
            oldestResolutionPosition = 0;

            timestamps = new ulong[ResolutionTableSize];
            timestampsHeadPosition = 0;
            timestampsHeadId = 0;
            lastConfirmedTimestampId = 0;
        }

        public void InitiateHandshake(SuperPacket<PQ> superpacket)
        {
            superpacket.SetFlag(SuperPacketFlags.Handshake);
        }

        public Buffer update(ushort tickId, IBaseClient client, SuperPacket<PQ> superpacket)
        {
            ++sinceLastPing;
            if (needsPing())
            {
                sinceLastPing = 0;
                superpacket.SetFlag(SuperPacketFlags.Ping);
            }

            // TODO(gpascualg): Lock superpacket
            bool first_packet = true;
            superpacket.prepare();
            if (superpacket.finish(tickId, first_packet))
            {
                Buffer buffer = new Buffer(superpacket.getBuffer());

                // Register time for ping purposes
                ushort packetIdDiff = Overflow.sub(buffer.readUshort(2), timestampsHeadId);
                timestampsHeadPosition = Overflow.mod(Overflow.add(timestampsHeadPosition, packetIdDiff), ResolutionTableSize);
                timestamps[timestampsHeadPosition] = DateTimeExtensions.now();
                timestampsHeadId = buffer.readUshort(2);

                // Update estimate
                sendKbpsEstimateAcc += buffer.getPosition();
                lastSendSize = (ushort)buffer.getPosition();

                first_packet = false;

                return buffer;
            }

            lastSendSize = 0;
            return null;
        }

        private bool needsPing()
        {
            return sinceLastPing >= 20;
        }

        public bool read(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal marshal)
        {
            timestampBlockId = ExpectedTickId;
            timestamp = DateTimeExtensions.now();

            if (!client.hasPendingSuperPackets())
            {
                if (++sinceLastRecv >= Constants.MaxBlocksUntilDisconnection)
                {
                    client.disconnect();
                    return false;
                }

                marshal.Update(client, ExpectedTickId);
                ExpectedTickId = Overflow.inc(ExpectedTickId);
                return false;
            }

            sinceLastRecv = 0;
            ushort expectedId = ExpectedTickId;
            if (!Constants.UseKumoQueues)
            {
                ExpectedTickId = Overflow.sub(ExpectedTickId, bufferSize);
            }

            while (client.hasPendingSuperPackets() &&
                    !Overflow.ge(client.firstSuperPacketTickId(), expectedId))
            {
                read_impl(client, superpacket, marshal);
            }

            marshal.Update(client, ExpectedTickId);
            ExpectedTickId = Overflow.inc(ExpectedTickId);
            return true;
        }

        public void HandleServerTick(SuperPacketReader reader, SuperPacket<PQ> superpacket)
        {
            // Update sizes
            recvKbpsEstimateAcc += reader.size();
            lastRecvSize += reader.size();
            perTickSize.AddOrUpdate(reader.tickId(), reader.size(), (key, old) => old + reader.size());

            // Check handshake status
            if (!superpacket.HasFlag(SuperPacketFlags.Handshake))
            {
                LastServerId = Overflow.max(LastServerId, reader.tickId());

                // Update lag estimation
                foreach (ushort ack in reader.getAcks())
                {
                    if (Overflow.geq(lastConfirmedTimestampId, ack) && Overflow.sub(timestampsHeadId, lastConfirmedTimestampId) < 100)
                    {
                        continue;
                    }

                    lastConfirmedTimestampId = ack;
                    ushort position = Overflow.mod(Overflow.sub(timestampsHeadPosition, Overflow.sub(timestampsHeadId, ack)), ResolutionTableSize);
                    ulong diff = DateTimeExtensions.now() - timestamps[position];
                    const float w = 0.99f;
                    estimatedRTT = estimatedRTT * w + diff * (1.0f - w);
                }

                // TODO(gpascualg): Make phase sync id diff optional
                int idDiff = Overflow.abs_diff(phaseSync.TickId, LastServerId);
                int sign = Overflow.ge(phaseSync.TickId, LastServerId) ? 1 : -1;
                ServerTimeDiff = sign * idDiff - (estimatedRTT / 50.0f + 1); //- (int)(estimatedRTT / 2.0f);
            }
            else
            {
                // Fix phase sync, otherwise we will get a huge spike
                LastServerId = reader.tickId();
                if (serverBasedSync)
                {
                    phaseSync.FixTickId(LastServerId);
                }
            }

            // Update PLL
            phaseSync.ServerPacket(reader.tickId(), LastServerId);
        }

        public void HandleAcks(ushort tickId, SuperPacketReader reader, SuperPacket<PQ> superpacket, IMarshal marshal)
        {
            // Ack packets
            foreach (ushort ack in reader.getAcks())
            {
                superpacket.Ack(ack);
            }

            // Schedule ack if necessary
            bool is_handshake = reader.HasFlag(SuperPacketFlags.Handshake);
            if (is_handshake || reader.hasData() || reader.isPingPacket())
            {
                superpacket.scheduleAck(reader.id());
            }

            // Handle flags already
            if (is_handshake)
            {
                // Check if there was too much of a difference, in which case, flag handshake again
                // TODO(gpascualg): Remove re-handshake max diff magic number
                if (Overflow.abs_diff(reader.tickId(), tickId) > 10)
                {
                    superpacket.SetFlag(SuperPacketFlags.Handshake);
                }

                // During handshake, we update our tick to match the other side
                ExpectedTickId = reader.tickId();
                LastServerId = reader.tickId();

                // Reset all variables related to packet parsing
                timestampBlockId = ExpectedTickId;
                timestamp = DateTimeExtensions.now();
                loopCounter = 0;

                // Reset marshal
                ResetResolutionTable(reader.tickId());
                marshal.Reset();

                if (!reader.HasFlag(SuperPacketFlags.Ack))
                {
                    superpacket.SetFlag(SuperPacketFlags.Ack);
                    superpacket.SetFlag(SuperPacketFlags.Handshake);
                }
            }
        }

        private void read_impl(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal marshal)
        {
            SuperPacketReader reader = client.popPendingSuperPacket();

            // Handshake process skips all procedures, including order
            if (reader.HasFlag(SuperPacketFlags.Handshake))
            {
                // Nothing to do here, it's a handshake packet
                // TODO(gpascualg): We don't need to add them at all
                LastTickIdRead = reader.tickId();
                return;
            }

            Debug.Assert(!IsOutOfOrder(reader.tickId()), "Should never have out of order packets");

            if (Overflow.sub(ExpectedTickId, reader.tickId()) > Constants.MaximumBlocksUntilResync)
            {
                superpacket.SetFlag(SuperPacketFlags.Handshake);
            }

            if (LastTickIdRead > reader.tickId())
            {
                loopCounter = (byte)(loopCounter + 1);
            }

            LastTickIdRead = reader.tickId();
            reader.handlePackets<PQ, IBaseClient>(this, marshal, client);
        }

        public bool IsOutOfOrder(ushort id)
        {
            if (Constants.UseKumoQueues)
            {
                return Overflow.le(id, Overflow.sub(LastTickIdRead, bufferSize));
            }

            return Overflow.le(id, LastTickIdRead);
        }

        public bool resolve(PacketReader packet, ushort blockId)
        {
            // Check if this is an older block id
            if (Overflow.le(blockId, oldestResolutionBlockId))
            {
                ResetResolutionTable(blockId);
                //client->flag_desync();
                return false;
            }

            // Otherwise, it might be newer
            ushort diff = Overflow.sub(blockId, oldestResolutionBlockId);
            ushort idx = (ushort)(Overflow.add(oldestResolutionPosition, diff) % ResolutionTableSize);
            if (diff >= ResolutionTableSize)
            {

                // We have to move oldest so that newest points to blockId
                ushort move_amount = Overflow.sub(diff, ResolutionTableDiff);
                oldestResolutionBlockId = Overflow.add(oldestResolutionBlockId, move_amount);
                oldestResolutionPosition = (ushort)(Overflow.add(oldestResolutionPosition, move_amount) % ResolutionTableSize);

                // Fix diff so we don't overrun the new position
                idx = (ushort)(Overflow.add(oldestResolutionPosition, Overflow.sub(diff, move_amount)) % ResolutionTableSize);

                // Clean position, as it is a newer packet that hasn't been parsed yet
                resolutionTable[idx] = 0;
            }

            // Compute packet mask
            ulong mask = (ulong)(1) << packet.getCounter();

            // Get blockId position, bitmask, and compute
            if ((resolutionTable[idx] & mask) != 0)
            {
                // The packet is already in
                return false;
            }

            resolutionTable[idx] |= mask;
            return true;
        }

        public void ResetResolutionTable(ushort blockId)
        {
            Array.Clear(resolutionTable, 0, resolutionTable.Length);
            oldestResolutionBlockId = Overflow.sub(blockId, ResolutionTableSize / 2);
            oldestResolutionPosition = 0;
        }
    }
}
