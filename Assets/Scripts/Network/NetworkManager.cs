using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steam;
using Steamworks;
using UnityEngine;

public class NetworkManager : Mirror.NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        var playerObj = Instantiate(playerPrefab, new Vector3(80f, 100f, 50f), Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, playerObj);
    }
}
