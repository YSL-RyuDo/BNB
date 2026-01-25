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
            Debug.LogWarning($"[LobbyUserList] 버튼을 찾지 못했습니다: {item.name}");
        }
    }

    private void OnClickUserButton_NoParam()
    {
        ButtonSoundManager.Instance?.PlayClick();
        var clicked = EventSystem.current?.currentSelectedGameObject;
        if (clicked == null)
        {
            Debug.LogWarning("[LobbyUserList] currentSelectedGameObject가 null입니다.");
            return;
        }

        // 클릭된 오브젝트 기준으로 하위/상위를 탐색하며 "NicknameText"를 찾음
        string nickname = FindNicknameFrom(clicked.transform);
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("[LobbyUserList] NicknameText를 찾지 못했습니다.");
            return;
        }
        LastRequestedNickname = nickname.Trim();

        userPage.SetActive(true);
        lobbySender.SendGetUserPageInfo(nickname.Trim());
        Debug.Log($"[LobbyUserList] 유저 페이지 요청: {nickname}");
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
            t = t.parent; // 한 단계 위로 올라가서 다시 시도 (버튼이 프리팹의 하위일 수 있으므로)
        }
        return null;
    }
    public void OnClickUserButton(string nickname)
    {
        ButtonSoundManager.Instance?.PlayClick();
        LastRequestedNickname = nickname?.Trim();
        userPage.SetActive(true);
        lobbySender.SendGetUserPageInfo(nickname);
        Debug.Log($"[LobbyUserList] 유저 페이지 요청: {LastRequestedNickname}");
    }

}
