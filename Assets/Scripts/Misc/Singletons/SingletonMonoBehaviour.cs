using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Misc.Singletons
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        
        public static T Instance { get; }
    
        protected virtual void Awake()
        {
            CreateInstance();
        }
    
        private void CreateInstance()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
            }
    
            _instance = this as T;
        }
    
    
    
    }
}

