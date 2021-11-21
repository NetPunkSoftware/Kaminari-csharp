using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Kaminari
{
    public enum SuperPacketFlags
    {
        None        = 0x00,
        Handshake   = 0x01,
        Ping        = 0x02,
        Ack         = 0x80,
        All         = 0xFF
    }

    public enum SuperPacketInternalFlags
    {
        None        = 0x00,
        WaitFirst   = 0x01,
        All         = 0xFF
    }

    public class SuperPacket<PQ> where PQ : IProtocolQueues
    {
        public const ushort MAX_SIZE = 512;

        public ushort _id;
        private byte _flags;
        private byte _internalFlags;
        private ushort _ackBase;
        private uint _pendingAcks;
        private bool _mustAck;
        private byte _counter;
        private Dictionary<ushort, byte> _clearFlagsOnAck;
        private Buffer _buffer;
        private PQ _queues;
        private bool _last_left_data;

        public Buffer getBuffer()
        {
            return _buffer;
        }

        public ushort getID()
        {
            return _id;
        }

        public SuperPacket(PQ queues)
        {
            _ackBase = 0;
            _pendingAcks = 0;
            _mustAck = false;
            _clearFlagsOnAck = new Dictionary<ushort, byte>();
            _buffer = new Buffer(MAX_SIZE);
            _queues = queues;
            
            reset();
        }

        public void reset()
        {
            _id = 0;
            _flags = 0;
            _internalFlags = 0;
            _ackBase = 0;
            _pendingAcks = 0;
            _mustAck = false;
            _buffer.reset();
            _queues.reset();
        }
        
        public void Ack(ushort id)
        {
            // Ack inside queues
            getQueues().ack(id);

            // Protocol level acks
            if (_clearFlagsOnAck.ContainsKey(id))
            {
                _flags = (byte)(_flags & (byte)(~_clearFlagsOnAck[id]));
                _clearFlagsOnAck.Remove(id);
            }
        }

        public void SetFlag(SuperPacketFlags flag)
        {
            _flags = (byte)(_flags | (byte)flag);

            if (_clearFlagsOnAck.ContainsKey(_id))
            {
                _clearFlagsOnAck[_id] = (byte)(_clearFlagsOnAck[_id] | (byte)flag);
            }
            else
            {
                _clearFlagsOnAck.Add(_id, (byte)flag);
            }
        }

        public void SetInternalFlag(SuperPacketInternalFlags flag)
        {
            _internalFlags = (byte)(_internalFlags | (byte)flag);
        }

        public void ClearInternalFlag(SuperPacketInternalFlags flag)
        {
            _internalFlags = (byte)(_internalFlags & (~(byte)flag));
        }

        public bool HasFlag(SuperPacketFlags flag)
        {
            return (_flags & (byte)flag) != 0x00;
        }

        public bool HasInternalFlag(SuperPacketInternalFlags flag)
        {
            return (_internalFlags & (byte)flag) != 0x00;
        }

        public PQ getQueues()
        {
            return _queues;
        }

        public void scheduleAck(ushort blockId)
        {
            if (Overflow.ge(blockId, _ackBase))
            {
                ushort ackDiff = Overflow.sub(blockId, _ackBase);
                _pendingAcks = (_pendingAcks << ackDiff) | 1; // | 1 because we are acking the base
                _ackBase = blockId;
            }
            else
            {
                ushort ackDiff = Overflow.sub(_ackBase, blockId);
                _pendingAcks = _pendingAcks | (ushort)(1 << ackDiff);
            }

            _mustAck = true;
        }

        public void prepare()
        {
            _counter = 0;
        }

        public bool finish(ushort tickId, bool isFirst)
        {
            _buffer.reset();

            //  First two bytes are tick id, next two id, finally 1 byte for flags
            _buffer.write(tickId);
            _buffer.write(_id);
            _buffer.write(_flags);

            // Reset if there is something we must ack
            bool hasAcks = _mustAck;
            _mustAck = false;

            // Write acks and move for next id
            // Moving acks bitset only happens if no doing handshake (ie. if incrementing id)
            _buffer.write(_ackBase);
            _buffer.write(_pendingAcks);

            //  -1 is to account for the number of blocks
            ushort remaining = (ushort)(MAX_SIZE - _buffer.getPosition() - 1 - 1);

            // During handshake/resync do not include any packets
            bool hasData = false;
            _last_left_data = false;
            if (!HasFlag(SuperPacketFlags.Handshake))
            {
                // Organize packets that must be resent until ack'ed
                SortedDictionary<uint, List<Packet>> by_block = new SortedDictionary<uint, List<Packet>>();

                _queues.process(tickId, _id, ref remaining, ref _last_left_data, by_block);

                // Write number of blocks
                _buffer.write((byte)by_block.Count);

                // Has there been any overflow? That can be detected by, for example
                //  checking max-min>thr
                hasData = by_block.Count != 0;
                if (hasData)
                {
                    uint max = by_block.Keys.ToList()[by_block.Count - 1]; // TODO(gpascualg): Linq .Last()?
                    ushort threshold = ushort.MaxValue / 2;

                    foreach (var key in by_block.Keys.ToList())
                    {
                        if (max - key < threshold)
                        {
                            break;
                        }

                        // Overflows are masked higher, so that they get pushed to the end
                        //  of the map
                        uint masked = (Convert.ToUInt32(1) << 16) | key;
                        by_block.Add(masked, by_block[key]);
                        by_block.Remove(key);
                    }

                    // Write in packets
                    foreach (var entry in by_block)
                    {
                        _buffer.write((ushort)Convert.ToUInt16(entry.Key & 0xffff));
                        _buffer.write((byte)entry.Value.Count);

                        UnityEngine.Assertions.Assert.AreNotEqual(entry.Value.Count, 0);
                        foreach (var packet in entry.Value)
                        {
                            if (entry.Key == _id)
                            {
                                packet.finish(_counter++);
                            }

                            _buffer.write(packet);
                        }
                    }
                }
            }

            // Increment _id for next iter and acks
            _id = Overflow.inc(_id);
            return hasAcks || hasData || (isFirst && _flags != 0);
        }
    }
}
