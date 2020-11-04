using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pixellation : MonoBehaviour
{
    public int pixellationFactor = 2;

    public RawImage destination = null;

    private int scrnWdth = 0;
    private int scrnHght = 0;

    private RenderTexture camTargetTex = null;

    void Start()
    {
        this.scrnWdth = Screen.width;
        this.scrnHght = Screen.height;

        this.RefreshPixellation();
    }

    void Update()
    {
        if (this.scrnWdth != Screen.width || this.scrnHght != Screen.height)
        {
            this.scrnWdth = Screen.width;
            this.scrnHght = Screen.height;

            this.RefreshPixellation();
        }
    }

    private void RefreshPixellation()
    {
        // Create target texture
        this.camTargetTex = new RenderTexture(Screen.width / this.pixellationFactor, Screen.height / this.pixellationFactor, 16);
        this.camTargetTex.filterMode = FilterMode.Point;

        // Set new texture
        Camera.main.targetTexture = this.camTargetTex;
        this.destination.texture = this.camTargetTex;
    }
}
