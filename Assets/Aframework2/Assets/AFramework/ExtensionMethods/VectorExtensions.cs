using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class VectorExtensions
    {
        #region Vecter2 Extentions

        public static float GetAngleInRadian(this Vector2 v1, Vector2 v2)
        {
            return Mathf.Atan2(v2.y - v1.y, v2.x - v1.x);
        }

        public static float GetAngleInDegree(this Vector2 v1, Vector2 v2)
        {
            return Mathf.Atan2(v2.y - v1.y, v2.x - v1.x) * Mathf.Rad2Deg;
        }

        public static Vector2 Rotate(this Vector2 v, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.back) * v;
        }

        public static float GetAngleInDegree(this Vector2 v1)
        {
            return Mathf.Atan2(v1.y, v1.x) * Mathf.Rad2Deg;
        }

        #endregion

        #region Vecter3 Extentions
        // axisDirection - unit vector in direction of an axis (eg, defines a line that passes through zero)
        // point - the point to find nearest on line for
        public static Vector3 NearestPointOnAxis(this Vector3 axisDirection, Vector3 point, bool isNormalized = false)
        {
            if (!isNormalized) axisDirection.Normalize();
            var d = Vector3.Dot(point, axisDirection);
            return axisDirection * d;
        }

        // lineDirection - unit vector in direction of line
        // pointOnLine - a point on the line (allowing us to define an actual line in space)
        // point - the point to find nearest on line for
        public static Vector3 NearestPointOnLine(
            this Vector3 lineDirection, Vector3 point, Vector3 pointOnLine, bool isNormalized = false)
        {
            if (!isNormalized) lineDirection.Normalize();
            var d = Vector3.Dot(point - pointOnLine, lineDirection);
            return pointOnLine + (lineDirection * d);
        }

        public static bool IsValid(this Vector3 value)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z))
            {
                return false;
            }

            if (float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z))
            {
                return false;
            }

            return true;
        }

        public static float GetAngleInRadian(this Vector3 v1, Vector3 v2)
        {
            return Mathf.Atan2(v2.y - v1.y, v2.x - v1.x);
        }

        public static float GetAngleInDegree(this Vector3 v1, Vector3 v2)
        {
            return Mathf.Atan2(v2.y - v1.y, v2.x - v1.x) * Mathf.Rad2Deg;
        }

        public static float GetAngleInDegree(this Vector3 v1)
        {
            return Mathf.Atan2(v1.y, v1.x) * Mathf.Rad2Deg;
        }


        public static Vector3 Rotate(this Vector3 v, float angle)
        {
            v = Quaternion.AngleAxis(angle, Vector3.back) * v;
            return v;
        }

        public static Vector2 XZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 YZ(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }
        #endregion
    }
}