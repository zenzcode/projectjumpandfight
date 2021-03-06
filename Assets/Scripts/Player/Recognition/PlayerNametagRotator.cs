using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Network;
using Player.Network;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Player.Recognition{
    public class PlayerNametagRotator : NetworkBehaviour
    {
        #region variables
        private Dictionary<GameObject, GameObject> _nametagTexts;
        private List<GameObject> _players;

        [Header("Nametag settings")] [Tooltip("Nametag max distance to be still visible")]
        public float maxNametagDistance = 100f;

        private Text _debugText;
        
        #endregion

        public void Start()
        {
            if (!isLocalPlayer) return;
            NetworkClient.RegisterHandler<PlayerJoinMessage>(ReplaceNametags);
            CmdNewPlayerConnected();
        }

        [Command]
        private void CmdNewPlayerConnected()
        {
            var allPlayers = GameObject.FindGameObjectsWithTag("Player");
            var playerJoinMessage = new PlayerJoinMessage
            {
                players = allPlayers
            };
            NetworkServer.SendToAll(playerJoinMessage);
        }

        private void ReplaceNametags(PlayerJoinMessage playerJoinMessage)
        {
            _nametagTexts = new Dictionary<GameObject, GameObject>();
            _players = playerJoinMessage.players.ToList();
            foreach (var player in _players)
            {
                var nameTag = player.GetComponentInChildren<HorizontalLayoutGroup>(true).gameObject;
                if (nameTag == null)
                {
                    nameTag.gameObject.SetActive(false);
                    continue;
                }
                _nametagTexts.Add(player, nameTag);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;
            if (_players == null) return;

            foreach (var player in _players)
            {
                if (!_nametagTexts.TryGetValue(player, out var nameTag)) return;
                if (nameTag == null) return;
                //scales text based on distance
                var nameTagPlayerPos = player.transform.position;
                var distanceToPlayer = Vector3.Distance(transform.position, nameTagPlayerPos);
                var nameTagSize = 1 -  (distanceToPlayer / maxNametagDistance);
                nameTag.transform.localScale = new Vector3(nameTagSize, nameTagSize, -nameTagSize);
                //Calculate Rotation based on difference in x,z position
                var newRotation = GetRotationRelativeToPlayerPos(nameTagPlayerPos);
                nameTag.transform.localRotation = Quaternion.Euler(newRotation);
            }
        }

        //Returns Rotation that is facing player
        private Vector3 GetRotationRelativeToPlayerPos(Vector3 otherPos)
        {
            var differenceVector = (otherPos - transform.position).normalized;
            var desiredAngleY = Mathf.Atan2(differenceVector.x, differenceVector.z);
            return new Vector3(0, desiredAngleY, 0) * Mathf.Rad2Deg;
        }
    }
}

