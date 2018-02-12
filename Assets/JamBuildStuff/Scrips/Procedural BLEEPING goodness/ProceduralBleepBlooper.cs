using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralBleepBlooper : MonoBehaviour {

    AudioSource AS;
    AudioClip AC;
    Pawn p;

    public int SampleRate = 44100;
    public float maxFrequency;
    public float minFrequency;

    float freq, freq2;
    int position = 0;
    public float TimeSeconds = 1;
    public float Delay = 3;

    bool canAudio = false;
	
    void Start()
    {
        canAudio = false;
        AS = GetComponent<AudioSource>();
        p = GetComponent<Pawn>();
        StartCoroutine(resetCanAudio());
    }

    void OnCollisionEnter(Collision c)
    {
        if (canAudio && (p.controller))
        {
            if (p.controller.GetType() != typeof(PlayerController))
                return;
            freq = Random.Range(minFrequency, maxFrequency);
            freq2 = freq * (5.0f / 6.0f);
            position = 0;
            AC = AudioClip.Create("ANGERY", (int)(SampleRate * TimeSeconds), 1, SampleRate, false, BloopCallback);
            canAudio = false;
            AS.PlayOneShot(AC);
            canAudio = false;
            StartCoroutine(resetCanAudio());
        }
    }

    void BloopCallback(float[] data)
    {
        int count = 0;
        
        while(count < data.Length)
        {
            float f = 0;
            if (position > SampleRate * TimeSeconds / 2)
                f = freq2;
            else
                f = freq;
            data[count] = Square(f * position * 2 * Mathf.PI / SampleRate);
            position++;
            count++;
        }
    }

    float Square(float x)
    {
        return Mathf.Sin(x) > 0 ? 1 : -1;
    }

    IEnumerator resetCanAudio()
    {
        yield return new WaitForSeconds(Delay);
        canAudio = true;
    }

}
