using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventuallySyncedQueue<P, D> where P : IPacker<D, D> where D : IData
{
	private P packer;

	public EventuallySyncedQueue(P packer)
	{
		this.packer = packer;
	}

	public void add(IMarshal marshal, ushort opcode, D data, IAckCallback callback)
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
	}

	public void ack(ushort blockId)
	{
		packer.ack(blockId);
	}

	public void clear()
	{
		packer.clear();
	}
}
