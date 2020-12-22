using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasDataVector<T> : IData
{
	void initialize();
	List<T> getData();
}
