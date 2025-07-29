using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyRoomData
{
    public string RoomName { get; private set; }
    public string MapName { get; private set; }
    public bool HasPassword { get; private set; }

    public LobbyRoomData(string roomName, string mapName, bool hasPassword)
    {
        RoomName = roomName;
        MapName = mapName;
        HasPassword = hasPassword;
    }
}
