    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Mirror;
    using TMPro;
    using UnityEngine;
    using UnityEngine.AI;
    using Random = UnityEngine.Random;

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
                nameText.gameObject.SetActive(false);
            }

            private void PlayerNameChanged(string oldValue, string newValue)
            {
                string oldNew = newValue;
                newValue = "skrrr" + oldNew;
                nameText.text = newValue;
            }

            #endregion
        }
    }