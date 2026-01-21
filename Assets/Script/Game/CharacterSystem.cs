using System.Collections.Generic;
using UnityEngine;

public class CharacterSystem : MonoBehaviour
{
    public static CharacterSystem Instance;

    [Header("캐릭터 프리팹들 (인덱스에 맞게 넣기)")]
    public GameObject[] characterPrefabs;

    private Dictionary<string, GameObject> playerCharacters = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();

    public Sprite[] characterAliveImage;
    public Sprite[] characterDeathImage;
    public Sprite[] characterWinImage;
    private void Awake()
    {
        Instance = this;
    }

    public void SpawnCharacterAt(string playerId, int characterIndex, int x, int y, int layer)
    {
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
        {
            Debug.LogWarning($"잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }

        Vector3 spawnPos = ConvertGridToWorldPosition(x, y, layer);

        if (playerCharacters.ContainsKey(playerId))
        {
            // 기존 캐릭터 위치 갱신 및 목표 위치도 함께 변경
            playerCharacters[playerId].transform.position = spawnPos;
            targetPositions[playerId] = spawnPos;
            Debug.Log($"기존 캐릭터 위치 갱신: {playerId}");
            return;
        }

        GameObject prefab = characterPrefabs[characterIndex];
        GameObject character = Instantiate(prefab, spawnPos, Quaternion.identity);
        character.name = $"Character_{playerId}";

        playerCharacters[playerId] = character;
        targetPositions[playerId] = spawnPos;

        Debug.Log($"캐릭터 생성됨: {playerId} 캐릭터 인덱스: {characterIndex}, 위치({x}, {y}), 층({layer})");
        var renderer = character.GetComponentInChildren<SkinnedMeshRenderer>();

        var outline = renderer.gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        float width = 5f * (Screen.height / 1080f);
        width = Mathf.Clamp(width, 2f, 10f);  
        outline.OutlineWidth = width;

        var nc = NetworkConnector.Instance;
        string myNick = nc.UserNickname;

        if (playerId == myNick)
        {
            outline.OutlineColor = Color.blue;
        }
        else
        {
            if (nc.IsCoopMode)
            {
                string myTeam = nc.UserTeams.TryGetValue(myNick, out var mt) ? mt : "None";
                string playerTeam = nc.UserTeams.TryGetValue(playerId, out var pt) ? pt : "None";

                if (!string.IsNullOrEmpty(myTeam) && myTeam != "None" && myTeam == playerTeam)
                    outline.OutlineColor = Color.green;
                else
                    outline.OutlineColor = Color.red;
            }
            else
            {
                outline.OutlineColor = Color.red;
            }
        }

    }

    private Vector3 ConvertGridToWorldPosition(int x, int y, int layer)
    {
        float tileSize = 1.0f;
        float posX = x * tileSize;
        float posY = 0.0f; // 필요시 layer에 따라 조절 가능
        float posZ = y * tileSize;

        return new Vector3(posX, posY, posZ);
    }


}
