using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Player.Recognition
{
    public class PlayerSteamNameShower : NetworkBehaviour
    {
        #region variables

        public TMP_Text nameText;
        
        [SyncVar(hook = nameof(PlayerNameChanged))]
        private string _playerName;


        #endregion

        #region Client
        public void Start()
        {
            if (!isLocalPlayer) return;
            //Set Name to steam name if player is local player
            _playerName = SteamClient.Name;
            //Deactivate nametag for own player
            nameText.gameObject.SetActive(false);
        }

        private void PlayerNameChanged(string oldValue, string newValue)
        {
            nameText.text = newValue;
        }
        #endregion
    }
}