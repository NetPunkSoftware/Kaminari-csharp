using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderedPacker : Packer<Packet, IData>
{
	protected bool hasNewPacket;
	protected ushort lastBlock;
	
	
	public override void onAck(List<PendingData<Packet>> toBeRemoved)
	{
		// Do nothing on purpose
	}

	public override void onClear()
	{
		// Do nothing on purpose
	}

	public override void add(Packet packet)
	{
		pending.Add(new PendingData<Packet>(packet));
		hasNewPacket = true;
	}

	public override void add(IMarshal marshal, ushort opcode, IData data, IAckCallback callback)
	{
		Packet packet = Packet.make(opcode, callback);
		data.pack(marshal, packet);
		add(packet);
	}

	public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
	{
		if (!isPending(blockId)) 
		{
			return;
		}
		
		int numInserted = 0;
		foreach (PendingData<Packet> pnd in pending) 
		{
			uint actualBlock = getActualBlock(pnd.blocks, blockId);
			ushort size = pnd.data.getSize();
			
			if (byBlock.ContainsKey(actualBlock)) 
			{
				if (size > remaining) 
				{
					break;
				}
				
				byBlock[actualBlock].Add(pnd.data);
			}
			else 
			{
				size = (ushort) (size + 4);
				if (size > remaining)
				{
					break;
				}
				
				byBlock.Add(actualBlock, new List<Packet>());
				byBlock[actualBlock].Add(pnd.data);
			}
			
			pnd.blocks.Add(blockId);
			remaining = (ushort) (remaining - size);
			++numInserted;
		}
		
		if (numInserted > 0)
		{
			hasNewPacket = false;
			lastBlock = blockId;
		}
	}
	
	protected bool isPending(ushort blockId)
	{
		if (pending.Count == 0) 
		{
			return false;
		}
		
		return hasNewPacket ||
				Overflow.sub(blockId, lastBlock) >= Constants.ResendThreshold;
	}
}
