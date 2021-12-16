using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Player.Recognition
{
 public class PlayerColor : NetworkBehaviour
 {
     [SyncVar(hook = nameof(PlayerColorChanged))] private Color _playerColor;

     public override void OnStartServer()
     {
         SetRandomPlayerColor();
     }

     private void SetRandomPlayerColor()
     {
         _playerColor = Random.ColorHSV();
     }
 
     private void PlayerColorChanged(Color oldColor, Color newColor)
     {
         var meshRenderer = GetComponentInChildren<MeshRenderer>();
         var materials = new List<Material>();
         meshRenderer.GetMaterials(materials);
         materials[0].color = Color.HSVToRGB(newColor.r, newColor.g, newColor.b);
         meshRenderer.materials = materials.ToArray();
 
     }
 }   
}
