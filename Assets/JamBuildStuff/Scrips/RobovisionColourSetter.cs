using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobovisionColourSetter : MonoBehaviour {

    public Color roboColour = new Color(0,0,0,0.3f);
    Renderer r;
    void Start()
    {
        r = GetComponent<Renderer>();
    }

	// Update is called once per frame
	void Update () {
        r.material.SetColor("_RoboVisionColour", roboColour);
    }
}
