using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetSliderValue : MonoBehaviour {

    [Tooltip("The name of the value in the Player Prefs file")]
    public string valueName;

    void Start()
    {
        GetComponent<Slider>().value = PlayerPrefs.GetFloat(valueName, 1);
        GetComponent<Slider>().onValueChanged.AddListener(SetPlayerPrefs);
    }

    void SetPlayerPrefs(float val)
    {
        PlayerPrefs.SetFloat(valueName, val);
    }
}
