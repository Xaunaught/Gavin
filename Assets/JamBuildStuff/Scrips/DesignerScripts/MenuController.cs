using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MenuController : MonoBehaviour {

    public AudioMixer masterMixer;
    public GameObject mainMenu;
    // Use this for initialization
    void Start () {
        masterMixer = Resources.Load("AudioMixer") as AudioMixer;
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 0.5f));
        SetEffectsVolume(PlayerPrefs.GetFloat("EffectsVolume", 0.5f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.5f));
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }
    }


    void Update()
    {
        if (Input.GetButtonDown("Cancel") && mainMenu != null)
        {
            mainMenu.SetActive(!mainMenu.activeInHierarchy);
            if (mainMenu.activeInHierarchy)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
    }

    public void ToggleOnOff(GameObject target)
    {
        target.SetActive(!target.activeInHierarchy);
    }

    public void TurnOff(GameObject target)
    {
        target.SetActive(false);
    }

    public void SetMasterVolume(float volume)
    {
        float dB;
        if (volume != 0)
        {
            dB = 20.0f * Mathf.Log10(volume);
        }
        else
        {
            dB = -144.0f;
        }
        masterMixer.SetFloat("MasterVolume", dB);
    }

    public void SetEffectsVolume(float volume)
    {
        float dB;
        if (volume != 0)
        {
            dB = 20.0f * Mathf.Log10(volume);
        }
        else
        {
            dB = -144.0f;
        }
        masterMixer.SetFloat("EffectsVolume", dB);
    }

    public void SetMusicVolume(float volume)
    {
        float dB;
        if (volume != 0)
        {
            dB = 20.0f * Mathf.Log10(volume);
        }
        else
        {
            dB = -144.0f;
        }
        masterMixer.SetFloat("MusicVolume", dB);
    }


}
