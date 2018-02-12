using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetScene : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Robot" && FindObjectOfType<PlayerController>().posessedPawn.gameObject == other.gameObject)
        {
            Reload();
        }
    }

    public void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
