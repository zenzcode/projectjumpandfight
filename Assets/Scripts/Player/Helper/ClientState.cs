using System.Collections;
using System.Collections.Generic;
using Network.Helpers;
using UnityEngine;

[SerializeField]
public class ClientState
{
    public float x = 0;
    public float y = 0;
    public float orientationY = 0;
    public bool grounded = false;
    public bool jumping = false;
    public bool crouching = false;
    public Vector3f rigidPos = Vector3f.FromVector(Vector3.zero);
    public Vector3f rigidVel = Vector3f.FromVector(Vector3.zero);
    public Vector3f rigidAngularVel = Vector3f.FromVector(Vector3.zero);
    public Vector3f rigidRot = Vector3f.FromVector(Vector3.zero);
}