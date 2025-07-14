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
    public Button createRoomButton;

    public GameObject roomCreatePanel;

    private Dictionary<string, bool> roomPasswordMap = new Dictionary<string, bool>();

    public GameObject enterRoomPanel;
    public RoomButtonUI[] roomSlots;

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
        createRoomButton.onClick.AddListener(OpenCreateRoomPanel);
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
            string[] rooms = string.IsNullOrWhiteSpace(data) ? new string[0] : data.Split('|');

            roomPasswordMap.Clear();

            // Step 1: 6개 슬롯 전부 초기화
            for (int i = 0; i < roomSlots.Length; i++)
            {
                roomSlots[i].Clear();
            }

            // Step 2: 받아온 방 수만큼 0번부터 덮어쓰기
            int count = Mathf.Min(rooms.Length, roomSlots.Length);
            for (int i = 0; i < count; i++)
            {
                string[] parts = rooms[i].Split(',');
                if (parts.Length < 3) continue;

                string roomName = parts[0];
                string mapName = parts[1];
                bool hasPassword = parts[2].Trim() == "1";

                roomPasswordMap[roomName] = hasPassword;

                int index = i;
                roomSlots[i].SetInfo(roomName, () =>
                {
                    NetworkConnector.Instance.CurrentRoomName = roomName;
                    if (roomPasswordMap.TryGetValue(roomName, out bool isLocked) && isLocked)
                    {
                        enterRoomPanel.SetActive(true);
                    }
                    else
                    { 
                        string enterMsg = $"ENTER_ROOM|{roomName}|\n";
                        SendToServer(enterMsg);
                    }
                });
            }
        }
        else if (message.StartsWith("LOBBY_CHAT|"))
        {
            string chat = message.Substring("LOBBY_CHAT|".Length);
            lobbyChatUI.AddChatMessage(chat);
        }
        else if (message.StartsWith("CREATE_ROOM_SUCCESS|"))
        {
            string[] parts = message.Split('|');
            string roomName = parts[1];
            string mapName = parts[2];
            string role = parts[3]; // CREATOR
            string userListStr = parts[4];
            string hasPasswordStr = parts[5];

            // 유저 정보 저장
            NetworkConnector.Instance.CurrentRoomName = roomName;
            NetworkConnector.Instance.CurrentMap = mapName;
            NetworkConnector.Instance.IsRoomOwner = true;
            NetworkConnector.Instance.IsRoomPassworded = hasPasswordStr == "1";
            List<string> userList = new List<string>(userListStr.Split(','));
            NetworkConnector.Instance.CurrentUserList = userList;
            // 씬 이동
            SceneManager.LoadScene("RoomScene");
        }
        else if (message.StartsWith("ROOM_CREATED|"))
        {
            Debug.Log($"{message}");
        }
        else if (message.StartsWith("ENTER_ROOM_SUCCESS|"))
        {
            string[] parts = message.Split('|');
            string roomName = parts[1];
            string userListStr = parts[2];

            NetworkConnector.Instance.CurrentRoomName = roomName;
            NetworkConnector.Instance.CurrentUserList = new List<string>(userListStr.Split(','));

            SceneManager.LoadScene("RoomScene");
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

    private void OpenCreateRoomPanel()
    {
        roomCreatePanel.SetActive(true);
    }

}
