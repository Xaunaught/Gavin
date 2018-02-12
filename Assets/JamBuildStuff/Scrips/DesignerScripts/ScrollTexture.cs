using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollTexture : MonoBehaviour {
    public float speed;
    private Renderer rend;
    private float offset;
	// Use this for initialization
	void Start () {
        rend = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
        offset += Time.deltaTime * speed;
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));

    }
}
