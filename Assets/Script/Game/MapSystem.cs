using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    public GameObject[] map1Prefabs; // 0,1,2 ������
    //public GameObject[] map2Prefabs;
    // ...
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
                // tilePrefabs = map2Prefabs;
                break;
            default:
                Debug.LogError("�� �� ���� �� �̸�: " + mapName);
                return;
        }

        GameObject mapParent = new GameObject($"Map_{mapName}");
        mapParent.transform.rotation = Quaternion.identity;

        string[] rows = rawData.Split(';');
        int totalRows = rows.Length;
        int layerHeight = totalRows / 2;  // 13 (�� ����)
        int mapWidth = rows[0].Split(',').Length;  // 15 (���� �ʺ�)

        for (int y = 0; y < totalRows; y++)
        {
            string[] cols = rows[y].Split(',');

            bool isUpperLayer = (y >= layerHeight);
            float yOffset = isUpperLayer ? 1f : 0f;

            int localZ = y % layerHeight;

            if (!isUpperLayer)
            {
                // 0�� (�ٴ�) - ���� 15x13 �� �״�� ����
                for (int x = 0; x < mapWidth; x++)
                {
                    if (int.TryParse(cols[x], out int tileIndex))
                    {
                        if (tileIndex == 5) continue; // Ư�� Ÿ�� ����

                        Vector3 pos = new Vector3(x, yOffset, localZ);
                        Quaternion rotation = Quaternion.Euler(90, 0, 180); // X������ 90�� ȸ��
                        GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, rotation);
                        tile.transform.SetParent(mapParent.transform);
                    }
                    else
                    {
                        Debug.LogWarning($"[�� �ε�] ���� ��ȯ ���� ({mapName}, ��ġ: {x},{y})");
                    }
                }
            }
            else
            {
                // 1�� (����) - �¿�, ���� �׵θ� ���� 17x15 ���� ����

                int startX = -1;
                int endX = mapWidth; // 15

                int startZ = -1;
                int endZ = layerHeight; // 13

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
                                tile.transform.SetParent(mapParent.transform);
                            }
                        }
                        else
                        {
                            // ���� Ÿ���� ���� �����ͷ� ä�� (1�� �����ʹ� rows[layerHeight + z])
                            // z�� -1~13 �����̹Ƿ� ���δ� 0~12 ������ ���� �����ͷ� ����
                            if (z >= 0 && z < layerHeight && x >= 0 && x < mapWidth)
                            {
                                string[] upperLayerCols = rows[layerHeight + z].Split(',');
                                if (int.TryParse(upperLayerCols[x], out int originalTileIndex))
                                {
                                    tileIndex = originalTileIndex;
                                    if (tileIndex == 5) continue; // ���� Ÿ��
                                }
                                else
                                {
                                    Debug.LogWarning($"[�� �ε�] ���� ��ȯ ���� ({mapName}, ��ġ: {x},{layerHeight + z})");
                                    continue;
                                }
                            }
                            else
                            {
                                // ��� �� ���� �ƴѵ� ������ ����, ����
                                continue;
                            }
                        }

                        if (tileIndex >= 0 && tileIndex < tilePrefabs.Length)
                        {
                            Vector3 pos = new Vector3(x, yOffset, z);
                            GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, Quaternion.identity);
                            tile.transform.SetParent(mapParent.transform);
                        }
                    }
                }
                // �� �྿ ó���ϴ� ��� x,z ���� for������ ��ü ���� Ŀ��
                break; // ������ �ѹ��� ó���ϸ� ��
            }
        }

        Debug.Log("�� �ε� �Ϸ�: " + mapName);
    }

}
