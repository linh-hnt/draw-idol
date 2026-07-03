using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class IListExtensions
    {
        /// <summary>
        /// Shuffle the list in place using the Fisher-Yates method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                --n;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Return a random item from the list.
        /// Sampling with replacement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RandomItem<T>(this IList<T> list)
        {
#if UNITY_EDITOE
            if (list.Count == 0) throw new System.IndexOutOfRangeException("Cannot select a random item from an empty list");
#endif
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Removes a random item from the list, returning that item.
        /// Sampling without replacement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RemoveRandom<T>(this IList<T> list)
        {
#if UNITY_EDITOR
            if (list.Count == 0) throw new System.IndexOutOfRangeException("Cannot remove a random item from an empty list");
#endif
            int index = UnityEngine.Random.Range(0, list.Count);
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static bool IsNullOrEmpty(this IEnumerable @this)
        {
            if (@this != null)
            {
                return !@this.GetEnumerator().MoveNext();
            }

            return true;
        }

        /// <summary>
        /// Check null of list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsNull<T>(this IList<T> list)
        {
            return list == null;
        }

        /// <summary>
        /// Check empty of list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this IList<T> list)
        {
            return list.Count <= 0;
        }

        /// <summary>
        /// Check element is out of list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsIndexOutOfList<T>(this IList<T> list, int index)
        {
            return (index < 0) || (index >= list.Count);
        }

        public static void Resize<T>(this List<T> list, int sz, T c)
        {
            int cur = list.Count;
            if (sz < cur)
                list.RemoveRange(sz, cur - sz);
            else if (sz > cur)
            {
                if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                    list.Capacity = sz;

                for (int i = 0, length = sz - cur; i < length; ++i)
                {
                    list.Add(c);
                }
            }
        }
        public static void Resize<T>(this List<T> list, int sz)
        {
            Resize(list, sz, default);
        }
    }
}