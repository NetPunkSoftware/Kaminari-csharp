using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kaminari
{
    public enum SuperPacketFlags
    {
        None        = 0x00,
        Handshake   = 0x01,
        Ack         = 0x80,
        All         = 0xFF
    }

    public class SuperPacket<PQ> where PQ : IProtocolQueues
    {
        public ushort _id;
        private byte _flags;
        private List<ushort> _pendingAcks;
        private Dictionary<ushort, byte> _clearFlagsOnAck;
        private Buffer _buffer;
        private PQ _queues;

        public Buffer getBuffer()
        {
            return _buffer;
        }

        public SuperPacket(PQ queues)
        {
            _pendingAcks = new List<ushort>();
            _buffer = new Buffer();
            _queues = queues;
            
            reset();
        }

        public void reset()
        {
            _id = 0;
            _flags = 0;
            _pendingAcks.Clear();
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
                _flags = _flags & (~_clearFlagsOnAck[id]);
            }
        }

        public void SetFlag(SuperPacketFlags flag)
        {
            _flags = _flags | (byte)flag;
            _clearFlagsOnAck.Add(_id, _clearFlagsOnAck.GetValueOrDefault(_id) | (byte)flag);
        }

        // public void ClearFlag(SuperPacketFlags flag)
        // {
        //     _flags = _flags & (~(byte)flag);
        // }

        public bool HasFlag(SuperPacketFlags flag)
        {
            return (_flags & (byte)flag) != 0x00;
        }

        public PQ getQueues()
        {
            return _queues;
        }

        public void scheduleAck(ushort blockId)
        {
            _pendingAcks.Add(blockId);
        }

        public void serverUpdatedId(ushort id)
        {
            _id = Math.Max(id, _id);
        }

        public bool finish()
        {
            _buffer.reset();

            //  First two bytes are size, next two id, finally 1 byte for flags
            _buffer.write((short)0);
            _buffer.write(_id);
            _buffer.write(_flags);

            _buffer.write((byte)_pendingAcks.Count);
            bool hasAcks = _pendingAcks.Count > 0;
            foreach (ushort ack in _pendingAcks)
            {
                _buffer.write(ack);
            }

            // Clear acks
            _pendingAcks.Clear();

            //  -1 is to account for the number of blocks
            ushort remaining = (ushort)(500 - _buffer.getPosition() - 1);

            // During handshake/resync do not include any packets
            bool hasData = false;
            if (!HasFlag(SuperPacketFlags.Handshake))
            {
                // Organize packets that must be resent until ack'ed
                SortedDictionary<uint, List<Packet>> by_block = new SortedDictionary<uint, List<Packet>>();

                _queues.process(_id, ref remaining, by_block);

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

                    byte counter = 0;

                    // Write in packets
                    foreach (var entry in by_block)
                    {
                        _buffer.write((ushort)Convert.ToUInt16(entry.Key & 0xffff));
                        _buffer.write((byte)entry.Value.Count);

                        foreach (var packet in entry.Value)
                        {
                            if (entry.Key == _id)
                            {
                                packet.finish(counter++);
                            }

                            _buffer.write(packet);
                        }
                    }
                }

                // Id only increases outside handshakes
                ++_id;
            }

            _buffer.write(0, (byte)_buffer.getPosition());
            return _flags != 0 || hasAcks || hasData;
        }
    }
}
