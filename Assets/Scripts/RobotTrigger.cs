using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotTrigger : MonoBehaviour {
    public Vector3 target;
    public GameObject zone;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        
	}

    public void Tween()
    {
            LeanTween.rotate(gameObject, target, 1).setEaseInQuad();
    }
}
