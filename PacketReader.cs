using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public class PacketReader : Packet
	{
		private ulong _timestamp;

		public PacketReader(Buffer buffer, ulong timestamp) : base(buffer)
		{ 
			_timestamp = timestamp;
		}

		public ulong timestamp()
		{
			return _timestamp;
		}

		public int bytesRead()
		{
			return buffer.getPosition();
		}

		public int bufferSize()
		{
			return buffer.getSize();
		}
	}
}
