using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ColourSwitcher : MonoBehaviour {
    [Header("Mat Management")]
    public GameObject baseColourObject;
    [SerializeField]
    private Material baseColour;
    public Material selected;

    public List<GameObject> MaterialList;

    // Use this for initialization
    void Start () {
        baseColour = baseColourObject.GetComponent<Renderer>().material;
    }
	
	public void ActiveOn()
    {
        foreach(GameObject item in MaterialList)
        {
            item.GetComponent<Renderer>().material = selected;
        }
    }

    public void ActiveOff()
    {
        foreach (GameObject item in MaterialList)
        {
            item.GetComponent<Renderer>().material = baseColour;
        }
    }
}
