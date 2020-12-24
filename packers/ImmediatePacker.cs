using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
		{
			foreach (PendingData<Packet> pnd in pending)
			{
				if (!isPending(pnd.blocks, blockId, false))
				{
					continue;
				}

				uint actualBlock = getActualBlock(pnd.blocks, blockId);
				ushort size = (ushort)pnd.data.getSize();

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
					size = (ushort)(size + 4);
					if (size > remaining)
					{
						break;
					}

					byBlock.Add(actualBlock, new List<Packet>());
					byBlock[actualBlock].Add(pnd.data);
				}

				pnd.blocks.Add(blockId);
				remaining = (ushort)(remaining - size);
			}
		}
	}
}
