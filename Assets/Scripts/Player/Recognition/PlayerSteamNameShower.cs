    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Mirror;
    using Steamworks;
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
            
            [SyncVar(hook = nameof(SteamIDChanged))]
            private ulong _steamId;

            public ulong SteamId
            {
                set => _steamId = value;
            }

            #endregion

            #region Client
            public void Start()
            {
                if (!isLocalPlayer) return;
                nameText.gameObject.SetActive(false);
            }

            private void SteamIDChanged(ulong oldValue, ulong newValue)
            {
                var playerName = SteamFriends.GetFriendPersonaName(new CSteamID(newValue));
                nameText.SetText(playerName);
            }

            #endregion
        }
    }