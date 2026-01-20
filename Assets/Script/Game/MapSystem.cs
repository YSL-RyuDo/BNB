using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    private Dictionary<string, GameObject> blocksDict = new Dictionary<string, GameObject>(); // 블록 관리

    public GameObject[] map1Prefabs; // 0,1,2 프리팹
    public GameObject[] map2Prefabs;

    [SerializeField]
    private GameObject[] backGroundPlane;

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
                backGroundPlane[0].SetActive(true);
                break;
            case "Map2":
                tilePrefabs = map2Prefabs;
                backGroundPlane[1].SetActive(true);
                break;
            default:
                Debug.LogError("알 수 없는 맵 이름: " + mapName);
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
                        tile.name = $"{tilePrefabs[tileIndex].name}_{x}_{localZ}"; // 이름 변경
                        tile.transform.SetParent(mapParent.transform);

                        // 블록 등록
                        blocksDict[tile.name] = tile;
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
                                GameObject tile = Instantiate(tilePrefabs[5], pos, Quaternion.Euler(0, 180, 0));
                                tile.name = $"{tilePrefabs[5].name}_{x}_{z}_h{height}"; // 좌표 포함
                                tile.transform.SetParent(mapParent.transform);

                                // 블록 등록
                                blocksDict[tile.name] = tile;
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
                                Debug.LogWarning($"[맵 로딩] 숫자 변환 실패 ({mapName}, 위치: {x},{layerHeight + z})");
                                continue;
                            }
                        }

                        if (tileIndex >= 0 && tileIndex < tilePrefabs.Length)
                        {
                            Vector3 pos = new Vector3(x, yOffset, z);
                            GameObject tile = Instantiate(tilePrefabs[tileIndex], pos, Quaternion.Euler(0, 180, 0));
                            tile.name = $"{tilePrefabs[tileIndex].name}_{x}_{z}"; // 이름 변경
                            tile.transform.SetParent(mapParent.transform);

                            // 블록 등록
                            blocksDict[tile.name] = tile;
                        }
                    }
                }
                break;
            }
        }

        Debug.Log("맵 로딩 완료: " + mapName);
    }

    // 메시지 처리에서 바로 호출
    public void HandleDestroyBlockMessage(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 2) return;

        string[] blockNames = parts[1].Split(',');

        // 메인 스레드에서 실행
        StartCoroutine(DestroyBlocksCoroutine(blockNames));
    }

    private IEnumerator DestroyBlocksCoroutine(string[] blockNames)
    {
        foreach (var rawName in blockNames)
        {
            string name = rawName.Trim();
            if (blocksDict.TryGetValue(name, out GameObject block) && block != null)
            {
                block.SetActive(false);
                Destroy(block);
                blocksDict.Remove(name);
                Debug.Log($"[Map] 블록 파괴: {name}");
            }
            else
            {
                Debug.LogWarning($"[Map] 블록을 찾을 수 없음: {name}");
            }
        }
        yield return null;
    }
}
