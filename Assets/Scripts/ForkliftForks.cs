using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForkliftForks : MonoBehaviour {
    [SerializeField]
    private bool touchingPickup = false;
    public GameObject pickup;

    // Use this for initialization
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "RobotTrigger")
        {
            touchingPickup = true;
            pickup = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "RobotTrigger")
        {
            touchingPickup = false;
            pickup = null;
        }
    }

    public bool IsTouchingPickup()
    {
        return touchingPickup;
    }
}
