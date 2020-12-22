using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReliableQueue<P, T> : ReliableQueueBase<P, T, IData> where P : IPacker<T, IData>
{
	public ReliableQueue(P packer) : base(packer)
	{}
}
