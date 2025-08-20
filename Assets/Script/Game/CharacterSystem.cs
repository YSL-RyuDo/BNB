using System.Collections.Generic;
using UnityEngine;

public class CharacterSystem : MonoBehaviour
{
    public static CharacterSystem Instance;

    [Header("ĳ���� �����յ� (�ε����� �°� �ֱ�)")]
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
            Debug.LogWarning($"�߸��� ĳ���� �ε���: {characterIndex}");
            return;
        }

        Vector3 spawnPos = ConvertGridToWorldPosition(x, y, layer);

        if (playerCharacters.ContainsKey(playerId))
        {
            // ���� ĳ���� ��ġ ���� �� ��ǥ ��ġ�� �Բ� ����
            playerCharacters[playerId].transform.position = spawnPos;
            targetPositions[playerId] = spawnPos;
            Debug.Log($"���� ĳ���� ��ġ ����: {playerId}");
            return;
        }

        GameObject prefab = characterPrefabs[characterIndex];
        GameObject character = Instantiate(prefab, spawnPos, Quaternion.identity);
        character.name = $"Character_{playerId}";

        playerCharacters[playerId] = character;
        targetPositions[playerId] = spawnPos;

        Debug.Log($"ĳ���� ������: {playerId} ĳ���� �ε���: {characterIndex}, ��ġ({x}, {y}), ��({layer})");

        Outline outline = character.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineWidth = 5f;

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
        float posY = 0.0f; // �ʿ�� layer�� ���� ���� ����
        float posZ = y * tileSize;

        return new Vector3(posX, posY, posZ);
    }


}
