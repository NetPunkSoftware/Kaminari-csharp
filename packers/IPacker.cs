using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public interface IPacker<T, D> where D : IData
	{
		void onAck(List<PendingData<T>> toBeRemoved);
		void onClear();
		void add(Packet packet);
		void add(IMarshal marshal, ushort opcode, D data, Action callback);
		void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock);
		void ack(ushort blockId);
		void clear();
		bool isPending(List<ushort> blocks, ushort blockId, bool force);
		ushort getActualBlock(List<ushort> blocks, ushort blockId);
		ushort newBlockCost(ushort blockId, SortedDictionary<uint, List<Packet>> byBlock);
		void removeByCount(ushort count);
	}
}
