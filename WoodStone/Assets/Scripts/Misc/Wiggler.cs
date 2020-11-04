using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wiggler : MonoBehaviour
{
    public Vector3 positionA = Vector3.zero;
    public Vector3 positionB = Vector3.zero;

    public float wiggleSpeed = 0.2f;

    private bool aToB = true;
    private float progress = 0f;

    private Vector3 initialPosition = Vector3.zero;

    private void Start()
    {
        this.initialPosition = this.transform.position;
    }

    void Update()
    {
        // Update Progress
        if (this.aToB)
            this.progress = Mathf.Clamp01(this.progress + (Time.deltaTime / this.wiggleSpeed));
        else
            this.progress = Mathf.Clamp01(this.progress - (Time.deltaTime / this.wiggleSpeed));

        // Swap direction
        if (this.progress == 0f || this.progress == 1f)
            this.aToB = !this.aToB;

        // SmoothStep interp Position
        this.transform.position = this.initialPosition + Vector3.Lerp(this.positionA, this.positionB, Mathf.SmoothStep(0f, 1f, this.progress));
    }
}
