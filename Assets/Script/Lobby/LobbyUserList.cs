using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class LobbyUserList : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;

    public GameObject userPrefab;
    public Transform userListContent;

    private async void Start()
    {
        if (NetworkConnector.Instance != null)
        {
            var stream = NetworkConnector.Instance.Stream;

            string sendUserStr = "GET_LOBBY_USER_LIST|\n";
            byte[] sendUserBytes = Encoding.UTF8.GetBytes(sendUserStr);
            await stream.WriteAsync(sendUserBytes, 0, sendUserBytes.Length);

        }
        //lobbySender.SendGetLobbyUserList();
    }

    public void UpdateUserList(string nickname, int level)
    {
        GameObject item = Instantiate(userPrefab, userListContent);
        TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
        foreach (var text in texts)
        {
            if (text.name == "NicknameText") text.text = nickname;
            else if (text.name == "LevelText") text.text = $"Lv. {level}";
        }
    }
}
