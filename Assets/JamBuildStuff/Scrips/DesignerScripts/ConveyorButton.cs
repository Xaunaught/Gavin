using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ConveyorButton : MonoBehaviour {
    public ConveyorBelt pairedBelt;



    public void Activate()
    {
        pairedBelt.speed *= -1f;
    }

}
