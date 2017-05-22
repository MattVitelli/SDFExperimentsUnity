using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFTest : MonoBehaviour {
    public Texture2D heightmap;
    public SDFImage sdfImage;
    public Material sdfMaterial;
    public Light sun;
    public Transform worldRoot;

	// Use this for initialization
	void Start () {
        BuildSDFMap();
    }

    Image25D generateHeightMap()
    {
        Vector3 minPt = Vector3.zero;
        Vector3 maxPt = Vector3.zero;
        bool isAssigned = false;
        Renderer[] rs = worldRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rs)
        {
            if(!isAssigned)
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
        cam.orthographicSize = Mathf.Max(delta.x, delta.z)*0.5f;
        float eps = 1.0e-3f;
        Vector3 center = (minPt + maxPt) * 0.5f;
        Vector3 camPos = center;
        Vector3 extent = maxPt - minPt;
        camPos.y = maxPt.y + eps*5;
        cam.transform.position = camPos;
        cam.transform.LookAt(center, Vector3.forward);
        cam.nearClipPlane = eps;
        cam.farClipPlane = extent.y * 2;
        cam.targetTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGBFloat);
        cam.backgroundColor = new Color(0, 0, 0, 0);
        Shader shader = Shader.Find("Custom/GenerateHeightmap");
        Shader.SetGlobalVector(Shader.PropertyToID("_MinBounds"), minPt);
        Shader.SetGlobalVector(Shader.PropertyToID("_MaxBounds"), maxPt);
        cam.RenderWithShader(shader, string.Empty);

        RenderTexture src = cam.targetTexture;
        Texture2D newTex = new Texture2D(256, 256, TextureFormat.RGBAFloat, false);
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = src;
        newTex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        RenderTexture.active = old;

        Image25D img = new Image25D();
        img.Image = newTex;
        img.MinPoint = minPt;
        img.MaxPoint = maxPt;
        DestroyImmediate(cam.gameObject);
        return img;
    }

    void reassignMaterials()
    {
        Renderer[] rs = worldRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rs)
        {
            r.material = sdfMaterial;
        }
    }

    public void BuildSDFMap()
    {
        reassignMaterials();
        Image25D hm2D = generateHeightMap();
        Texture2D hm = hm2D.Image;
        float[] heights = MakeSDF.ExtractHeightmap(hm);
        sdfImage = MakeSDF.BuildSDF3D(heights, heightmap.width, 128, heightmap.height);

        sdfMaterial.SetVector(Shader.PropertyToID("_minPointCube"), hm2D.MinPoint);
        sdfMaterial.SetVector(Shader.PropertyToID("_maxPointCube"), hm2D.MaxPoint);
        sdfMaterial.SetTexture(Shader.PropertyToID("_sdfMap"), sdfImage.Image);
        sdfMaterial.SetTexture(Shader.PropertyToID("_heightMap"), hm);
        sdfMaterial.SetVector(Shader.PropertyToID("_encodeRange"), new Vector4(sdfImage.EncodingRange.x, sdfImage.EncodingRange.y - sdfImage.EncodingRange.x, 0, 0));
        sdfMaterial.SetVector(Shader.PropertyToID("_invDims"), new Vector4(1.0f / sdfImage.Image.width, 1.0f / sdfImage.Image.height, 1.0f / sdfImage.Image.depth, 0));
    }

    // Update is called once per frame
    void Update () {
        sdfMaterial.SetVector(Shader.PropertyToID("_lightDir"), new Vector4(sun.transform.forward.x, sun.transform.forward.y, sun.transform.forward.z));
	}
}
