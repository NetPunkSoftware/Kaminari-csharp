using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class Packet
	{
		public const int MAX_PACKET_SIZE = 255;

		public const byte opcode_position = 0;
		public const ushort opcode_mask = 0xFFFF & (~((1 << 0) | (1 << 1) | (1 << 2) | (1 << 3)));
    
		public const byte counter_position = 1;
		public const ushort counter_mask = 0xFFFF 
        & (~((1 << 15) | (1 << 14) | (1 << 13) | (1 << 12))) // Higher-most 4 bits are from opcode
        & (~((1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5))); // Lowest-most 6 bits are from diff
		public const ushort counter_shift = 6;

		public const byte header_unshifted_flags_position = 2;

		public const byte DataStart = (header_unshifted_flags_position + 1) * sizeof(byte);

		public Action onAcked;
		protected Buffer buffer;

		public static Packet make(ushort opcode)
		{
			return new Packet(opcode);
		}

		// FIXME(gpascualg): Packets with ack callbacks
		public static Packet make(ushort opcode, Action onAcked)
		{
			Packet packet = Packet.make(opcode);
			packet.onAcked = onAcked;
			return packet;
		}

		public Packet(ushort opcode)
		{
			buffer = new Buffer();
			buffer.write(0, opcode);
		}

		public Packet(Buffer buffer)
		{
			this.buffer = buffer;
		}

		public Buffer getData()
		{
			return buffer;
		}

		public ushort getOpcode()
		{
			return (ushort)(buffer.readUshort(opcode_position) & opcode_mask);
		}

		public byte getCounter()
		{
			byte low = (byte)(buffer.readUshort(0) & 0x0F);
			byte high = (byte)((buffer.readUshort(2) & 0xC0) >> 2);
			return (byte)(high | low);
		}

		public uint getExtendedId()
        {
			return buffer.readUint(0) & 0x00C0FFFF;
		}

		public byte getOffset()
		{
			return (byte)(buffer.readByte(header_unshifted_flags_position) & (~((1 << 7) | (1 << 6))));
		}

		public byte getSize()
		{
			return (byte)buffer.getPosition();
		}

		public void finish(byte counter)
		{
			// We must take into account that OPCODE (HHL0) gets shifted in memory to L0HH
			buffer.write(0, (byte)(buffer.readByte(0) | (byte)(counter & 0x0F)));
			buffer.write(2, (byte)(buffer.readByte(2) | (byte)((counter & 0x30) << 2)));
		}
	}
}
