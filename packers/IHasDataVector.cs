using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public interface IHasDataVector<T> : IData
	{
		void initialize();
		List<T> getData();
	}
}
