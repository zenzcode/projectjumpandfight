using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FollowCamera
{
    public class Movement : MonoBehaviour
    {
        #region Assignable

        public Player.Movement.Movement player;

        #endregion

        // Update is called once per frame
        void Update()
        {
            transform.position = player.headPosition.position;
        }
    }

}