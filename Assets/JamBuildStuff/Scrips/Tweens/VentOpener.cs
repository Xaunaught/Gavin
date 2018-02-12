using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VentOpener : MonoBehaviour {
    public Vector3 target;

	void Trigger()
    {
        LeanTween.rotate(gameObject, target, 1).setEaseInQuad();
    }
}
