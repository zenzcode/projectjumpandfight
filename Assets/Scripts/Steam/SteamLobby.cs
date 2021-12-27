using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Misc.Singletons;
using Network;
using Steamworks;
using UnityEngine;
using NetworkManager = Mirror.NetworkManager;

public class SteamLobby : SingletonMonoBehaviour<SteamLobby>
{
    #region variables

    [Header("Setters to be removed when start")]
    public GameObject[] buttons;

    private const string HostAddressKey = "HostAddress";
    private NetworkManager m_networkManager;

    public CSteamID SteamLobbyId { get; private set; }

    #endregion
    
    #region SteamCallbacks

    private Callback<LobbyCreated_t> m_lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> m_joinRequested;
    private Callback<LobbyEnter_t> m_lobbyEnter;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        print("Steam Lobby Instance created");  
    }

    private void Start()
    {
        m_networkManager = GameObject.Find("SteamNetworkManager").GetComponent<NetworkManager>();
        if (!SteamManager.Initialized) return;
        m_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        m_joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        m_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void Host()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, m_networkManager.maxConnections);
        foreach (var button in buttons)
        {
            button.SetActive(false);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            foreach (var button in buttons)
            {
                button.SetActive(true);
            }
            return;
        }

        m_networkManager.StartHost();
        SteamLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey,
            SteamUser.GetSteamID().ToString());
    }

    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamLobbyId = callback.m_steamIDLobby;
        SteamMatchmaking.JoinLobby(SteamLobbyId);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        //If we already have a server running on our machine we dont want to join a lobby
        if (NetworkServer.active) return;

        var hostAddress = SteamMatchmaking.GetLobbyData(SteamLobbyId, HostAddressKey);
        m_networkManager.networkAddress = hostAddress;
        m_networkManager.StartClient();
        

        foreach (var button in buttons)
        {
            button.SetActive(false);
        }
    }
}
