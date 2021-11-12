using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class ImmediatePacker : Packer<Packet, IData>
	{
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
		}

		public override void add(IMarshal marshal, ushort opcode, IData data, Action callback)
		{
			Packet packet = Packet.make(opcode, callback);
			data.pack(marshal, packet);
			add(packet);
		}

		public override void process(IMarshal marshal, ushort tickId, ushort blockId, ref ushort remaining, ref bool unfittingData, SortedDictionary<uint, List<Packet>> byBlock)
		{
			foreach (PendingData<Packet> pnd in pending)
			{
				if (!isPending(pnd.InternalTickList, tickId, false))
				{
					continue;
				}

				uint actualBlock = getActualTickId(pnd.InternalTickList, tickId);
				ushort size = (ushort)pnd.data.getSize();

				if (byBlock.ContainsKey(actualBlock))
				{
					if (size > remaining)
					{
						unfittingData = true;
						break;
					}

					byBlock[actualBlock].Add(pnd.data);
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
					byBlock[actualBlock].Add(pnd.data);
				}

				pnd.InternalTickList.Add(tickId);
				pnd.ClientAckIds.Add(blockId);
				remaining = (ushort)(remaining - size);
			}
		}
	}
}
