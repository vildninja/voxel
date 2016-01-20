using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace VildNinja.Utils
{
    /// <summary>
    /// A HashSet capable of running in foreach, without generating garbage.
    /// Do note that it needs to check the entire allocated array when iterating
    /// (skipping all empty fields)
    /// </summary>
    /// <typeparam name="T">Type of item</typeparam>
    public class HashList<T> : IEquatable<HashList<T>>, IEnumerable<T> where T : IEquatable<T>
    {

        private class HashListEnumerator<E> : IEnumerator<E> where E : IEquatable<E>
        {
            public HashList<E> list;

            private int index;

            public bool MoveNext()
            {
                while (index < list.open.Length - 1)
                {
                    index++;
                    if (list.open[index] == 2)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            public E Current { get { return list.array[index]; } }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                enumerators[typeof(E)].Add(this);
            }
        }

        private static readonly Dictionary<Type, List<object>> enumerators = new Dictionary<Type, List<object>>();  

        private T[] array;
        private byte[] open;
        private int count;

        public int Count
        {
            get { return count; }
        }

        public HashList(int capacity = 32)
        {
            array = new T[capacity];
            open = new byte[capacity];
            count = 0;
        }

        public int AddRange(IEnumerable<T> range)
        {
            int added = 0;

            var iterator = range.GetEnumerator();
            while (iterator.MoveNext())
            {
                if (Add(iterator.Current))
                {
                    added++;
                }
            }

            return added;
        }

        public bool Add(T item)
        {
            if (count * 1.4f > array.Length)
            {
                Expand();
            }

            int hash = item.GetHashCode();
            if (hash < 0)
                hash += int.MaxValue;

            hash = hash % array.Length;

            while (true)
            {
                if (open[hash] < 2)
                {
                    array[hash] = item;
                    open[hash] = 2;
                    count++;
                    return true;
                }

                if (array[hash].Equals(item))
                {
                    return false;
                }

                hash++;
                if (hash >= array.Length)
                {
                    hash = 0;
                }
            }
        }

        public bool Remove(T item)
        {
            int hash = item.GetHashCode();
            if (hash < 0)
                hash += int.MaxValue;

            hash = hash % array.Length;

            while (true)
            {
                if (open[hash] == 0)
                {
                    return false;
                }

                if (open[hash] == 2 && array[hash].Equals(item))
                {
                    open[hash] = 1;
                    array[hash] = default(T);
                    count--;
                    return true;
                }

                hash++;
                if (hash >= array.Length)
                {
                    hash = 0;
                }
            }
        }
        
        public bool Contains(T item)
        {
            int hash = item.GetHashCode();
            if (hash < 0)
                hash += int.MaxValue;

            hash = hash % array.Length;

            while (true)
            {
                if (open[hash] == 0)
                {
                    return false;
                }

                if (open[hash] == 2 && array[hash].Equals(item))
                {
                    return true;
                }

                hash++;
                if (hash >= array.Length)
                {
                    hash = 0;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (open[i] == 1)
                {
                    open[i] = 0;
                }
                else if (open[i] == 2)
                {
                    open[i] = 0;
                    array[i] = default(T);
                }
            }

            count = 0;
        }

        private void Expand()
        {
            var oa = array;
            var oo = open;

            array = new T[array.Length * 2];
            open = new byte[array.Length];

            for (int i = 0; i < oa.Length; i++)
            {
                if (oo[i] == 2)
                {
                    Add(oa[i]);
                }
            }
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            List<object> list;
            if (!enumerators.TryGetValue(typeof (T), out list))
            {
                list = new List<object>();
                enumerators.Add(typeof(T), list);
            }

            HashListEnumerator<T> enumerator;
            if (list.Count > 0)
            {
                enumerator = (HashListEnumerator<T>)list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }
            else
            {
                enumerator = new HashListEnumerator<T>();
            }

            enumerator.Reset();
            enumerator.list = this;
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(HashList<T> other)
        {
            return this == other;
        }
    }
}