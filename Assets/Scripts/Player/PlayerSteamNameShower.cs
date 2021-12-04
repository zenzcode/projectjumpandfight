using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Player
{
    public class PlayerSteamNameShower : NetworkBehaviour
    {
        #region variables

        public TMP_Text nameText;
        
        [SyncVar(hook = nameof(PlayerNameChanged))]
        private string _playerName;


        #endregion

        public void Start()
        {
            _playerName = SteamClient.Name;
        }

        private void PlayerNameChanged(string oldValue, string newValue)
        {
            nameText.text = newValue;
        }
    }
}