using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AFramework
{
    public class ObjectPool : MonoBehaviour
    {
        static Dictionary<PooledObject, ObjectPool> sPoolDict = new Dictionary<PooledObject, ObjectPool>();

        public PooledObject prefab;

        Stack<PooledObject> objCached = new Stack<PooledObject>();
        List<PooledObject> objUsed = new List<PooledObject>();
        int cacheNum = 0;

        void OnDestroy()
        {
            if (prefab != null && sPoolDict.ContainsKey(prefab))
            {
                sPoolDict.Remove(prefab);
            }

            var num = objCached.Count;
            while (num > 0)
            {
                Object.Destroy(objCached.Pop().gameObject);
                --num;
            }
        }

        public void Cache(int num)
        {
            if (!sPoolDict.ContainsKey(prefab)) sPoolDict[prefab] = this;
            while (cacheNum < num)
            {
                PooledObject obj = Instantiate<PooledObject>(prefab, this.transform);
                obj.gameObject.SetActive(false);
                obj.Pool = this;
                objCached.Push(obj);
                ++cacheNum;
            }
        }

        public PooledObject GetObject()
        {
            PooledObject obj;
            if (objCached.Count > 0)
            {
                obj = objCached.Pop();
                obj.gameObject.SetActive(true);
            }
            else
            {
                obj = Instantiate<PooledObject>(prefab, this.transform);
                obj.Pool = this;
                ++cacheNum;
            }
            objUsed.Add(obj);
            return obj;
        }

        public bool IsPoolUsed()
        {
            return cacheNum != objCached.Count;
        }

        public void Return(PooledObject obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(this.transform);
            if (!objCached.Contains(obj)) objCached.Push(obj);
            objUsed.Remove(obj);
        }

        public void ReturnAll()
        {
            while (objUsed.Count > 0)
            {
                var obj = objUsed[0];
                obj.ReturnToPool();
            }
        }

        public static ObjectPool GetPool(PooledObject prefab)
        {
            return Create(prefab, 1);
        }

        public static ObjectPool Create(PooledObject prefab, int cacheNum, Transform parent = null)
        {
            if (!sPoolDict.ContainsKey(prefab))
            {
                GameObject obj = new GameObject(prefab.name + " Pool");
                ObjectPool pool = null;
                if (parent == null) DontDestroyOnLoad(obj);
                else
                {
                    obj.transform.SetParent(parent);
                    pool = parent.GetComponent<ObjectPool>();
                }
                if (pool == null) pool = obj.AddComponent<ObjectPool>();
                pool.prefab = prefab;
                sPoolDict[prefab] = pool;
                prefab.Pool = pool;
            }

            var poolObj = sPoolDict[prefab];
            poolObj.Cache(cacheNum);
            return poolObj;
        }

        public static void Destroy(ObjectPool pool)
        {
            Object.Destroy(pool.gameObject);
        }
    }
}

