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
    public Sprite[] emoticonPrefabs;           // 인덱스로 접근할 이모티콘 이미지 배열
    public Button[] emoticonButtons;           // 이모티콘 버튼 4개

    // Start is called before the first frame update
    void Start()
    {
        nickName = NetworkConnector.Instance.UserNickname;

        myPageSender.SendGetEmoticon(nickName);
    }
}
