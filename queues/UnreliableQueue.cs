using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnreliableQueue<P, T> where P : IPacker<T, IData>
{
	private P packer;
	private ushort maxRetries = 0;

	public UnreliableQueue(P packer)
	{
		this.packer = packer;
	}

	public UnreliableQueue(P packer, ushort maxRetries)
	{
		this.packer = packer;
		this.maxRetries = maxRetries;
	}

	public void add(IMarshal marshal, ushort opcode, IData data, IAckCallback callback)
	{
		packer.add(marshal, opcode, data, callback);
	}

	public void add(Packet packet)
	{
		packer.add(packet);
	}

	public void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
	{
		packer.process(marshal, blockId, ref remaining, byBlock);

		if (maxRetries == 0)
		{
			packer.clear();
		}
		else
		{
			packer.removeByCount(maxRetries);
		}
	}

	public void ack(ushort blockId)
	{
		if (maxRetries > 0)
		{
			packer.ack(blockId);
		}
	}

	public void clear()
	{
		packer.clear();
	}
}
