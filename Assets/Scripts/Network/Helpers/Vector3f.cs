using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network.Helpers
{
    [System.Serializable]
    public class Vector3f
    {
        public float x, y, z;

        public Vector3f(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //Returns length of a vector
        public float GetVectorMagnitude()
        {
            return Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2));
        }

        //returns a normalized vector
        public Vector3f Normalize()
        {
            var normalizedX = x / GetVectorMagnitude();
            var normalizedY = y / GetVectorMagnitude();
            var normalizedZ = z / GetVectorMagnitude();
            return new Vector3f(normalizedX, normalizedY, normalizedZ);
        }

        public string String()
        {
            return $"({x}, {y}, {z})";
        }

        public Vector3 ToVector()
        {
            return new Vector3(x, y, z);
        }
        
        /*
         *
         * Overloaded Operators
         */
        public static Vector3f operator +(Vector3f one, Vector3f other)
        {
            return new Vector3f(one.x + other.x, one.y + other.y, one.z + other.z);
        }
        
        public static Vector3f operator -(Vector3f one, Vector3f other)
        {
            return new Vector3f(one.x - other.x, one.y - other.y, one.z - other.z);
        }
        
        public static Vector3f operator *(Vector3f one, Vector3f other)
        {
            return new Vector3f(one.x * other.x, one.y * other.y, one.z * other.z);
        }
        
        public static Vector3f operator /(Vector3f one, Vector3f other)
        {
            return new Vector3f(one.x / other.x, one.y / other.y, one.z / other.z);
        }
        
        //Statics
        public static Vector3f FromVector(Vector3 vector)
        {
            return new Vector3f(vector.x, vector.y, vector.z);
        }
    }

}
