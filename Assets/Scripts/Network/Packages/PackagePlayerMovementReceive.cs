using System.Collections;
using System.Collections.Generic;
using Network.Helpers;
using UnityEngine;

[System.Serializable]
public class PackagePlayerMovementReceive
{
    public Vector3f position;
    public PackagePlayerMovement lastMove;
    public Vector3f rigidVelocity;
    public Vector3f angularRigidVelocity;
    public float timestamp;
}
