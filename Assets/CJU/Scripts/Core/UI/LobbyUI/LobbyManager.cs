using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public UserInfoUI userInfoUI;
    public LobbyChatUI lobbyChatUI;

    public Transform userList;
    public GameObject userListPrefab;

    public Transform roomList;
    public GameObject roomButtonPrefab;

    public Button logoutButton;

    // Start is called before the first frame update
    void Awake()
    {
        string nickname = NetworkConnector.Instance.UserNickname;
        SendToServer($"GET_USER_INFO|{nickname}\n");
        SendToServer("GET_LOBBY_USER_LIST|\n");
        SendToServer("GET_ROOM_LIST|\n");
    }

    private void Start()
    {
        logoutButton.onClick.AddListener(OnLogoutClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnReceiveMessage(string message)
    {
        if (message.StartsWith("USER_INFO|"))
        {
            string data = message.Substring("USER_INFO|".Length);
            string[] tokens = data.Split(',');

            string nickname = tokens[0];
            int level = int.Parse(tokens[1]);
            float exp = float.Parse(tokens[2]);

            userInfoUI.SetUserInfo(nickname, level, exp);
        }
        else if (message.StartsWith("USER_NOT_FOUND"))
        {
            Debug.LogWarning("유저 정보를 찾을 수 없습니다.");
        }
        else if (message.StartsWith("LOBBY_USER_LIST|"))
        {
            string data = message.Substring("LOBBY_USER_LIST|".Length);
            string[] users = data.Split('|');

            foreach (Transform child in userList)
            {
                Destroy(child.gameObject);
            }
            foreach (string userInfo in users)
            {
                string[] tokens = userInfo.Split(',');
                if (tokens.Length < 2) continue;

                string nickname = tokens[0];
                string levelStr = tokens[1];

                if (int.TryParse(levelStr, out int level))
                {
                    GameObject userButton = Instantiate(userListPrefab, userList);

                    // 텍스트 설정
                    TextMeshProUGUI[] texts = userButton.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var text in texts)
                    {
                        if (text.name == "NicknameText") text.text = nickname;
                        else if (text.name == "LevelText") text.text = $"Lv. {level}";
                    }
                }
            }
        }
        else if (message.StartsWith("ROOM_LIST|"))
        {
            string data = message.Substring("ROOM_LIST|".Length);
            string[] rooms = data.Split('|');

            // 기존 방 리스트 초기화
            foreach (Transform child in roomList)
            {
                Destroy(child.gameObject);
            }

            // 최대 6개까지만 생성
            int count = Mathf.Min(rooms.Length, 6);
            for (int i = 0; i < count; i++)
            {
                string[] tokens = rooms[i].Split(',');
                if (tokens.Length < 3) continue;

                string roomName = tokens[0];
                string mapName = tokens[1];
                bool hasPassword = bool.Parse(tokens[2]);

                // 방 버튼 생성
                GameObject btn = Instantiate(roomButtonPrefab, roomList);
                RoomButtonUI roomUI = btn.GetComponent<RoomButtonUI>();
            }
        }
        else if (message.StartsWith("LOBBY_CHAT|"))
        {
            string chat = message.Substring("LOBBY_CHAT|".Length);
            lobbyChatUI.AddChatMessage(chat);
        }
    }

    void OnLogoutClicked()
    {
        string nickname = NetworkConnector.Instance.UserNickname;
        string logoutMsg = $"LOGOUT|{nickname}\n";

        SendToServer(logoutMsg);

        NetworkConnector.Instance.UserNickname = null;

        SceneManager.LoadScene("LoginScene");
    }

    private async void SendToServer(string msg)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        await NetworkConnector.Instance.Stream.WriteAsync(bytes, 0, bytes.Length);
        Debug.Log("유저 정보 요청" + msg);

    }

}
