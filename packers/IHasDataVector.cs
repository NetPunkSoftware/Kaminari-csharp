using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public interface IHasDataVector<T> : IData
	{
		void initialize();
		List<T> getData();
	}
}
