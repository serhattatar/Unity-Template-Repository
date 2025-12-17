using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask support

namespace Core.Pooling
{
    public static class PoolExtensions
    {
        /// <summary>
        /// Spawns a clone of the prefab from the Pool.
        /// Usage: myPrefab.Spawn(pos, rot);
        /// </summary>
        public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return PoolManager.Spawn(prefab, position, rotation, parent);
        }

        /// <summary>
        /// Returns the instance to the pool immediately.
        /// Usage: myInstance.ReturnToPool();
        /// </summary>
        public static void ReturnToPool(this GameObject instance)
        {
            if (instance.TryGetComponent(out PoolObject poolObj))
            {
                poolObj.ReturnToPool();
            }
            else
            {
                Object.Destroy(instance);
            }
        }

        /// <summary>
        /// Returns the instance to the pool after a delay (Async).
        /// Usage: myInstance.ReturnToPool(2.5f);
        /// </summary>
        public static void ReturnToPool(this GameObject instance, float delay)
        {
            PoolManager.ReturnWithDelay(instance, delay);
        }
    }
}