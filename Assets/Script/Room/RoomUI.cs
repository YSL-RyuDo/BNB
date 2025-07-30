using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public GameObject[] roomPlayerInfos;

    public Button[] characterChooseButtons;
    public Sprite[] characterSprites;

    public Button startGameButton;
    public Button exitButton;

    private int myPlayerIndex = -1;

    [SerializeField] private RoomSender roomSender;


    private void Start()
    {

        if (!string.IsNullOrEmpty(NetworkConnector.Instance.PendingRoomEnterMessage))
        {
            string msg = NetworkConnector.Instance.PendingRoomEnterMessage;
            NetworkConnector.Instance.PendingRoomEnterMessage = null;

            // RoomReceiver가 메시지를 처리하도록 강제 호출
            FindObjectOfType<RoomReceiver>()?.HandleMessage(msg);
        }

        var client = NetworkConnector.Instance;

        if (roomNameText != null)
            roomNameText.text = client.CurrentRoomName;

        OnEnterRoomSuccess(client.CurrentRoomName, string.Join(",", client.CurrentUserList));
        startGameButton.onClick.AddListener(OnClickStartGame);
        exitButton.onClick.AddListener(OnExitRoomClicked);

        string myNick = client.UserNickname?.Trim();

        // 내 인덱스 찾기
        myPlayerIndex = client.CurrentUserList.FindIndex(nick => nick.Trim() == myNick);

        // 캐릭터 버튼 이벤트 등록
        for (int i = 0; i < characterChooseButtons.Length; i++)
        {
            int idx = i;
            characterChooseButtons[i].onClick.AddListener(() => OnClickChooseCharacter(idx));
        }

        // 기본 UI 설정
        UpdatePlayerInfoUI(client.CurrentUserList);

        // 캐릭터 정보 요청
        roomSender.SendGetCharacterInfo(myNick);
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

    public void UpdatePlayerSlots(List<string> userList)
    {
        var characterMap = NetworkConnector.Instance.CurrentUserCharacterIndices;

        for (int i = 0; i < roomPlayerInfos.Length; i++)
        {
            var slot = roomPlayerInfos[i];
            var nameText = slot.transform.Find("Image/NameText")?.GetComponent<TextMeshProUGUI>();
            var image = slot.transform.Find("PlayerCharacterImage")?.GetComponent<Image>();

            if (nameText == null || image == null) continue;

            if (i < userList.Count)
            {
                string nick = userList[i];
                nameText.text = nick;

                if (!characterMap.TryGetValue(nick, out int charIndex))
                {
                    charIndex = 0;
                    NetworkConnector.Instance.CurrentUserCharacterIndices[nick] = 0;
                }

                if (charIndex >= 0 && charIndex < characterSprites.Length)
                    image.sprite = characterSprites[charIndex];
                else
                    image.sprite = null;
            }
            else
            {
                nameText.text = "";
                image.sprite = null;
            }
        }
    }



    private void OnClickChooseCharacter(int characterIndex)
    {
        if (myPlayerIndex < 0 || myPlayerIndex >= roomPlayerInfos.Length)
        {
            Debug.LogWarning("내 슬롯 인덱스가 올바르지 않습니다.");
            return;
        }

        string nickname = NetworkConnector.Instance.UserNickname;//PlayerPrefs.GetString("nickname")?.Trim();
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        roomSender.SendChooseCharacter(nickname, roomName, characterIndex);
    }

    public void OnClickStartGame()
    {
        // 따로 복사할 필요 없이 CurrentUserCharacterIndices가 이미 최신 상태라고 가정
        // 만약 다른 변수에 데이터가 있다면 그걸 반영하는 코드를 여기에 추가

        string roomName = NetworkConnector.Instance.CurrentRoomName;

        roomSender.SendStartGame(roomName);
    }

    public void OnExitRoomClicked()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string myNickname = NetworkConnector.Instance.UserNickname;
        
        roomSender.SendExitRoom(roomName, myNickname);

        SceneManager.LoadScene("LobbyScene");
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

    public void UpdateCharacterChoice(string userNickname, int characterIndex)
    {
        NetworkConnector.Instance.SetOrUpdateUserCharacter(userNickname, characterIndex);

        // UI 즉시 반영
        UpdatePlayerInfoUI(NetworkConnector.Instance.CurrentUserList);
    }
}
