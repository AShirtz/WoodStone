using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioIntroLooper : MonoBehaviour
{
    public AudioSource intro = null;
    public AudioSource loop = null;

    void Start()
    {
        intro.loop = false;

        if (!intro.isPlaying)
            intro.Play();

        loop.volume = intro.volume;

        loop.loop = true;
        loop.PlayScheduled(intro.clip.length);

    }
}
