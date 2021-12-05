using UnityEngine;

namespace Player.Network
{
    public struct PlayerState
    {
        private Vector2 _input;
        private Vector3 _position;
        private float _deltaTime;

        public PlayerState(Vector2 input, Vector3 position, float deltaTime)
        {
            _input = input;
            _position = position;
            _deltaTime = deltaTime;
        }

        public Vector2 Input => _input;
        public Vector3 Position => _position;
        public float DeltaTime => _deltaTime;
    }   
}
