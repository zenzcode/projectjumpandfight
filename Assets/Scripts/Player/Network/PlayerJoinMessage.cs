
using UnityEngine;

namespace Player.Network
{
    internal struct PlayerJoinMessage : Mirror.NetworkMessage
    {
        public GameObject[] players;
    }
}
