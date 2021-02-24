using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warp : Interactable
{
    public Transform warpPoint;

    public override void Interact()
    {
        PlayerController.player.transform.position = warpPoint.position;
    }
}
