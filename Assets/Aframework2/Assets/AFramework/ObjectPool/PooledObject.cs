using UnityEngine;
using System.Collections;

namespace AFramework
{
    public class PooledObject : MonoBehaviour
    {
        [System.NonSerialized]
        ObjectPool poolInstanceForPrefab;

        public T GetPooledInstance<T>() where T : PooledObject
        {
            if (Pool == null)
            {
                Pool = ObjectPool.GetPool(this);
            }
            return (T)Pool.GetObject();
        }

        public ObjectPool Pool { get; set; }

        public virtual void ReturnToPool()
        {
            if (Pool)
            {
                Pool.Return(this);
            }
            else {
                Destroy(gameObject);
            }
        }
    }
}