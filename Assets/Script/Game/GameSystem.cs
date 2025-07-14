using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        string roomName = NetworkConnector.Instance.CurrentRoomName;
        string selectedMap = NetworkConnector.Instance.SelectedMap;
        string getMapMsg = $"GET_MAP|{selectedMap}\n";
        byte[] getMapBytes = Encoding.UTF8.GetBytes(getMapMsg);
        await NetworkConnector.Instance.Stream.WriteAsync(getMapBytes, 0, getMapBytes.Length);
        Debug.Log(getMapMsg);
        Debug.Log("[GameSceneInitializer] 서버에 GET_MAP 요청 보냄");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
