using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instantiator : MonoBehaviour {
    public GameObject targetObject;
    public float time;
	// Use this for initialization
	void Start () {
        StartCoroutine(Spawn());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    IEnumerator Spawn()
    {
        while(true)
        {
            Instantiate(targetObject, transform.position, transform.rotation);
            yield return new WaitForSeconds(time);
        }
    }
}
