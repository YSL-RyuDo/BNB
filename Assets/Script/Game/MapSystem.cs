using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    public GameObject[] map1Prefabs; // 0,1,2 프리팹
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
                Debug.LogError("알 수 없는 맵 이름: " + mapName);
                return;
        }

        GameObject mapParent = new GameObject($"Map_{mapName}");
        mapParent.transform.rotation = Quaternion.identity;

        string[] rows = rawData.Split(';');
        int totalRows = rows.Length;
        int layerHeight = totalRows / 2;  // 13 (층 높이)
        int mapWidth = rows[0].Split(',').Length;  // 15 (가로 너비)

        for (int y = 0; y < totalRows; y++)
        {
            string[] cols = rows[y].Split(',');

            bool isUpperLayer = (y >= layerHeight);
            float yOffset = isUpperLayer ? 1f : 0f;

            int localZ = y % layerHeight;

            if (!isUpperLayer)
            {
                // 0층 (바닥) - 기존 15x13 맵 그대로 생성
                for (int x = 0; x < mapWidth; x++)
                {
                    if (int.TryParse(cols[x], out int tileIndex))
                    {
                        if (tileIndex == 5) continue; // 특정 타일 무시

                        Vector3 pos = new Vector3(x, yOffset, localZ);
                        Quaternion rotation = Quaternion.Euler(90, 0, 180); // X축으로 90도 회전
                        GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, rotation);
                        tile.transform.SetParent(mapParent.transform);
                    }
                    else
                    {
                        Debug.LogWarning($"[맵 로딩] 숫자 변환 실패 ({mapName}, 위치: {x},{y})");
                    }
                }
            }
            else
            {
                // 1층 (상층) - 좌우, 상하 테두리 포함 17x15 범위 생성

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
                            // 내부 타일은 원래 데이터로 채움 (1층 데이터는 rows[layerHeight + z])
                            // z는 -1~13 범위이므로 내부는 0~12 범위만 원래 데이터로 읽음
                            if (z >= 0 && z < layerHeight && x >= 0 && x < mapWidth)
                            {
                                string[] upperLayerCols = rows[layerHeight + z].Split(',');
                                if (int.TryParse(upperLayerCols[x], out int originalTileIndex))
                                {
                                    tileIndex = originalTileIndex;
                                    if (tileIndex == 5) continue; // 무시 타일
                                }
                                else
                                {
                                    Debug.LogWarning($"[맵 로딩] 숫자 변환 실패 ({mapName}, 위치: {x},{layerHeight + z})");
                                    continue;
                                }
                            }
                            else
                            {
                                // 경계 밖 내부 아닌데 데이터 없음, 무시
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
                // 한 행씩 처리하는 대신 x,z 이중 for문으로 전체 영역 커버
                break; // 상층은 한번만 처리하면 됨
            }
        }

        Debug.Log("맵 로딩 완료: " + mapName);
    }

}
