using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class ReliableQueueByOpcode<P> : ReliableQueueBase<P, PacketWithOpcode, IData> where P : IPacker<PacketWithOpcode, IData>
	{
		public ReliableQueueByOpcode(P packer) : base(packer)
		{ }
	}
}
