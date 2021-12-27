using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using DG.Tweening;
using kcp2k;
using Mirror;
using Misc;
using Network;
using Network.Helpers;
using Player.Network;
using TMPro;
using Unity.VisualScripting;
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

        [Header("Network Settings")] [Range(0.0f, 1f)]public float networkSendRate = 0.5f;
        [SerializeField] public bool isPredictionEnabled;
        [SerializeField] public float correctionTreshold;

        [Header("Debug Info")] [SerializeField]
        public TMP_Text serverPos;

        [SerializeField] public TMP_Text clientPos;
        [SerializeField] public TMP_Text diffPos;
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

        //Simulating our own FixedUpdate
        private float _tickTime = 0;
        
        //Saves move to List to maybe resimulate if determinism fails
        private ClientState[] _clientBuffer;
        
        //Ticknr to send with package
        private int _tickNr = 0;
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
            _clientBuffer = new ClientState[2048];
            for (var i = 0; i < _clientBuffer.Length; ++i)
            {
                _clientBuffer[i] = new ClientState();
            }
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
            serverPos = GameObject.Find("Text_ServerPos").GetComponent<TMP_Text>();
            clientPos = GameObject.Find("Text_ClientPos").GetComponent<TMP_Text>();
            diffPos = GameObject.Find("Text_Diff").GetComponent<TMP_Text>();
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
            _tickTime += Time.deltaTime;
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

            orientation.transform.localRotation = Quaternion.Euler(0, nextPackage.orientationY, 0);
            PhysicsMovement(nextPackage.deltaTime, nextPackage.grounded, nextPackage.jumping, nextPackage.crouching, nextPackage.x, nextPackage.y);
            if (transform.position == _lastPosition) return;
            _lastPosition = transform.position;
            m_receiveMovementManager.AddPackage(new PackagePlayerMovementReceive
            {
                position = new Vector3f(playerRigidbody.position.x, playerRigidbody.position.y, playerRigidbody.position.z),
                lastMove = nextPackage,
                tickNr = nextPackage.tickNr + 1,
                rigidVelocity =  new Vector3f(playerRigidbody.velocity.x, playerRigidbody.velocity.y, playerRigidbody.velocity.z),
                angularRigidVelocity = new Vector3f(playerRigidbody.angularVelocity.x, playerRigidbody.angularVelocity.y, playerRigidbody.angularVelocity.z)
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
            var bufferSlot = _tickNr % _clientBuffer.Length;
            _clientBuffer[bufferSlot].x = _x;
            _clientBuffer[bufferSlot].y = _y;
            _clientBuffer[bufferSlot].grounded = _isGrounded;
            _clientBuffer[bufferSlot].jumping = _isJumping;
            _clientBuffer[bufferSlot].crouching = _isCrouching;
            _clientBuffer[bufferSlot].orientationY = orientation.eulerAngles.y;
            _clientBuffer[bufferSlot].rigidPos = Vector3f.FromVector(playerRigidbody.position);
            _clientBuffer[bufferSlot].rigidVel = Vector3f.FromVector(playerRigidbody.velocity);
            _clientBuffer[bufferSlot].rigidAngularVel = Vector3f.FromVector(playerRigidbody.angularVelocity);
            PhysicsMovement(Time.deltaTime, _isGrounded, _isJumping, _isCrouching, _x, _y);
        }

        private void RemoteClientUpdate()
        {
            if (isServer) return;

            var data = m_receiveMovementManager.GetNextDataReceive();
            if (data == null) return;

            
            if (isLocalPlayer && isPredictionEnabled)
            {            
                var bufferSlot = data.tickNr % _clientBuffer.Length;
                var positionOffset = data.position - _clientBuffer[bufferSlot].rigidPos;
                if (positionOffset.ToVector().sqrMagnitude > 0.0000001f)
                {
                    playerRigidbody.position = data.position.ToVector();
                    playerRigidbody.velocity = data.rigidVelocity.ToVector();
                    playerRigidbody.angularVelocity = data.angularRigidVelocity.ToVector();

                    var rewindTickNr = data.tickNr;
                    while (rewindTickNr < _tickNr)
                    {
                        var rewindBufferSlot = rewindTickNr % _clientBuffer.Length;
                        _clientBuffer[rewindBufferSlot].rigidPos = Vector3f.FromVector(playerRigidbody.position);
                        _clientBuffer[rewindBufferSlot].rigidVel = Vector3f.FromVector(playerRigidbody.velocity);
                        _clientBuffer[rewindBufferSlot].rigidAngularVel = Vector3f.FromVector(playerRigidbody.angularVelocity);
                        _clientBuffer[rewindBufferSlot].x = _x;
                        _clientBuffer[rewindBufferSlot].y = _y;
                        _clientBuffer[rewindBufferSlot].grounded = _isGrounded;
                        _clientBuffer[rewindBufferSlot].jumping = _isJumping;
                        _clientBuffer[rewindBufferSlot].crouching = _isCrouching;
                        _clientBuffer[rewindBufferSlot].orientationY = orientation.eulerAngles.y;
                        PhysicsMovement(Time.deltaTime, _isGrounded, _isJumping, _isCrouching, _x, _y);
                        ++rewindTickNr;
                    }
                }
            }
            else
            {
                playerRigidbody.position = data.position.ToVector();
            }
        }

        //handles actual movement
        private void PhysicsMovement(float delta, bool isGrounded, bool isJumping, bool isCrouching, float x, float y)
        {
            while (_tickTime >= Time.fixedDeltaTime)
            {  
                _tickTime -= Time.fixedDeltaTime; 
                //add extra gravity downwards
                playerRigidbody.AddForce(Vector3.down * (delta * extraGravityMultiplicator));

                //if grounded and jump key pressed, perform jump
                if (isGrounded && isJumping)
                {
                    PerformJump();
                }
            
                //calculate velocity relative to look 
                var mag = FindVelRelativeToLook();
            
                //perform counter movement so we dont go yeet into space
                CounterMovement(delta, isGrounded, isJumping, isCrouching, x, y, mag);

                //check if we go brrrr, then reset input
                if (x > 0 && mag.x > playerMaxSpeed) x = 0;
                if (x < 0 && mag.x < -playerMaxSpeed) x = 0;
                if (y > 0 && mag.y > playerMaxSpeed) y = 0;
                if (y < 0 && mag.y < -playerMaxSpeed) y = 0;

                var multiplierX = 1f;
                var multiplierY = 1f;

                if (!isGrounded)
                {
                    multiplierX = 0.5f;
                    multiplierY = 0.5f;
                }
            
                //when sliding
                if (isGrounded && isCrouching)
                {
                    multiplierY = 0f;
                }
                //add actual movement force
                playerRigidbody.AddForce(orientation.transform.right * (x  * delta * (walkMultiplier * multiplierX * multiplierY)));
                playerRigidbody.AddForce(orientation.transform.forward * (y * delta * (walkMultiplier * multiplierY)));
                Physics.Simulate(Time.fixedDeltaTime);
                ++_tickNr;
            }
            
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
        private void CounterMovement(float deltaTime, bool isGrounded, bool isJumping, bool isCrouching, float x, float y, Vector2 relativeVelocity)
        {
            //no counter movement when in air
            if (!isGrounded || isJumping) return;
            
            //sliding should not be that heavy slowed down as other
            if (isCrouching)    
            {
                playerRigidbody.AddForce(walkMultiplier * deltaTime * -playerRigidbody.velocity.normalized * slidingCounterMovement);
                return;
            }
            
            //handling letting go of key,                                       switch directions
            if (Math.Abs(relativeVelocity.x) > 0.01f && Math.Abs(x) < 0.05f || relativeVelocity.x < -0.01f && x > 0 || relativeVelocity.x > 0.01f && x < 0)
            {
                playerRigidbody.AddForce(orientation.transform.right * (walkMultiplier * (deltaTime * -relativeVelocity.x * counterModifier)));
            }
            
            //handling letting go of key,                                       switch directions
            if (Math.Abs(relativeVelocity.y) > 0.01f && Math.Abs(y) < 0.05f || relativeVelocity.y < -0.01f && y > 0 || relativeVelocity.y > 0.01f && y < 0)
            {
                playerRigidbody.AddForce(orientation.transform.forward * (walkMultiplier * (deltaTime * -relativeVelocity.y * counterModifier)));
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
            var timeStamp = Time.time;
            m_playerMovementManager.AddPackage(new PackagePlayerMovement
            {
                x = _x,
                y = _y,
                grounded = _isGrounded,
                jumping = _isJumping,
                crouching = _isCrouching,
                orientationY = orientation.localRotation.eulerAngles.y,
                deltaTime = Time.deltaTime,
                tickNr = _tickNr
            });
        }
        
        //Checks and Performs jump
        private void PerformJump()
        {
            //TODO: Make Player Jump Over server and kms because this will take ages again haha kms kms 
            if (!_readyToJump) return;
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
            var lookAngle = orientation.transform.localRotation.eulerAngles.y;
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