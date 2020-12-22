using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketReader : Packet
{
	public PacketReader(Buffer buffer) : base(buffer)
	{}

	public long timestamp()
	{
		return 0;
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
