using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class LayerMaskExtensions
    {
        public static int GetRaycastValue(this LayerMask mask)
        {
            return 1 << mask;
        }
    }
}