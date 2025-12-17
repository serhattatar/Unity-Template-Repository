using UnityEngine;
using UnityEngine.Pool;

namespace Core.Pooling
{
    [DisallowMultipleComponent]
    public class PoolObject : MonoBehaviour
    {
        private IObjectPool<GameObject> _pool;

        public void Initialize(IObjectPool<GameObject> pool)
        {
            _pool = pool;
        }

        public virtual void OnSpawn()
        {
            
        }

        public virtual void OnDespawn()
        {
           
        }

        public void ReturnToPool()
        {
            if (_pool != null && gameObject.activeInHierarchy)
            {
                OnDespawn(); 
                _pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}