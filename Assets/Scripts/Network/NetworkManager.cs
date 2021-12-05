using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steam;
using Steamworks;
using UnityEngine;

public class NetworkManager : Mirror.NetworkManager
{
    public static Action PlayerConnectedEvent;

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        var playerObj = Instantiate(playerPrefab, new Vector3(33, 4, 60), Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, playerObj);
        PlayerConnectedEvent?.Invoke();
    }
}
