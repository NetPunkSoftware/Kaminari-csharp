using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Packer<T, D> : IPacker<T, D> where D : IData
{
	
	protected List<PendingData<T>> pending = new List<PendingData<T>>(25);
	
	public abstract void onAck(List<PendingData<T>> toBeRemoved);

	public abstract void onClear();

	public abstract void add(Packet packet);

	public abstract void add(IMarshal marshal, ushort opcode, D data, Action callback);
	
	public abstract void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock);

	public void ack(ushort blockId) 
	{
		List<PendingData<T>> toFind = new List<PendingData<T>>();

		for(int i = pending.Count -1; i >= 0; --i)
		{
			PendingData<T> p = pending[i];

			foreach (ushort s in p.blocks)
			{
				if(s == blockId) 
				{
					toFind.Add(p);
					pending.RemoveAt(i);
					break;
				}
			}
		}

		onAck(toFind);
	}


	public void clear()
	{
		this.onClear();
		pending.Clear();
	}

	public bool isPending(List<ushort> blocks, ushort blockId, bool force) 
	{
		if(blocks.Count != 0 && blocks[blocks.Count - 1] == blockId) {
			return false;
		}
		return force ||
				blocks.Count == 0 ||
				Overflow.sub(blockId, blocks[blocks.Count - 1]) >= Constants.ResendThreshold;
	}

	public ushort getActualBlock(List<ushort> blocks, ushort blockId) 
	{
		if (blocks.Count != 0) 
		{
			blockId = blocks[0];
		}

		return blockId;
	}

	public ushort newBlockCost(ushort blockId, SortedDictionary<uint, List<Packet>> byBlock) 
	{
		if (byBlock.ContainsKey(blockId)) 
		{
			return 0;
		}
		return 4;
	}

	public void removeByCount(ushort count) 
	{
	}
}
