using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public class ReliableQueueWithId<P> : ReliableQueueBase<P, PacketWithId, IData> where P : IPacker<PacketWithId, IData>
	{
		public ReliableQueueWithId(P packer) : base(packer)
		{ }
	}
}
