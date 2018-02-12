using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpener : MonoBehaviour {
    public Vector3 moveTo;
	void Trigger()
    {
        LeanTween.move(gameObject, moveTo, 1).setEaseInQuad();
    }
}
