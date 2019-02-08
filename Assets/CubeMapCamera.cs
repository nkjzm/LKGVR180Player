using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeMapCamera : MonoBehaviour
{

    public Camera camera;
    public Cubemap cubemap;
    public Image image;
    public Material convMaterial;

    public int outputWidth = 4096;
    public int outputHeight = 2048;
    public int cubeWidth = 1280;

    private RenderTexture renderTexture;
    private Texture2D equirectangularTexture;

    // Use this for initialization
    void Start()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        if (cubemap == null)
        {
            cubemap = new Cubemap(cubeWidth, TextureFormat.RGBA32, false);
        }

        if (convMaterial == null)
        {
            Shader conversionShader = Shader.Find("Conversion/CubemapToEquirectangular");
            convMaterial = new Material(conversionShader);
        }
        renderTexture = new RenderTexture(outputWidth, outputHeight, 24);
        equirectangularTexture = new Texture2D(outputWidth, outputHeight, TextureFormat.ARGB32, false);
        image.GetComponent<Image>().material.mainTexture = equirectangularTexture;
    }

    // Update is called once per frame
    void Update()
    {
        camera.RenderToCubemap(cubemap);

        Graphics.Blit(cubemap, renderTexture, convMaterial);
        equirectangularTexture.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0, false);
        equirectangularTexture.Apply();
    }
}