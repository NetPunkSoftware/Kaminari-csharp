using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
				PendingData<PacketWithId> pending = new PendingData<PacketWithId>(new PacketWithId(packet, id));
			}
		}

		public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
		{}
	}
}
