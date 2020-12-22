using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PendingData<T>
{
	public T data;
	public List<ushort> blocks;

	public PendingData(T data)
	{
		this.data = data;
		this.blocks = new List<ushort>();
	}
}
