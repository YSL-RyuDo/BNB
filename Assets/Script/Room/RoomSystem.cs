using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class RoomSystem : MonoBehaviour
{
    public static RoomSystem Instance;


    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButton;
    public Sprite[] characterSprites;
    private int myPlayerIndex = -1;

    public Button startGameButton;
    public Button exitButton;
    //public Dictionary<string, int> characterIndexMap = new Dictionary<string, int>();
    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        var client = NetworkConnector.Instance;

        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        // 더 이상 characterIndexMap 사용 안 하므로 삭제

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        string myNick = client.UserNickname?.Trim();

        // 내 플레이어 인덱스 찾기
        myPlayerIndex = client.CurrentUserList.FindIndex(nick => nick.Trim() == myNick);

        // 캐릭터 버튼 이벤트 연결
        for (int i = 0; i < characterChooseButton.Length; i++)
        {
            int idx = i; // 캡처 방지용 복사
            characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }

        // UI 업데이트 - 캐릭터 인덱스 맵은 NetworkConnector에서 직접 가져옴
        UpdatePlayerInfoUI(client.CurrentUserList);

        // 서버에 캐릭터 정보 요청 보내기
        string getCharacterMsg = $"GET_CHARACTER|{myNick}\n";
        byte[] getCharacterBytes = Encoding.UTF8.GetBytes(getCharacterMsg);

        var stream = client.Stream;
        if (stream != null && stream.CanWrite)
        {
            await stream.WriteAsync(getCharacterBytes, 0, getCharacterBytes.Length);
        }
    }


    public void HandleUserJoined(string message)
    {
        // 예: "REFRESH_ROOM_SUCCESS|방이름|맵이름|userA:1,userB:2,userC:0"
        string[] parts = message.Split('|');
        if (parts.Length < 4)
        {
            Debug.LogWarning("REFRESH_ROOM_SUCCESS 파싱 실패");
            return;
        }

        string roomName = parts[1];
        string mapName = parts[2];
        string[] userTokens = parts[3].Split(',');

        List<string> nicknames = new List<string>();

        // NetworkConnector.Instance.CurrentUserCharacterIndices.Clear();

        foreach (string token in userTokens)
        {
            if (token.Contains(":"))
            {
                var pair = token.Split(':');
                string nickname = pair[0].Trim();
                int charIndex = 0;
                if (!int.TryParse(pair[1], out charIndex))
                {
                    charIndex = 0; // 기본값
                }
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, charIndex);
                nicknames.Add(nickname);
            }
            else
            {
                string nickname = token.Trim();
                NetworkConnector.Instance.SetOrUpdateUserCharacter(nickname, 0);
                nicknames.Add(nickname);
            }
        }

        // 저장
        NetworkConnector.Instance.CurrentRoomName = roomName;
        NetworkConnector.Instance.SelectedMap = mapName;
        NetworkConnector.Instance.CurrentUserList = nicknames;
        NetworkConnector.Instance.CurrentRoomLeader = nicknames.Count > 0 ? nicknames[0] : null;

        RefreshRoomUI(nicknames, roomName);
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
        // 따로 복사할 필요 없이 CurrentUserCharacterIndices가 이미 최신 상태라고 가정
        // 만약 다른 변수에 데이터가 있다면 그걸 반영하는 코드를 여기에 추가

        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string msg = $"START_GAME|{roomName}\n";

        var stream = NetworkConnector.Instance.Stream;
        byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

        if (stream != null && stream.CanWrite)
        {
            await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
        }
        else
        {
            Debug.LogWarning("스트림이 없거나 쓰기 불가능 상태입니다.");
        }
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
        NetworkConnector.Instance.SetOrUpdateUserCharacter(userNickname, characterIndex);

        // UI 즉시 반영
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }

}
