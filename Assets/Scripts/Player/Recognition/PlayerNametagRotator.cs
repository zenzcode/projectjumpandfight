using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Player.Recognition{
    public class PlayerNametagRotator : NetworkBehaviour
    {
        #region variables

        private PlayerNametagRotator[] _playerNametags;
        private Dictionary<PlayerNametagRotator, TMP_Text> _nametagTexts;

        [Header("Nametag settings")] [Tooltip("Nametag max distance to be still visible")]
        public float maxNametagDistance = 100f;

        #endregion

        public void Start()
        {
            if (!isLocalPlayer) return;
            NetworkManager.PlayerConnectedEvent += ReplaceNametags;
        }

        private void ReplaceNametags()
        {
            _nametagTexts = new Dictionary<PlayerNametagRotator, TMP_Text>();
            //Get all Nametag Objects that are in scene and active, because we dont want to affect our own nametag
            _playerNametags = GameObject.FindObjectsOfType<PlayerNametagRotator>().Where(nameTag => nameTag.gameObject.activeInHierarchy).ToArray();
            //Get all texts from gameobjects
            foreach (var player in _playerNametags)
            {
                var nameTagText = player.GetComponentInChildren<TMP_Text>();
                _nametagTexts.Add(player, nameTagText);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            foreach (var player in _playerNametags)
            {
                if (!_nametagTexts.TryGetValue(player, out var text)) return;
                //scales text based on distance
                var nameTagPlayerPos = player.transform.position;
                var distanceToPlayer = Vector3.Distance(transform.position, nameTagPlayerPos);
                var nameTagSize = 1 -  (distanceToPlayer / maxNametagDistance);
                text.transform.localScale = new Vector3(nameTagSize, nameTagSize, -nameTagSize);
                //Calculate Rotation based on difference in x,z position
                var newRotation = GetRotationRelativeToPlayerPos(nameTagPlayerPos);
                text.transform.localRotation = Quaternion.Euler(newRotation);
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

