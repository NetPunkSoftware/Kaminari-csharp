using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Kaminari
{
	public class MostRecentPackerWithId : Packer<PacketWithId, IHasId>
	{

		private uint opcode;
		private Dictionary<ulong, PendingData<PacketWithId>> idMap = new Dictionary<ulong, PendingData<PacketWithId>>();

		public override void onAck(List<PendingData<PacketWithId>> toBeRemoved)
		{
			foreach (PendingData<PacketWithId> pending in toBeRemoved)
			{
				idMap.Remove(pending.data.id);
			}
		}

		public override void onClear()
		{
			idMap.Clear();
		}

		public override void add(Packet packet)
		{
			Debug.Assert(false, "Unsupported operation");
		}

		public override void add(IMarshal marshal, ushort opcode, IHasId data, Action callback)
		{
			Debug.Assert(false, "Unsupported operation");
		}

		private void add(Packet packet, ulong id)
		{
			if (idMap.ContainsKey(id))
			{
				idMap[id].data.packet = packet;
				idMap[id].blocks.Clear();
			}
			else
			{
				PendingData<PacketWithId> pendingData = new PendingData<PacketWithId>(new PacketWithId(packet, id));
				pending.Add(pendingData);
			}
		}

		public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
		{
			foreach (PendingData<PacketWithId> pnd in pending)
			{
				if (!isPending(pnd.blocks, blockId, false))
				{
					continue;
				}
				
				uint actualBlock = getActualBlock(pnd.blocks, blockId);
				ushort size = (ushort)pnd.data.packet.getSize();

				if (byBlock.ContainsKey(actualBlock))
				{
					if (size > remaining)
					{
						break;
					}

					byBlock[actualBlock].Add(pnd.data.packet);
				}
				else
				{
					size = (ushort)(size + 4);
					if (size > remaining)
					{
						break;
					}

					byBlock.Add(actualBlock, new List<Packet>());
					byBlock[actualBlock].Add(pnd.data.packet);
				}

				pnd.blocks.Add(blockId);
				remaining = (ushort)(remaining - size);
			}
		}
	}
}
