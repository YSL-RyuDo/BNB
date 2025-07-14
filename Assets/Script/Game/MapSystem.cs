using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    public GameObject[] map1Prefabs; // 0,1,2 ������
    //public GameObject[] map2Prefabs;
    // ...

    private void Awake()
    {
        Instance = this;
    }

    public void LoadMap(string mapName, string rawData)
    {
        GameObject[] tilePrefabs = null;

        switch (mapName)
        {
            case "Map1":
                tilePrefabs = map1Prefabs;
                break;
            case "Map2":
                //tilePrefabs = map2Prefabs;
                break;
            default:
                Debug.LogError("�� �� ���� �� �̸�: " + mapName);
                return;
        }

        GameObject mapParent = new GameObject($"Map_{mapName}");
        mapParent.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        string[] rows = rawData.Split(';');

        for (int y = 0; y < rows.Length; y++)
        {
            string[] cols = rows[y].Split(',');

            for (int x = 0; x < cols.Length; x++)
            {
                if (int.TryParse(cols[x], out int tileIndex) && tileIndex >= 0 && tileIndex < tilePrefabs.Length)
                {
                    Vector3 position = new Vector3(x, 0, -y); // Y��� Z�� ��� (������ ����)
                    GameObject tile = Instantiate(tilePrefabs[tileIndex], position, Quaternion.identity);
                    tile.transform.SetParent(mapParent.transform); // �θ� ����
                }
            }
        }

        Debug.Log("�� �ε� �Ϸ�: " + mapName);
    }
}
