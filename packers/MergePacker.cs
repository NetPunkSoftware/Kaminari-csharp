using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public class MergePacker<G, D> : Packer<D, D> where G : IHasDataVector<D>, new() where D : IHasId
	{
		private ushort _opcode;

		public MergePacker(ushort opcode)
		{
			_opcode = opcode;
		}

		public override void onAck(List<PendingData<D>> toBeRemoved)
		{
			// Do nothing on purpose
		}

		public override void onClear()
		{
			// Do nothing on purpose
		}

		public override void add(Packet packet)
		{
			Debug.Assert(false, "Not supported");
		}

		public override void add(IMarshal marshal, ushort _opcode, D data, Action callback)
		{
			pending.Add(new PendingData<D>(data));
		}

		public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
		{
			if (pending.Count == 0)
			{
				return;
			}

			G global = new G();
			global.initialize();
			ushort size = (ushort)(6 + 2 + newBlockCost(blockId, byBlock));

			foreach (PendingData<D> pnd in pending)
			{
				if (isPending(pnd.blocks, blockId, false))
				{
					break;
				}

				ushort nextSize = (ushort)(size + pnd.data.size(marshal));
				if (nextSize > remaining)
				{
					break;
				}

				size = nextSize;
				global.getData().Add(pnd.data);
				pnd.blocks.Add(blockId);
			}

			if (global.getData().Count == 0)
			{
				return;
			}

			Packet packet = Packet.make(_opcode);
			global.pack(marshal, packet);
			remaining = (ushort)(remaining - size);

			if (byBlock.ContainsKey(blockId))
			{
				byBlock[blockId].Add(packet);
			}
			else
			{
				byBlock.Add(blockId, new List<Packet>());
				byBlock[blockId].Add(packet);
			}
		}
	}
}
