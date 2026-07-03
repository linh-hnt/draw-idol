using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class ArrayExtension
    {
        public static T Find<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.Find(items, predicate);
        }

        public static T[] FindAll<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.FindAll(items, predicate);
        }

        public static void Shuffle<T>(this T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                --n;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }

        public static T RandomItem<T>(this T[] arr)
        {
            return arr[UnityEngine.Random.Range(0, arr.Length)];
        }
    }
}
