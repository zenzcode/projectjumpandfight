using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Player.Recognition;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;

namespace Network
{
    public class NetworkManager : Mirror.NetworkManager
    {
        
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            var playerObj = Instantiate(playerPrefab, new Vector3(33, 4, 60), Quaternion.identity);            
            playerObj.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, playerObj);

            var steamID = SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.Instance.SteamLobbyId, numPlayers - 1);

            var steamNameShower = conn.identity.GetComponent<PlayerSteamNameShower>();
            if (steamNameShower == null) return;
            steamNameShower.SteamId = steamID.m_SteamID;
            
            var steamAvatarShower = conn.identity.GetComponent<PlayerAvatarShower>();
            if (steamAvatarShower == null) return;
            steamAvatarShower.SteamId = steamID.m_SteamID;
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            NetworkServer.RemovePlayerForConnection(conn, true);
        }
    }   
}
