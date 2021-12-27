using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Player.Recognition
{
    public class PlayerAvatarShower : NetworkBehaviour
    {
        #region variables

        public RawImage avatarImage;

        [SyncVar(hook = nameof(SteamIDChanged))]
        private ulong _steamId;

        private Callback<AvatarImageLoaded_t> m_avatarLoaded;

        public ulong SteamId
        {
            set => _steamId = value;
        }

        #endregion

        #region Client

        public void Start()
        {
            if (!isLocalPlayer) return;
            avatarImage.gameObject.SetActive(false);
            m_avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
        }

        private void SteamIDChanged(ulong oldValue, ulong newValue)
        {
            avatarImage.texture = LoadImage();
        }

        private Texture2D LoadImage()
        { 
            var avatar = SteamFriends.GetLargeFriendAvatar(new CSteamID(_steamId));
          if (avatar == -1) return null;

          var valid = SteamUtils.GetImageSize(avatar, out var width, out var height);
          if (!valid) return null;
          var image = new byte[width * height * 4 * sizeof(char)];

          valid = SteamUtils.GetImageRGBA(avatar, image, (int)(4 * height * width * sizeof(char)));
          if (!valid) return null;
          var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, false);
          texture.LoadRawTextureData(image);
          texture.Apply();

          return texture;

        }

        private void OnAvatarLoaded(AvatarImageLoaded_t callback)
        {
            if (callback.m_steamID.m_SteamID != _steamId) return;
            avatarImage.texture = LoadImage();

        }

        #endregion
    }
}
