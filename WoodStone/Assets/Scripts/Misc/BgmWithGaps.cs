using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BgmWithGaps : MonoBehaviour
{
    public AudioSource src = null;

    public float initialDelay = 15f;
    public float minGap = 60f;
    public float maxGap = 120f;

    void Start()
    {
        this.StartCoroutine(this.handleBGM());
    }

    private IEnumerator handleBGM()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            this.src.Play();

            yield return new WaitForSeconds(Random.Range(this.minGap, this.maxGap) + this.src.clip.length / Mathf.Clamp01(this.src.pitch));
        }
    }
}
