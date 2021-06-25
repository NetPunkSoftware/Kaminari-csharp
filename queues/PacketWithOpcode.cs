using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public class PacketWithOpcode
    {
        public Packet packet;
        public ushort opcode;

		public PacketWithOpcode(Packet packet, ushort opcode)
		{
			this.packet = packet;
			this.opcode = opcode;
		}
    }
}
