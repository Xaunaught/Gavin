using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {
    public string SceneName;
    // Use this for initialization
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        if(other.tag == "Robot" && FindObjectOfType<PlayerController>().posessedPawn.gameObject==other.gameObject)
        {
            Debug.Log("YOU DID IT");
            SceneManager.LoadScene(SceneName);
        }
    }
}
