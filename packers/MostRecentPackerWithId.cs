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
				idMap[id].InternalTickList.Clear();
				idMap[id].ClientAckIds.Clear();
			}
			else
			{
				PendingData<PacketWithId> pendingData = new PendingData<PacketWithId>(new PacketWithId(packet, id));
				pending.Add(pendingData);
			}
		}

		public override void process(IMarshal marshal, ushort tickId, ushort blockId, ref ushort remaining, ref bool unfittingData, SortedDictionary<uint, List<Packet>> byBlock)
		{
			foreach (PendingData<PacketWithId> pnd in pending)
			{
				if (!isPending(pnd.InternalTickList, tickId, false))
				{
					continue;
				}
				
				uint actualBlock = getActualTickId(pnd.InternalTickList, tickId);
				ushort size = (ushort)pnd.data.packet.getSize();

				if (byBlock.ContainsKey(actualBlock))
				{
					if (size > remaining)
					{
						unfittingData = true;
						break;
					}

					byBlock[actualBlock].Add(pnd.data.packet);
				}
				else
				{
					size = (ushort)(size + 4);
					if (size > remaining)
					{
						unfittingData = true;
						break;
					}

					byBlock.Add(actualBlock, new List<Packet>());
					byBlock[actualBlock].Add(pnd.data.packet);
				}

				pnd.InternalTickList.Add(tickId);
				pnd.ClientAckIds.Add(blockId);
				remaining = (ushort)(remaining - size);
			}
		}
	}
}
