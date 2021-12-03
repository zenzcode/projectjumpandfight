using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Physic Based Movement for Jump and Fight

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movement : SingletonMonoBehaviour<Player.Movement>
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

        #region Functions Movement Related
        protected override void Awake()
        {
            base.Awake();
            _playerHeight = transform.localScale.y;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _readyToJump = true;
        }

        private void Update()
        {
            CheckInput();
            CallInputFunctions();
            MouseLook();
        }

        //function that handles functionality to start crouching
        private void StartCrouch()
        {
            var crouchScale = transform.localScale;
            crouchScale.y = playerCrouchHeight;
            transform.localScale = crouchScale;
            transform.position = new Vector3(transform.position.x, transform.position.y - playerCrouchHeight, transform.position.z);
        }

        //function that handles functionally to stop crouching
        private void StopCrouch()
        {
            var standScale = transform.localScale;
            standScale.y = _playerHeight;
            transform.localScale = standScale;
            transform.position = new Vector3(transform.position.x, transform.position.y + playerCrouchHeight, transform.position.z);
        }

        //Adds some counter movement
        private void CounterMovement(Vector2 relativeVelocity)
        {            
            //handling letting go of key,                                       switch directions
            if (Math.Abs(relativeVelocity.x) > 0.01f && Math.Abs(_x) < 0.05f || relativeVelocity.x < -0.01f && _x > 0 || relativeVelocity.x > 0.01f && _x < 0)
            {
                playerRigidbody.AddForce(orientation.transform.right * (walkMultiplier * (Time.deltaTime * -relativeVelocity.x * counterModifier)));
            }
            
            //handling letting go of key,                                       switch directions
            if (Math.Abs(relativeVelocity.y) > 0.01f && Math.Abs(_y) < 0.05f || relativeVelocity.y < -0.01f && _y > 0 || relativeVelocity.y > 0.01f && _y < 0)
            {
                playerRigidbody.AddForce(orientation.transform.forward * (walkMultiplier * (Time.deltaTime * -relativeVelocity.y * counterModifier)));
            }
        }

        //Calculates and sets new Mouse position
        private void MouseLook()
        {
            var currentRotation = playerCam.transform.localRotation.eulerAngles;
            var mouseXInput = Input.GetAxisRaw(Misc.InputAxis.MouseX);
            var mouseYInput = -Input.GetAxisRaw(Misc.InputAxis.MouseY);

            //Calculate new value for Y-Axis rotation
            float desiredY = 0;
            desiredY = currentRotation.y + (mouseXInput * Time.deltaTime * sensitivityMultiplier * mouseSensitivity);

            //Calculate new Value for X-Axis rotation
            _rotationX += (mouseYInput * Time.deltaTime * sensitivityMultiplier * mouseSensitivity);
            //Clamp angle so we cant do 360* up/down rotations
            _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);

            //set new localRotation to a rotation rotated n degrees about axis 
            playerCam.transform.localRotation = Quaternion.Euler(_rotationX, desiredY, 0);
            //set orientation to be facing forward in new direction
            orientation.transform.localRotation = Quaternion.Euler(0, desiredY, 0);
        }
        
        //Checks and Performs jump
        private void PerformJump()
        {
            if (!_readyToJump || !_isGrounded) return;
            //Add Jump Force to player
            playerRigidbody.AddForce(Vector3.up * (jumpMultiplier * Time.deltaTime));
            _readyToJump = false;

            Invoke(nameof(ReactivateJump), jumpDelay);
        }
        
        //Reactivates Jump ability after a defined delay
        private void ReactivateJump()
        {
            _readyToJump = true;
        }

        #endregion
        
        #region Getters Setters and Checks

        private void CheckInput()
        {
            _x = Input.GetAxis(Misc.InputAxis.Horizontal);
            _y = Input.GetAxis(Misc.InputAxis.Vertical);
            
            //TODO: Make Keys Configurable
            _isSprinting = Input.GetKeyDown(KeyCode.LeftShift);
            _isCrouching = Input.GetKeyDown(KeyCode.LeftControl);
            _isJumping = Input.GetKey(KeyCode.Space);
        }

        private void CallInputFunctions()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                StartCrouch();
            }
            else if(Input.GetKeyUp(KeyCode.LeftControl))
            {
                StopCrouch();
            }
        }
        
        //kind of stolen from karlson but it works
        //not good at physics nor maths, ngl
        private Vector2 FindVelRelativeToLook()
        {
            var lookAngle = orientation.transform.eulerAngles.y;
            var moveAngle = Mathf.Atan2(playerRigidbody.velocity.x, playerRigidbody.velocity.z) * Mathf.Rad2Deg;

            var u = Mathf.DeltaAngle(lookAngle, moveAngle);
            var v = 90 - u;

            var magnitude = playerRigidbody.velocity.magnitude;
            var magY = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
            var magX = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

            return new Vector2(magX, magY);
        }

        //Resets grounded state
        private void ResetGrounded()
        {
            _isGrounded = false;
        }

        private bool IsSlopeWalkable(Vector3 normal)
        {
            return Vector3.Angle(Vector3.up, normal) < maxSlope;
        }

        #endregion
    }
}