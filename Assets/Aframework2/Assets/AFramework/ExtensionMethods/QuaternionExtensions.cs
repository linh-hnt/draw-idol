using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class QuaternionExtensions
    {
        public static bool IsValid(this Quaternion value)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsNaN(value.w))
            {
                return false;
            }
            if (float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z) || float.IsInfinity(value.w))
            {
                return false;
            }
            return true;
        }
    }
}