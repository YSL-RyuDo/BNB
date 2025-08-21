using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    [SerializeField] private RoomSender roomSender;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private GameObject[] roomPlayerInfos;
    [SerializeField] private Button[] characterChooseButton;
    [SerializeField] private Sprite[] characterSprites;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button teamChangeButton;

    private bool isCoopMode = false;
    private readonly Dictionary<string, string> userTeamMap = new();
    [SerializeField] private Color redTeamColor = Color.red;
    [SerializeField] private Color blueTeamColor = Color.blue;
    [SerializeField] private Color soloColor = Color.black;
    private int myPlayerIndex = -1;

    private void Start()
    {
        var client = NetworkConnector.Instance;

        roomNameText.text = client.CurrentRoomName;
        myPlayerIndex = client.CurrentUserList.FindIndex(n => n.Trim() == client.UserNickname?.Trim());

        ParseEnterRoomPacket(client.PendingRoomEnterMessage);


        for (int i = 0; i < characterChooseButton.Length; i++)
        {
            int idx = i;
            characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }

        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnClickExitRoom);

        teamChangeButton.onClick.AddListener(OnClickTeamChange);

        UpdatePlayerInfoUI(client.CurrentUserList);
        roomSender.SendGetCharacterInfo(client.UserNickname?.Trim());

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        UpdateTeamChangeButtonInteractable();
    }

    private void ParseEnterRoomPacket(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        var p = message.Split('|');
        if (p.Length < 4 || p[0] != "ENTER_ROOM_SUCCESS") return;

        string userListStr = p[3];                          // nick:idx(:team),...
        isCoopMode = p.Length >= 6 && p[5].Trim() == "1";   // coop 1/0

        userTeamMap.Clear();
        foreach (var token in userListStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var up = token.Split(':'); // nick:idx(:team)
            string nickname = up[0].Trim();
            string team = (up.Length >= 3) ? up[2].Trim() : "None";
            userTeamMap[nickname] = team;
        }

        NetworkConnector.Instance.IsCoopMode = isCoopMode;
        NetworkConnector.Instance.UserTeams.Clear();
        foreach (var kv in userTeamMap)
            NetworkConnector.Instance.UserTeams[kv.Key] = kv.Value;
    }

    public void HandleUserJoined(string message)
    {
        // 예: "REFRESH_ROOM_SUCCESS|방이름|맵이름|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS 파싱 실패: " + message);
            return;
        }

        string roomName = parts[1];
        string mapName = parts[2];

        // 빈 항목 제거 (끝 콤마/공백 방지)
        string[] userTokens = parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries);

        var nicknames = new List<string>();
        userTeamMap.Clear();
        bool anyTeamField = false;

        foreach (string raw in userTokens)
        {
            string token = raw.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            // nick:idx(:team[:...])
            var up = token.Split(':');
            if (up.Length < 2) continue;

            string nickname = up[0].Trim();
            if (string.IsNullOrEmpty(nickname)) continue;

            int charIndex = 0;
            if (up.Length >= 2) int.TryParse(up[1], out charIndex);
            NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);

            string team = (up.Length >= 3 && !string.IsNullOrWhiteSpace(up[2])) ? up[2].Trim() : "None";
            userTeamMap[nickname] = team;
            if (up.Length >= 3) anyTeamField = true;

            nicknames.Add(nickname);
        }

        if (nicknames.Count == 0)
        {
            Debug.LogWarning("[RoomUI] 빈 유저 리스트 수신 → UI 보존. 원문: " + message);
            return;
        }

        if (anyTeamField) isCoopMode = true;

        NetworkConnector.Instance.IsCoopMode = isCoopMode;
        NetworkConnector.Instance.UserTeams.Clear();
        foreach (var kv in userTeamMap)
            NetworkConnector.Instance.UserTeams[kv.Key] = kv.Value;

        // 상태 저장 및 UI 갱신 (여기서만 반영)
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = (nicknames.Count > 0) ? nicknames[0] : null;

        RefreshRoomUI(nicknames, roomName);

        // 협동전이면 계속 누를 수 있게
        UpdateTeamChangeButtonInteractable();
    }

    public void RefreshRoomUI(List<string> updatedUserList, string updatedRoomName = null)
    {
        Debug.Log($"RefreshRoomUI 호출 - updatedRoomName: {updatedRoomName}, updatedUserList.Count: {updatedUserList.Count}");

        if (!string.IsNullOrEmpty(updatedRoomName))
        {
            if (roomNameText != null)
            {
                roomNameText.text = updatedRoomName;
                Debug.Log("roomNameText.text 업데이트됨: " + updatedRoomName);
            }

            NetworkConnector.Instance.CurrentRoomName = updatedRoomName;
        }

        if (updatedUserList.Count > 0)
        {
            NetworkConnector.Instance.CurrentRoomLeader = updatedUserList[0];
            Debug.Log($"[방장 지정] 리더는 {updatedUserList[0]}");
        }
        else
        {
            NetworkConnector.Instance.CurrentRoomLeader = null;
            Debug.LogWarning("[방장 지정] 업데이트된 유저 리스트가 비어있음");
        }

        // 누락된 유저 닉네임에 해당하는 캐릭터 정보 제거
        var currentUsers = new HashSet<string>(updatedUserList);
        var keysToRemove = new List<string>();

        var characterIndexMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

        foreach (var kvp in characterIndexMap)
        {
            if (!currentUsers.Contains(kvp.Key))
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
            characterIndexMap.Remove(key);

        NetworkConnector.Instance.CurrentUserList = updatedUserList;
        UpdatePlayerInfoUI(updatedUserList);

        OnEnterRoomSuccess(NetworkConnector.Instance.CurrentRoomName, string.Join(",", updatedUserList));

        UpdateTeamChangeButtonInteractable();
    }

    public void HandleCharacterList(string message)
    {
        // 예: CHARACTER_LIST|1,0,1,1,0,0,1
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogWarning("[RoomSystem] CHARACTER_LIST 메시지 형식 오류");
            return;
        }

        string[] tokens = parts[1].Split(',');
        if (tokens.Length != characterChooseButton.Length)
        {
            Debug.LogWarning($"[RoomSystem] 캐릭터 개수 불일치: 버튼={characterChooseButton.Length}, 데이터={tokens.Length}");
            return;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            bool hasCharacter = int.TryParse(tokens[i], out int hasChar) && hasChar == 1;

            // 캐릭터 버튼 클릭 가능 여부
            characterChooseButton[i].interactable = hasCharacter;

            // 잠금 이미지 처리
            Transform lockImage = characterChooseButton[i].transform.Find("LockImage");
            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(!hasCharacter); // 보유 안 하면 잠금 이미지 켜기
            }

            // 필요하면 투명도 조정도 가능
            var buttonImage = characterChooseButton[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasCharacter ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
        }


        Debug.Log("[RoomSystem] 캐릭터 보유 여부 UI 및 잠금 반영 완료");
    }

    private void OnClickChooseCharacter(int index)
    {
        string room = NetworkConnector.Instance.CurrentRoomName;
        string nickname = NetworkConnector.Instance.UserNickname;
        roomSender.SendChooseCharacter(room, nickname, index);
    }

    private void OnClickStartGame()
    {
        roomSender.SendStartGame(NetworkConnector.Instance.CurrentRoomName);
    }

    private void OnClickExitRoom()
    {
        roomSender.SendExitRoom(NetworkConnector.Instance.CurrentRoomName, NetworkConnector.Instance.UserNickname);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public void UpdatePlayerInfoUI(List<string> userList)
    {
        Debug.Log($"UpdatePlayerInfoUI 호출 - userList.Count: {userList.Count}");

        var characterIndexMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            var nameTextTransform = roomPlayerInfos[i].transform.Find("Image/NameText");
            var characterImageTransform = roomPlayerInfos[i].transform.Find("PlayerCharacterImage");

            if (nameTextTransform == null || characterImageTransform == null)
                continue;

            var nameText = nameTextTransform.GetComponent<TextMeshProUGUI>();
            var imageComponent = characterImageTransform.GetComponent<Image>();

            if (nameText == null || imageComponent == null)
                continue;

            if (i < userList.Count)
            {
                string nickname = userList[i];
                nameText.text = nickname;

                ApplyNameColor(nameText, nickname);

                if (characterIndexMap.TryGetValue(nickname, out int characterIndex))
                {
                    Debug.Log($"[{i}] 캐릭터 인덱스: {characterIndex}");
                    if (characterIndex >= 0 && characterIndex < characterSprites.Length)
                    {
                        imageComponent.sprite = characterSprites[characterIndex];
                    }
                    else
                    {
                        Debug.LogWarning($"캐릭터 인덱스가 스프라이트 배열 범위를 벗어남: {characterIndex}");
                        imageComponent.sprite = null;
                    }
                }
                else
                {
                    Debug.LogWarning($"characterIndexMap에 {nickname} 키 없음 -> 기본값 지정");
                    characterIndex = 0;

                    // 기본값을 NetworkConnector에도 설정 (선택 사항)
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;

                    imageComponent.sprite = characterSprites[0];
                }

                Debug.Log($"roomPlayerInfos[{i}]에 유저 이름/캐릭터 설정: {nickname} / {characterIndex}");
            }
            else
            {
                nameText.text = "";
                imageComponent.sprite = null; // 슬롯 비우기
                Debug.Log($"roomPlayerInfos[{i}]에 빈 슬롯 초기화");
            }
        }
    }

    public void OnEnterRoomSuccess(string roomName, string userList)
    {
        string[] users = userList.Split(',');
        string myNick = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
        string leaderNick = NetworkConnector.Instance.CurrentRoomLeader?.Trim();

        Debug.Log($"내 닉네임: '{myNick}', 방장 닉네임: '{leaderNick}'");

        if (!string.IsNullOrEmpty(myNick) && !string.IsNullOrEmpty(leaderNick) &&
            myNick.Equals(leaderNick, System.StringComparison.OrdinalIgnoreCase))
        {
            startGameButton.gameObject.SetActive(true);
            Debug.Log("방장입니다! 시작 버튼 활성화.");
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
            Debug.Log("방장이 아닙니다. 시작 버튼 비활성화.");
        }
    }

    private void ApplyNameColor(TextMeshProUGUI nameText, string nickname)
    {
        nameText.color = soloColor;

        if (!isCoopMode)
        {
            return;
        }

        if (userTeamMap.TryGetValue(nickname, out var team))
        {
            if (string.Equals(team, "Blue", System.StringComparison.OrdinalIgnoreCase))
                nameText.color = blueTeamColor;
            else if (string.Equals(team, "Red", System.StringComparison.OrdinalIgnoreCase))
                nameText.color = redTeamColor;
        }
    }


    private void OnClickTeamChange()
    {
        if (!isCoopMode) return;

        string myNick = NetworkConnector.Instance.UserNickname?.Trim();
        if (string.IsNullOrEmpty(myNick)) return;


        roomSender.SendTeamChange(myNick);
    }

    private void UpdateTeamChangeButtonInteractable()
    {
        if (isCoopMode)
        {
            teamChangeButton.gameObject.SetActive(true);
            teamChangeButton.interactable = true;
        }
        else
        {
            teamChangeButton.gameObject.SetActive(false);
        }

    }
}
