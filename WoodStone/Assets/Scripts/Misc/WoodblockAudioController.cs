using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodblockAudioController : MonoBehaviour
{

    public AudioSource src = null;

    public AudioClip tick = null;
    public AudioClip tock = null;

    private bool toggle = false;

    public void playSFX ()
    {
            this.src.clip = ((this.toggle) ? (this.tick) : (this.tock));
            this.src.Play();

        this.toggle = !this.toggle;
    }

    public void reset()
    {
        this.toggle = false;
    }
}
