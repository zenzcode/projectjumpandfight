using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MultiplayerMovement : NetworkBehaviour
{
            #region Inputs
        //player inputs
        private float _x, _y;
        private bool _isSprinting, _isCrouching, _isJumping;
        private bool _isGrounded;
        #endregion
    
        #region Components
        [Header("Components to be added from this or other GameObjects")]
        [Tooltip("Rigidbody force should be applied to")]
        public Rigidbody playerRigidbody;

        [Tooltip("Position Camera should be set to (height)")]
        public Transform headPosition;

        [Tooltip("Orientation of Player facing Forwards")]
        public Transform orientation;

        [Tooltip("Camera for this Player that should be moved with him")]
        public Camera playerCam;
        #endregion

        #region Settings
        private float _playerHeight;
        [Header("Player Crouch Settings")]
        [Tooltip("Player Height when crouched")]
        public float playerCrouchHeight = 0.3f;


        [Header("Settings for Player Movement")] 
        [Tooltip("Max angle a player can walk up ")]
        public float maxSlope = 50f;

        [Tooltip("Force multiplier for when player is walking")]
        public float walkMultiplier = 400f;

        [Tooltip("Force multiplier for when player is sprinting")]
        public float sprintMultiplier = 500f;
        
        [Tooltip("Extra Gravity multiplier to add")]
        public float extraGravityMultiplicator = 11;

        [Tooltip("Player max speed")] public float playerMaxSpeed = 4000;

        [Tooltip("Jump Multiplier")] public float jumpMultiplier = 250f;

        [Header("Mouse Settings")] 
        [Tooltip("Mouse Sensitivity")]
        public float mouseSensitivity = 1.5f;

        [Tooltip("Multiplier for Sensitivity")]
        public float sensitivityMultiplier = 1f;

        [Header("Coyote Time Settings")] [Tooltip("Sets time in Seconds for Coyote time")]
        public float coyoteTime = 3f;

        [Header("Jump Settings")] [Tooltip("Time after a jump until jump is reactivated")]
        private float jumpDelay = 3f;

        [Header("Counter Movement Settings")]
        [Tooltip("Counter Movement Modifier, Slows down or fastens Counter Movement")]
        public float counterModifier = 0.3f;
        #endregion
        
        #region Data
        //Used to avoid jittery rotation
        private float _rotationX;
        //determines if player is ready to jump
        private bool _readyToJump;
        #endregion

    
    #region Server
    

    //called on client, executed on server
    [Command]
    private void CmdDoMove(float x, float y)
    {
        print("Sending Inputs to Server");
        if (Mathf.Abs(x) > 1 || Mathf.Abs(y) > 1) return;
        PhysicBasedMovement(x, y);
    }

    #endregion

    #region Functions

    //Actual Movement Function which handles counter movement, speed, ...
    private void PhysicBasedMovement(float x, float y)
    {
        //Add gravity downwards to prevent flying to the mars
        playerRigidbody.AddForce(Vector3.down * (Time.deltaTime * extraGravityMultiplicator));

        // if (_readyToJump && _isJumping)
        // {
        //     PerformJump();
        // }
        //
        // var mag = FindVelRelativeToLook();
        //     
        //stop slithery sliding a bit before adding force again
        // CounterMovement(mag);

        // if (_x > 0 && mag.x > playerMaxSpeed) _x = 0;
        // if (_x < 0 && mag.x < -playerMaxSpeed) _x = 0;
        // if (_y > 0 && mag.y > playerMaxSpeed) _y = 0;
        // if (_y < 0 && mag.y < -playerMaxSpeed) _y = 0;
            
        playerRigidbody.AddForce(orientation.transform.forward * (y * Time.deltaTime * walkMultiplier));
        playerRigidbody.AddForce(orientation.transform.right * (x * Time.deltaTime * walkMultiplier));
    }
    
    private void CheckInput()
    {
        _x = Input.GetAxis(Misc.InputAxis.Horizontal);
        _y = Input.GetAxis(Misc.InputAxis.Vertical);
            
        //TODO: Make Keys Configurable
        _isSprinting = Input.GetKeyDown(KeyCode.LeftShift);
        _isCrouching = Input.GetKeyDown(KeyCode.LeftControl);
        _isJumping = Input.GetKey(KeyCode.Space);
        PhysicBasedMovement(_x, _y);
        CmdDoMove(_x, _y);
    }

    #endregion

    #region Client

    //activate camera only if local player to avoid switching to latest cam
    private void Start()
    {
        print(isLocalPlayer);
        if (!isLocalPlayer) return;
        playerCam.gameObject.SetActive(true);
    }


    private void Update()
    {
        CheckInput();
    }

    #endregion
}
