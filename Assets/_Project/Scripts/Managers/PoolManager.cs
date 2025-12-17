using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Core.Pooling;
using Cysharp.Threading.Tasks;
using Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A MonoBehaviour that acts as a Static Service for Object Pooling.
/// Hides the instance from the public API, exposing only static methods.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensure this initializes before other scripts
public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public struct PoolConfig
    {
        public GameObject Prefab;
        public int PrewarmCount;
        public int MaxSize;
    }

    [Header("Pool Configuration")]
    [SerializeField] private List<PoolConfig> _initialPools = new List<PoolConfig>();

    // --- INTERNAL STATE ---
    private static PoolManager _instance;

    private readonly Dictionary<GameObject, IObjectPool<GameObject>> _pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
    private readonly Dictionary<GameObject, Transform> _parents = new Dictionary<GameObject, Transform>();
    private Transform _rootTransform;

    private void Awake()
    {
        // Service Logic (Hidden from Public API)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _rootTransform = new GameObject("--- POOL SYSTEM ---").transform;
        DontDestroyOnLoad(_rootTransform);
        DontDestroyOnLoad(gameObject);

        if (_initialPools != null)
        {
            foreach (var config in _initialPools)
            {
                CreatePoolInternal(config.Prefab, config.PrewarmCount, config.MaxSize);
            }
        }
    }

    // --- PUBLIC STATIC API (THE SERVICE LAYER) ---

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (_instance == null)
        {
            GameLogger.Warning("[PoolManager] System missing! Instantiating directly.");
            return Instantiate(prefab, position, rotation, parent);
        }
        return _instance.SpawnInternal(prefab, position, rotation, parent);
    }

    public static void Return(GameObject obj)
    {
        if (obj.TryGetComponent(out PoolObject pObj)) pObj.ReturnToPool();
        else Destroy(obj);
    }

    public static void ReturnWithDelay(GameObject obj, float delay)
    {
        if (_instance == null) return;
        _instance.ReturnWithDelayInternal(obj, delay).Forget();
    }

    // --- INTERNAL LOGIC ---

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;

        if (!_pools.ContainsKey(prefab))
        {
            CreatePoolInternal(prefab, 5, 100);
        }

        var instance = _pools[prefab].Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        if (parent != null) instance.transform.SetParent(parent);

        return instance;
    }

    private void CreatePoolInternal(GameObject prefab, int initialSize, int maxSize)
    {
        if (prefab == null || _pools.ContainsKey(prefab)) return;

        var groupObj = new GameObject($"Pool_{prefab.name}");
        groupObj.transform.SetParent(_rootTransform);
        _parents[prefab] = groupObj.transform;

        IObjectPool<GameObject> pool = null;

        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var obj = Instantiate(prefab, groupObj.transform);

                if (!obj.TryGetComponent(out PoolObject pObj))
                {
                    pObj = obj.AddComponent<PoolObject>();
                }

                pObj.Initialize(pool); 
                return obj;
            },
            actionOnGet: (obj) =>
            {
                obj.SetActive(true);
                if (obj.TryGetComponent(out PoolObject p)) p.OnSpawn();
            },
            actionOnRelease: (obj) =>
            {
                if (obj.TryGetComponent(out PoolObject p)) p.OnDespawn();

                obj.SetActive(false);
                if (groupObj != null) obj.transform.SetParent(groupObj.transform);
            },
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize
        );

        _pools.Add(prefab, pool);

        // Prewarm Logic
        if (initialSize > 0)
        {
            var temp = new List<GameObject>();
            for (int i = 0; i < initialSize; i++) temp.Add(pool.Get());
            foreach (var obj in temp) pool.Release(obj);
        }
    }

    private async UniTaskVoid ReturnWithDelayInternal(GameObject obj, float delay)
    {
        if (obj == null) return;

        var token = obj.GetCancellationTokenOnDestroy();
        // SuppressCancellationThrow avoids exception logs if object is destroyed while waiting
        bool canceled = await UniTask.Delay((int)(delay * 1000), cancellationToken: token).SuppressCancellationThrow();

        if (!canceled && obj != null && obj.activeInHierarchy)
        {
            if (obj.TryGetComponent(out PoolObject pObj)) pObj.ReturnToPool();
            else Destroy(obj);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PoolManager))]
public class PoolManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        string helpMessage = "<b>SERVICE MODE (No Singleton):</b>\n\n" +
                             "1. <b>Spawn:</b> PoolManager.Spawn(prefab, ...)\n" +
                             "2. <b>Or use Extensions:</b> prefab.Spawn(...)\n\n" +
                             "<i>* Ensure this object exists in the first scene.</i>";

        GUIStyle style = new GUIStyle(EditorStyles.helpBox);
        style.richText = true;
        EditorGUILayout.LabelField(helpMessage, style);
        EditorGUILayout.Space(5);
        DrawDefaultInspector();
        EditorGUILayout.Space(10);

        var signatureStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        signatureStyle.fontStyle = FontStyle.Italic;
        EditorGUILayout.LabelField("System by M.S.T.", signatureStyle);
    }
}
#endif