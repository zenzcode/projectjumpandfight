using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PackagePlayerMovement
{
    public float x;
    public float y;
    public float orientationY;
    public bool grounded;
    public bool jumping;
    public bool crouching;
    public float deltaTime;
    public float timestamp;
}
