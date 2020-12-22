using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReliableQueueByOpcode<P> : ReliableQueueBase<P, PacketWithOpcode, IData> where P : IPacker<PacketWithOpcode, IData>
{
	public ReliableQueueByOpcode(P packer) : base(packer)
	{}
}
