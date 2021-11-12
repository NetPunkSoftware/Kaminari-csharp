using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class EventuallySyncedQueue<P, D> where P : IPacker<D, D> where D : IData
	{
		private P packer;

		public EventuallySyncedQueue(P packer)
		{
			this.packer = packer;
		}

		public void add(IMarshal marshal, ushort opcode, D data, Action callback)
		{
			packer.add(marshal, opcode, data, callback);
		}

		public void add(Packet packet)
		{
			packer.add(packet);
		}

		public void process(IMarshal marshal, ushort tickId, ushort blockId, ref ushort remaining, ref bool unfittingData, SortedDictionary<uint, List<Packet>> byBlock)
		{
			packer.process(marshal, tickId, blockId, ref remaining, ref unfittingData, byBlock);
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
}
