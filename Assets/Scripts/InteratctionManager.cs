using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteratctionManager : MonoBehaviour {
    public GameObject activeRobot;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0))
        {
            Ray toMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rhInfo;
            bool didHit = Physics.Raycast(toMouse, out rhInfo, 500f);

            if(didHit && rhInfo.collider.tag == "Interactable")
            {
                rhInfo.collider.gameObject.SendMessage("Trigger");
            }
            if(didHit && rhInfo.collider.tag == "Robot")
            {
                activeRobot = rhInfo.collider.gameObject;
            }
        }
	}
}
