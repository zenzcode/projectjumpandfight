using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Player.Recognition;
using Unity.VisualScripting;
using UnityEngine;

namespace Network
{
    public class NetworkManager : Mirror.NetworkManager
    {
        
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            var playerObj = Instantiate(playerPrefab, new Vector3(33, 4, 60), Quaternion.identity);
            playerObj.name = NetworkServer.connections.Count + playerObj.name;
            NetworkServer.AddPlayerForConnection(conn, playerObj);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            NetworkServer.RemovePlayerForConnection(conn, true);
        }
    }   
}
