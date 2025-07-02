using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ������Ʈ Ǯ���� �����ϴ� �Ŵ���
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance {  get; private set; }

    // Ǯ�� ���� ������Ʈ ����
    [System.Serializable]
    public class Pool
    {
        public string key; 
        public GameObject prefab;
        public int initialSize = 10;
    }

    // Ǯ ���
    public List<Pool> pools;

    // Ǯ ��ųʸ�
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //��� Ǯ�� ���� ť�� ����� ������Ʈ �ش� ���ڸ�ŭ ����
        foreach (var pool in pools)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }
            poolDict.Add(pool.key, objectQueue);
        }
    }

    // ������Ʈ�� Ǯ���� ����
    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(key))
        {
            Debug.LogWarning($"Ǯ Ű '{key}'�� �������� �ʽ��ϴ�.");
            return null;
        }

        if (poolDict[key].Count == 0)
        {
            GameObject prefab = pools.Find(p => p.key == key).prefab;
            GameObject newObj = Instantiate(prefab, position, rotation, transform);
            newObj.SetActive(true);
            return newObj;
        }

        GameObject obj = poolDict[key].Dequeue();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    
    // ������Ʈ Ǯ�� ��ȯ
    public void Return(string key, GameObject obj)
    {
        obj.SetActive(false);
        poolDict[key].Enqueue(obj);
    }
}
