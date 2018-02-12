using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MusicController : MonoBehaviour {

    public static MusicController instance = null;

    public AudioClip[] musicClips;
    public AudioSource[] source;
    private AudioListener listener;
    private int currentClip=0;
    private int currentSource = 0;
    private bool fadingOut;
    private PlayerController player;


    private void Awake()
    {
        DontDestroyOnLoad(this);
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        player = FindObjectOfType<PlayerController>();
        listener = GetComponent<AudioListener>();
        source = GetComponents<AudioSource>();
        source[currentSource].clip = musicClips[currentClip];
        source[currentSource].Play();
        foreach (AudioListener aud in FindObjectsOfType<AudioListener>())
        {
            if (aud != listener)
            {
                aud.enabled = false;
            }
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        foreach (AudioListener aud in FindObjectsOfType<AudioListener>())
        {
            if (aud != listener && aud.gameObject != instance.gameObject)
            {
                aud.enabled = false;
            }
            else
            {
                aud.enabled = true;
            }
        }
        player = FindObjectOfType<PlayerController>();

    }


    // Update is called once per frame
    void Update () {

        if (player.posessedPawn != null)
        {
            transform.position = player.posessedPawn.transform.position;
        }
        if (!fadingOut)
        {
            if (source[currentSource].time > musicClips[currentClip].length - 5)
            {
                fadingOut = true;
                int oldSource = currentSource;
                currentSource = (1+currentSource) % source.Length;
                currentClip = (1 + currentClip) % musicClips.Length;
                source[currentSource].clip = musicClips[currentClip];
                source[currentSource].Play();
                LeanTween.value(0, 1, 5).setOnUpdate(delegate (float f) { source[currentSource].volume = f; });
                LeanTween.value(1, 0, 5).setOnUpdate(delegate (float f) { source[oldSource].volume = f; }).setOnComplete(delegate () { fadingOut = false; });
            }
        }
    }

}
