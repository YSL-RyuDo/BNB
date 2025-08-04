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

    //[SerializeField] private Button[] characterChooseButton;
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Transform characterButtonParent;
    private List<Button> characterButtons = new();

    [SerializeField] private Sprite[] characterSprites;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitButton;

    private int myPlayerIndex = -1;

    private void Start()
    {
        var client = NetworkConnector.Instance;

        roomNameText.text = client.CurrentRoomName;
        myPlayerIndex = client.CurrentUserList.FindIndex(n => n.Trim() == client.UserNickname?.Trim());

        //for (int i = 0; i < characterChooseButton.Length; i++)
        //{
        //    int idx = i;
        //    characterChooseButton[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        //}

        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnClickExitRoom);

        UpdatePlayerInfoUI(client.CurrentUserList);
        roomSender.SendGetCharacterInfo(client.UserNickname?.Trim());

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
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

    public void GenerateCharacterButtons(int count)
    {
        foreach (Transform child in characterButtonParent)
            Destroy(child.gameObject);

        characterButtons.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject buttonGO = Instantiate(characterButtonPrefab, characterButtonParent);
            Button btn = buttonGO.GetComponent<Button>();
            int index = i;

            btn.onClick.AddListener(() => OnClickChooseCharacter(index));
            characterButtons.Add(btn);

            Image characterImage = btn.GetComponent<Image>();
            if (characterImage != null && i < characterSprites.Length)
            {
                characterImage.sprite = characterSprites[i];
                Debug.Log($"[캐릭터 버튼 생성] index: {i}, sprite: {characterSprites[i].name}");
            }
            else
            {
                Debug.LogWarning($"[캐릭터 버튼 생성] index {i}에 대한 sprite 없음");
            }
        }

        Debug.Log($"{count}개의 캐릭터 버튼 생성 완료 (스프라이트 포함)");
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

        int count = tokens.Length;

        GenerateCharacterButtons(count);

        for (int i = 0; i < count; i++)
        {
            bool hasCharacter = int.TryParse(tokens[i], out int hasChar) && hasChar == 1;

            Button btn = characterButtons[i];
            btn.interactable = hasCharacter;

            Transform lockImage = btn.transform.Find("LockImage");
            if (lockImage != null)
                lockImage.gameObject.SetActive(!hasCharacter);

            var buttonImage = btn.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = hasCharacter ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }


        Debug.Log("[RoomSystem] 캐릭터 보유 여부 UI 및 잠금 반영 완료");
    }

    private void OnClickChooseCharacter(int index)
    {
        if (myPlayerIndex < 0 || myPlayerIndex >= roomPlayerInfos.Length)
        {
            Debug.LogWarning("내 슬롯 인덱스가 올바르지 않습니다.");
            return;
        }

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
        if (roomPlayerInfos == null)
        {
            Debug.LogWarning("roomPlayerInfos 배열이 null입니다.");
            return;
        }

        Debug.Log($"UpdatePlayerInfoUI 호출 - userList.Count: {userList.Count}");

        var characterIndexMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            if (roomPlayerInfos[i] == null)
            {
                Debug.LogWarning($"roomPlayerInfos[{i}]는 이미 파괴된 오브젝트입니다.");
                continue;
            }

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
}