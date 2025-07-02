using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 오브젝트 풀링을 관리하는 매니저
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance {  get; private set; }

    // 풀에 넣을 오브젝트 정보
    [System.Serializable]
    public class Pool
    {
        public string key; 
        public GameObject prefab;
        public int initialSize = 10;
    }

    // 풀 목록
    public List<Pool> pools;

    // 풀 딕셔너리
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //모든 풀에 대해 큐를 만들고 오브젝트 해당 숫자만큼 생성
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

    // 오브젝트를 풀에서 꺼냄
    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(key))
        {
            Debug.LogWarning($"풀 키 '{key}'가 존재하지 않습니다.");
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

    
    // 오브젝트 풀에 반환
    public void Return(string key, GameObject obj)
    {
        obj.SetActive(false);
        poolDict[key].Enqueue(obj);
    }
}
