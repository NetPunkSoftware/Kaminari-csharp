using System;
using System.Collections.Generic;

namespace Kaminari
{
    public class SyncList<T> : IEnumerable<T> where T : class
    {
        protected List<T> list = new List<T>();

        public int Count => list.Count;

        // Other Elements of IList implementation
        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        protected static object _lock = new object();

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return list[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    list[index] = value;
                }
            }
        }

        public List<T> Clone()
        {
            List<T> newList = new List<T>();

            lock (_lock)
            {
                list.ForEach(x => newList.Add(x));
            }

            return newList;
        }

        public void Add(T element)
        {
            lock (_lock)
            {
                list.Add(element);
            }
        }

        public void RemoveAll(Predicate<T> match)
        {
            lock (_lock)
            {
                list.RemoveAll(match);
            }
        }

        public void RemoveAllButLast()
        {
            lock (_lock)
            {
                list.RemoveRange(0, list.Count - 1);
            }
        }

        public T Last()
        {
            lock (_lock)
            {
                return list.Last();
            }
        }

        public bool PeekLast(out T obj)
        {
            obj = null;

            lock (_lock)
            {
                if (Count > 0)
                {
                    obj = list[Count - 1];
                    return true;
                }
            }

            return false;
        }
    }
}
