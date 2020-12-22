using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Optional<T>
{
	private T value;
	private bool empty;

	Optional()
	{
		empty = true;
	}

	public bool hasValue()
	{
		return !empty;
	}

	public T getValue()
	{
		return value;
	}

	public void setValue(T value)
	{
		Debug.Assert(empty, "Optional already has value");
		this.value = value;
	}
}
