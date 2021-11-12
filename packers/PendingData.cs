using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class PendingData<T>
	{
		public T data;
		public List<ushort> InternalTickList;
		public List<ushort> ClientAckIds;

		public PendingData(T data)
		{
			this.data = data;
			this.InternalTickList = new List<ushort>();
			this.ClientAckIds = new List<ushort>();
		}
	}
}
