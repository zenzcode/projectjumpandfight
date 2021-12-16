using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Mirror;
using Misc;
using Network;
using Player.Network;
using UnityEngine;

//Physic Based Movement for Jump and Fight

namespace Player.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movement : NetworkBehaviour
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
        public float jumpDelay = 3f;

        [Header("Counter Movement Settings")]
        [Tooltip("Counter Movement Modifier, Slows down or fastens Counter Movement")]
        public float counterModifier = 0.3f;

        [Tooltip("Sliding Counter Movement Modifier")]
        public float slidingCounterMovement = 0.1f;

        [Header("Ground Settings")] [Tooltip("Layer(s) that is/are ground")]
        public LayerMask groundLayer;

        [Header("Network Settings")] [Range(0.1f, 1f)]public float networkSendRate = 0.5f;
        [SerializeField] public bool isPredictionEnabled;
        [SerializeField] public float correctionTreshold;
        #endregion
        
        #region Data
        //Used to avoid jittery rotation
        private float _rotationX;
        //determines if player is ready to jump
        private bool _readyToJump;

        //holds what we have done and predicts what outcome on server will be
        private List<PackagePlayerMovementReceive> _predictedPackages;
        private Vector3 _lastPosition;
        #endregion
        
        #region Network

        private PackageManager<PackagePlayerMovement> m_playerMovementManager;
        private PackageManager<PackagePlayerMovementReceive> m_receiveMovementManager;
        #endregion


        #region Functions Movement Related
        protected void Awake()
        {
            _playerHeight = transform.localScale.y;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _readyToJump = true;
        }

        private void Start()
        {
            m_playerMovementManager = new PackageManager<PackagePlayerMovement>();
            m_receiveMovementManager = new PackageManager<PackagePlayerMovementReceive>();
            m_playerMovementManager.SendSpeed = networkSendRate;
            m_receiveMovementManager.SendSpeed = networkSendRate;
            _predictedPackages = new List<PackagePlayerMovementReceive>();
            if(isServer)
                m_receiveMovementManager.OnRequirePackageTransmit += TransmitPackageToClients;
            if (!isLocalPlayer) return;
            m_playerMovementManager.OnRequirePackageTransmit += TransmitPackageToServer;
            playerCam.gameObject.SetActive(true);
        }

        private void TransmitPackageToServer(byte[] data)
        {
            CmdTransmitPackages(data);
        }

        private void TransmitPackageToClients(byte[] data)
        {
            RpcReceiveDataOnClient(data);
        }

        //Called from Client, Executed on Server (call to server)
        [Command]
        private void CmdTransmitPackages(byte[] data)
        {
            m_playerMovementManager.ReceiveData(data);
        }

        //called on server, executed on client
        [ClientRpc]
        private void RpcReceiveDataOnClient(byte[] data)
        {
            m_receiveMovementManager.ReceiveData(data);
        }

        private void Update()
        {
            m_playerMovementManager.Tick();
            m_receiveMovementManager.Tick();
            LocalClientUpdate();
            ServerUpdate();
            RemoteClientUpdate();
        }

        [ServerCallback]
        private void ServerUpdate()
        {
            if (!isServer || isLocalPlayer) return;

            var nextPackage = m_playerMovementManager.GetNextDataReceive();

            if (nextPackage == null) return;
            
            PhysicsMovement(nextPackage.x, nextPackage.y);
            MouseLook(nextPackage.mouseX, nextPackage.mouseY);
            if (transform.position == _lastPosition) return;
            _lastPosition = transform.position;
            m_receiveMovementManager.AddPackage(new PackagePlayerMovementReceive
            {
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z,
                timestamp = nextPackage.timestamp
            });
        }

        [ClientCallback]
        private void LocalClientUpdate()
        {
            if (!isLocalPlayer) return;
            CheckInput(); 
            CallInputFunctions();
            var mouseX = Input.GetAxisRaw(Misc.Other.InputAxis.MouseX);
            var mouseY = Input.GetAxisRaw(Misc.Other.InputAxis.MouseY);
            MouseLook(mouseX, -mouseY);

            if (!isPredictionEnabled) return;
            PhysicsMovement(Input.GetAxis(Misc.Other.InputAxis.Horizontal), Input.GetAxis(Misc.Other.InputAxis.Vertical));
            _predictedPackages.Add(new PackagePlayerMovementReceive
            {
                timestamp = Time.time,
                x = transform.position.x, 
                y = transform.position.y,
                z = transform.position.z
            });
        }

        private void RemoteClientUpdate()
        {
            if (isServer) return;

            var data = m_receiveMovementManager.GetNextDataReceive();
            if (data == null) return;

            if (isLocalPlayer && isPredictionEnabled)
            {
                var transmittedPackage = _predictedPackages.FirstOrDefault(x => Math.Abs(x.timestamp - data.timestamp) < 0.1f);
                if (transmittedPackage == null) return;
                if (Vector3.Distance(new Vector3(transmittedPackage.x, transmittedPackage.y, transmittedPackage.z),
                    new Vector3(data.x, data.y, data.z)) > correctionTreshold)
                {
                    transform.position = new Vector3(data.x, data.y, data.z);
                }

                _predictedPackages.RemoveAll(x => x.timestamp <= data.timestamp);
            }
            else
            {
                transform.position = new Vector3(data.x, data.y, data.z);
            }
        }

        //handles actual movement
        private void PhysicsMovement(float x, float y)
        {
            var startingPos = playerRigidbody.position;
            //add extra gravity downwards
            playerRigidbody.AddForce(Vector3.down * (Time.deltaTime * extraGravityMultiplicator));

            //if grounded and jump key pressed, perform jump
            if (_isGrounded && _isJumping)
            {
                PerformJump();
            }
            
            //calculate velocity relative to look 
            var mag = FindVelRelativeToLook();
            
            //perform counter movement so we dont go yeet into space
            CounterMovement(mag);

            //check if we go brrrr, then reset input
            if (x > 0 && mag.x > playerMaxSpeed) x = 0;
            if (x < 0 && mag.x < -playerMaxSpeed) x = 0;
            if (y > 0 && mag.y > playerMaxSpeed) y = 0;
            if (y < 0 && mag.y < -playerMaxSpeed) y = 0;

            var multiplierX = 1f;
            var multiplierY = 1f;

            if (!_isGrounded)
            {
                multiplierX = 0.5f;
                multiplierY = 0.5f;
            }
            
            //when sliding
            if (_isGrounded && _isCrouching)
            {
                multiplierY = 0f;
            }
            
            //add actual movement force
            playerRigidbody.AddForce(orientation.transform.right * (x  * Time.deltaTime * (walkMultiplier * multiplierX * multiplierY)));
            playerRigidbody.AddForce(orientation.transform.forward * (y * Time.deltaTime * (walkMultiplier * multiplierY)));
        }

        //function that handles functionality to start crouching
        private void StartCrouch()
        {
            _isCrouching = true;
            var crouchScale = transform.localScale;
            crouchScale.y = playerCrouchHeight;
            transform.localScale = crouchScale;
            transform.position = new Vector3(transform.position.x, transform.position.y - playerCrouchHeight, transform.position.z);
        }

        //function that handles functionally to stop crouching
        private void StopCrouch()
        {
            _isCrouching = false;
            var standScale = transform.localScale;
            standScale.y = _playerHeight;
            transform.localScale = standScale;
            transform.position = new Vector3(transform.position.x, transform.position.y + playerCrouchHeight, transform.position.z);
        }

        //Adds some counter movement
        private void CounterMovement(Vector2 relativeVelocity)
        {
            //no counter movement when in air
            if (!_isGrounded || _isJumping) return;
            
            //sliding should not be that heavy slowed down as other
            if (_isCrouching)
            {
                playerRigidbody.AddForce(walkMultiplier * Time.deltaTime * -playerRigidbody.velocity.normalized * slidingCounterMovement);
                return;
            }
            
            //TODO: Maybe changeup counter movement a bit, feels quite unsatisfying
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
        private void MouseLook(float mouseXInput, float mouseYInput)
        {
            var currentRotation = playerCam.transform.localRotation.eulerAngles;

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
            //TODO: Make Player Jump Over server and kms because this will take ages again haha kms kms 
            if (!isLocalPlayer) return;
            if (!_readyToJump || !_isGrounded) return;
            //Add Jump Force to player
            playerRigidbody.AddForce(Vector3.up * (jumpMultiplier));
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
            _x = Input.GetAxis(Misc.Other.InputAxis.Horizontal);
            _y = Input.GetAxis(Misc.Other.InputAxis.Vertical);
            
            //TODO: Make Keys Configurable
            _isSprinting = Input.GetKeyDown(KeyCode.LeftShift);
            _isJumping = Input.GetKey(KeyCode.Space);

            var timeStamp = Time.time;
            m_playerMovementManager.AddPackage(new PackagePlayerMovement
            {
                x = _x,
                y = _y,
                mouseX = Input.GetAxisRaw(Misc.Other.InputAxis.MouseX),
                mouseY = -Input.GetAxisRaw(Misc.Other.InputAxis.MouseY),
                timestamp =  timeStamp
            });
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

        private void OnCollisionStay(Collision other)
        {
            if (!isLocalPlayer) return;
            var layerOfCollided = other.gameObject.layer;
            if (groundLayer != (groundLayer | 1 << layerOfCollided)) return;
            
            foreach(var contact in other.contacts)
            {
                var normal = contact.normal;
                if (IsSlopeWalkable(normal))
                {
                    _isGrounded = true;
                    CancelInvoke(nameof(ResetGrounded));
                }
                Invoke(nameof(ResetGrounded), Time.deltaTime * coyoteTime);
            }
        }

        //Resets grounded state
        private void ResetGrounded()
        {
            if (!isLocalPlayer) return;
            _isGrounded = false;
        }

        private bool IsSlopeWalkable(Vector3 normal)
        {
            return Vector3.Angle(Vector3.up, normal) < maxSlope;
        }

        #endregion
    }
}