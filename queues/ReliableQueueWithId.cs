using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReliableQueueWithId<P> : ReliableQueueBase<P, PacketWithId, IData> where P : IPacker<PacketWithId, IData>
{
	public ReliableQueueWithId(P packer) : base(packer)
	{ }
}
