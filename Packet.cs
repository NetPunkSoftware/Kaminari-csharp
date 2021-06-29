using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class Packet
	{
		public Action onAcked;
		public static int dataStart = sizeof(byte) * 2 + sizeof(ushort) + sizeof(byte);

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
			buffer.write(2, opcode);
		}

		public Packet(Buffer buffer)
		{
			this.buffer = buffer;
		}

		public Buffer getData()
		{
			return buffer;
		}

		public byte getLength()
		{
			return this.buffer.readByte(0);
		}

		public ushort getOpcode()
		{
			return buffer.readUshort(2);
		}

		public byte getId()
		{
			return buffer.readByte(1);
		}

		public byte getOffset()
		{
			return this.buffer.readByte(5);
		}

		public byte getSize()
		{
			return (byte)buffer.getPosition();
		}

		public void finish(byte counter)
		{
			buffer.write(0, (byte)buffer.getPosition());
			buffer.write(1, counter);
		}
	}
}
