using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    public GameObject[] map1Prefabs; // 0,1,2 ������
    public GameObject[] map2Prefabs;
    int wallHeight = 5;

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
                tilePrefabs = map2Prefabs;
                break;
            default:
                Debug.LogError("�� �� ���� �� �̸�: " + mapName);
                return;
        }

        GameObject mapParent = new GameObject($"Map_{mapName}");
        mapParent.transform.rotation = Quaternion.identity;

        string[] rows = rawData.Split(';');
        int totalRows = rows.Length;
        int layerHeight = totalRows / 2;
        int mapWidth = rows[0].Split(',').Length;

        for (int y = 0; y < totalRows; y++)
        {
            string[] cols = rows[y].Split(',');

            bool isUpperLayer = (y >= layerHeight);
            float yOffset = isUpperLayer ? 1f : 0f;
            int localZ = y % layerHeight;

            if (!isUpperLayer)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    if (int.TryParse(cols[x], out int tileIndex))
                    {
                        if (tileIndex == 5) continue;

                        Vector3 pos = new Vector3(x, yOffset, localZ);
                        Quaternion rotation = Quaternion.Euler(90, 0, 180);
                        GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, rotation);
                        tile.name = $"{tilePrefabs[tileIndex].name}_{x}_{localZ}"; // �̸� ����
                        tile.transform.SetParent(mapParent.transform);
                    }
                }
            }
            else
            {
                int startX = -1;
                int endX = mapWidth;
                int startZ = -1;
                int endZ = layerHeight;

                for (int x = startX; x <= endX; x++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        int tileIndex = -1;
                        bool isBorder = (x == startX || x == endX || z == startZ || z == endZ);

                        if (isBorder)
                        {
                            for (int height = 1; height <= wallHeight; height++)
                            {
                                Vector3 pos = new Vector3(x, height, z);
                                GameObject tile = Instantiate(tilePrefabs[5], pos, Quaternion.identity);
                                tile.name = $"{tilePrefabs[5].name}_{x}_{z}_h{height}"; // ��ǥ ����
                                tile.transform.SetParent(mapParent.transform);
                            }
                        }
                        else if (x >= 0 && x < mapWidth && z >= 0 && z < layerHeight)
                        {
                            string[] upperLayerCols = rows[layerHeight + z].Split(',');
                            if (int.TryParse(upperLayerCols[x], out int upperTileIndex))
                            {
                                tileIndex = upperTileIndex;
                                if (tileIndex == 5) continue;
                            }
                            else
                            {
                                Debug.LogWarning($"[�� �ε�] ���� ��ȯ ���� ({mapName}, ��ġ: {x},{layerHeight + z})");
                                continue;
                            }
                        }

                        if (tileIndex >= 0 && tileIndex < tilePrefabs.Length)
                        {
                            Vector3 pos = new Vector3(x, yOffset, z);
                            GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, Quaternion.identity);
                            tile.name = $"{tilePrefabs[tileIndex].name}_{x}_{z}"; // �̸� ����
                            tile.transform.SetParent(mapParent.transform);
                        }
                    }
                }
                break;
            }
        }

        Debug.Log("�� �ε� �Ϸ�: " + mapName);
    }
}
