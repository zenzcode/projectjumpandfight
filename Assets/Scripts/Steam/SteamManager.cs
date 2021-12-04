using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace Steam
{
    public class SteamManager : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            try
            {
                SteamClient.Init(408);
            }
            catch (System.Exception e)
            {
                print($"Something went wrong... {e.Message}");
            }
        }
    }
}