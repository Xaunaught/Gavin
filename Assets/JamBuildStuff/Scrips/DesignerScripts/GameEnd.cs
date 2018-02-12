using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEnd : MonoBehaviour {

    public GameObject youwin;
    public string mainMenu;
	
	
	public void Trigger()
    {
        youwin.SetActive(true);
        StartCoroutine(EndGame());
    }

    public IEnumerator EndGame()
    {
        yield return new WaitForSeconds(5);

        SceneManager.LoadScene(mainMenu);
    }
}
