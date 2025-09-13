using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class VFXPool : MonoBehaviour
{
    [Header("VFX Parent Group")]
    [SerializeField] private Transform vfxGroupParent;

    public static VFXPool Instance { get; private set; }

    [System.Serializable]
    public class VFXPrefabData
    {
        public string key;                 // 用於辨識的鍵，例如 "Cake", "Beer", "Attack", "Hurt"
        public GameObject prefab;          // 該效果的預置物
        public int defaultCapacity = 10;   // 物件池預設容量
        public int maxSize = 50;           // 物件池最大容量
    }

    [Header("VFX Prefabs")]
    [SerializeField] private List<VFXPrefabData> vfxPrefabs;

    private Dictionary<string, ObjectPool<GameObject>> pools =
        new Dictionary<string, ObjectPool<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 建立所有 VFX 的物件池
        foreach (var data in vfxPrefabs)
        {
            if (data.prefab == null || string.IsNullOrEmpty(data.key))
                continue;

            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(data.prefab, vfxGroupParent);
                    obj.SetActive(false);
                    return obj;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: data.defaultCapacity,
                maxSize: data.maxSize
            );

            pools[data.key] = pool;
        }
    }

    /// <summary>
    /// 取得 VFX（可選擇自動釋放）autoReleaseTime = -1 不限時間
    /// </summary>
    public GameObject SpawnVFX(string key, Vector3 position, Quaternion rotation, float autoReleaseTime = -1f)
    {
        if (key == null || string.IsNullOrEmpty(key)) return null;
        
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"VFXPool: 找不到 Key '{key}' 對應的物件池。");
            return null;
        }

        var obj = pools[key].Get();
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        if (autoReleaseTime > 0)
            StartCoroutine(AutoReleaseCoroutine(key, obj, autoReleaseTime));

        return obj;
    }

    /// <summary>
    /// 歸還 VFX
    /// </summary>
    private void ReleaseVFX(string key, GameObject obj)
    {
        if (pools.ContainsKey(key))
            pools[key].Release(obj);
        else
            Destroy(obj);
    }

    private System.Collections.IEnumerator AutoReleaseCoroutine(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseVFX(key, obj);
    }
}
