using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class RoomSystem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButton;
    public Sprite[] characterSprites;
    private int myPlayerIndex = -1;

    public Button startGameButton;
    public Button exitButton;
    public Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();

    private void Start()
    {
        var client = NetworkConnector.Instance;
        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        characterIndexMap = new Dictionary<string, int>(client.CurrentUserCharacterIndices);

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        foreach (var kvp in client.CurrentUserCharacterIndices)
        {
            characterIndexMap[kvp.Key] = kvp.Value;
        }

        string myNick = NetworkConnector.Instance.UserNickname; //PlayerPrefs.GetString("nickname")?.Trim();
        for (int i = 0; i < client.CurrentUserList.Count; i++)
        {
            if (client.CurrentUserList[i].Trim() == myNick)
            {
                myPlayerIndex = i;
                break;
            }
        }

        // 캐릭터 버튼 연결
        for (int i = 0; i < characterChooseButton.Length; i++)
        {
            int idx = i; // 캡처 방지용 복사
            characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }
        UpdatePlayerInfoUI(client.CurrentUserList);
    }

    public void HandleUserJoined(string message)
    {
        // 예: "REFRESH_ROOM_SUCCESS|방이름|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 3)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS 파싱 실패");
            return;
        }

        string roomName = parts[1];
        string[] userTokens = parts[2].Split(',');

        List<string> nicknames = new List<string>();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                string[] pair = token.Split(':');
                string nickname = pair[0].Trim();
                if (!string.IsNullOrEmpty(nickname))
                {
                    nicknames.Add(nickname);

                    if (int.TryParse(pair[1], out int charIndex))
                    {
                        characterIndexMap[nickname] = charIndex;
                        NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = charIndex;
                    }
                    else
                    {
                        characterIndexMap[nickname] = 0;
                        NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                        Debug.LogWarning($"캐릭터 인덱스 파싱 실패: {token}");
                    }
                }
            }
            else
            {
                string nickname = token.Trim();
                if (!string.IsNullOrEmpty(nickname))
                {
                    nicknames.Add(nickname);
                    characterIndexMap[nickname] = 0;
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nickname] = 0;
                }
            }
        }

        // 저장
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.CurrentUserList = nicknames;

        RefreshRoomUI(nicknames, roomName);
    }


    public void UpdatePlayerInfoUI(List<string> userList)
    {
        Debug.Log($"UpdatePlayerInfoUI 호출 - userList.Count: {userList.Count}");
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
                    characterIndexMap[nickname] = 0;
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

    // 서버에서 유저 리스트 갱신 메시지를 받을 때 호출할 메서드
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

        // 누락된 유저 닉네임에 해당하는 캐릭터 정보 제거
        var currentUsers = new HashSet<string>(updatedUserList);
        var keysToRemove = new List<string>();

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
    }

    private async void OnClickChooseCharacter(int characterIndex)
    {
        if (myPlayerIndex < 0 || myPlayerIndex >= roomPlayerInfos.Length)
        {
            Debug.LogWarning("내 슬롯 인덱스가 올바르지 않습니다.");
            return;
        }

        string nickname = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"CHOOSE_CHARACTER|{roomName}|{nickname}|{characterIndex}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

        Debug.Log($"캐릭터 선택 전송: {msg.Trim()}");
    }

    void OnEnterRoomSuccess(string roomName, string userList)
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

    public async void OnClickStartGame()
    {
        NetworkConnector.Instance.CurrentUserCharacterIndices.Clear();
        foreach (var kvp in characterIndexMap)
        {
            NetworkConnector.Instance.CurrentUserCharacterIndices[kvp.Key] = kvp.Value;
        }

        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"START_GAME|{roomName}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
    }
    public async void OnExitRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string myNickname = NetworkConnector.Instance.UserNickname;
        string msg = $"EXIT_ROOM|{roomName}|{myNickname}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }


    public void UpdateCharacterChoice(string userNickname, int characterIndex)
    {
        characterIndexMap[userNickname] = characterIndex;

        // 바로 UI 반영도 가능하지만, 전체 UI 재갱신 시 자동 반영되도록 해도 됨
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }
}
