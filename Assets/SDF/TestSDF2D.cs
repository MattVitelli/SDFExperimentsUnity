using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSDF2D : MonoBehaviour {
    public int width = 512;
    public int height = 512;
    public int depth = 512;
    public ComputeShader computeShader;
    public Material material;
    public Renderer quad;
    public GameObject worldRoot;
    // Use this for initialization

    Image25D generateHeightMap()
    {
        Vector3 minPt = Vector3.zero;
        Vector3 maxPt = Vector3.zero;
        bool isAssigned = false;
        Renderer[] rs = worldRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rs)
        {
            if (!isAssigned)
            {
                isAssigned = true;
                minPt = r.bounds.min;
                maxPt = r.bounds.max;
            }
            else
            {
                minPt = Vector3.Min(minPt, r.bounds.min);
                maxPt = Vector3.Max(maxPt, r.bounds.max);
            }
        }
        GameObject tmpCam = new GameObject("Camera");
        Camera cam = tmpCam.AddComponent<Camera>();
        cam.orthographic = true;
        Vector3 delta = maxPt - minPt;
        cam.orthographicSize = Mathf.Max(delta.x, delta.z) * 0.5f;
        float eps = 1.0e-3f;
        Vector3 center = (minPt + maxPt) * 0.5f;
        Vector3 camPos = center;
        Vector3 extent = maxPt - minPt;
        camPos.y = maxPt.y + eps * 5;
        cam.transform.position = camPos;
        cam.transform.LookAt(center, Vector3.forward);
        cam.nearClipPlane = eps;
        cam.farClipPlane = extent.y * 2;
        cam.targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat);
        cam.backgroundColor = new Color(0, 0, 0, 0);
        Shader shader = Shader.Find("Custom/GenerateHeightmap");
        Shader.SetGlobalVector(Shader.PropertyToID("_MinBounds"), minPt);
        Shader.SetGlobalVector(Shader.PropertyToID("_MaxBounds"), maxPt);
        cam.RenderWithShader(shader, string.Empty);

        RenderTexture src = cam.targetTexture;
        Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = src;
        newTex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        newTex.Apply();
        RenderTexture.active = old;

        Image25D img = new Image25D();
        img.Image = newTex;
        img.MinPoint = minPt;
        img.MaxPoint = maxPt;
        DestroyImmediate(cam.gameObject);
        return img;
    }
    void Start () {
        //*
        Image25D heightmap = generateHeightMap();
        SDFImage tex = MakeSDF.BuildSDF3DCompute(heightmap.Image, depth, computeShader);
        material.SetTexture(Shader.PropertyToID("_cmpTex"), tex.Image);
        material.SetTexture(Shader.PropertyToID("_hmTex"), heightmap.Image);
        /*
        material.SetInt(Shader.PropertyToID("_width"), heightmap.width);
        material.SetInt(Shader.PropertyToID("_depth"), heightmap.height);
        material.SetInt(Shader.PropertyToID("_height"), depth);
        material.SetInt(Shader.PropertyToID("_texWidth"), heightmap.width);
        material.SetInt(Shader.PropertyToID("_texHeight"), heightmap.height);
        //*/
        quad.material = material;
        //*/
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
