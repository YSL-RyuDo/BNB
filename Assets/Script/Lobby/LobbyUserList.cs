using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LobbyUserList : MonoBehaviour
{
    [SerializeField] private LobbySender lobbySender;

    public GameObject userPrefab;
    public Transform userListContent;

    public GameObject userPage;

    public static string LastRequestedNickname;

    private void Start()
    {
        lobbySender.SendGetLobbyUserList();
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

        var btn = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(OnClickUserButton_NoParam);
        }
        else
        {
            Debug.LogWarning($"[LobbyUserList] ��ư�� ã�� ���߽��ϴ�: {item.name}");
        }
    }

    private void OnClickUserButton_NoParam()
    {
        var clicked = EventSystem.current?.currentSelectedGameObject;
        if (clicked == null)
        {
            Debug.LogWarning("[LobbyUserList] currentSelectedGameObject�� null�Դϴ�.");
            return;
        }

        // Ŭ���� ������Ʈ �������� ����/������ Ž���ϸ� "NicknameText"�� ã��
        string nickname = FindNicknameFrom(clicked.transform);
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("[LobbyUserList] NicknameText�� ã�� ���߽��ϴ�.");
            return;
        }
        LastRequestedNickname = nickname.Trim();

        userPage.SetActive(true);
        lobbySender.SendGetUserPageInfo(nickname.Trim());
        Debug.Log($"[LobbyUserList] ���� ������ ��û: {nickname}");
    }
    private string FindNicknameFrom(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            var texts = t.GetComponentsInChildren<TMP_Text>(true);
            foreach (var tx in texts)
            {
                if (tx != null && tx.name == "NicknameText")
                    return tx.text;
            }
            t = t.parent; // �� �ܰ� ���� �ö󰡼� �ٽ� �õ� (��ư�� �������� ������ �� �����Ƿ�)
        }
        return null;
    }
    public void OnClickUserButton(string nickname)
    {
        LastRequestedNickname = nickname?.Trim();
        userPage.SetActive(true);
        lobbySender.SendGetUserPageInfo(nickname);
        Debug.Log($"[LobbyUserList] ���� ������ ��û: {LastRequestedNickname}");
    }

}
