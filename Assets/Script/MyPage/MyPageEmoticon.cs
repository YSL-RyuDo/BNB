using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MyPageEmoticon : MonoBehaviour
{
    [SerializeField] private MyPageSender myPageSender;

    private string nickName;
    public static EmoticonSystem Instance;
    public Sprite[] emoticonPrefabs;           // �ε����� ������ �̸�Ƽ�� �̹��� �迭
    public Button[] emoticonButtons;           // �̸�Ƽ�� ��ư 4��

    // Start is called before the first frame update
    void Start()
    {
        nickName = NetworkConnector.Instance.UserNickname;

        myPageSender.SendGetEmoticon(nickName);
    }
}
