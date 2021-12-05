using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Helper
{
    public class PlayerRespawn : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                transform.position = new Vector3(33, 4, 60);
            }
        }
    }
}