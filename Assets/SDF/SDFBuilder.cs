using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFBuilder : MonoBehaviour {
    public SDFImage sdfImage;
    public Material sdfMaterial;
    public Light sun;
    public Transform worldRoot;
    public int width = 256;
    public int height = 256;
    public int depth = 256;
    public ComputeShader computeShader;

    void reassignMaterials()
    {
        Renderer[] rs = worldRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rs)
        {
            r.material = sdfMaterial;
        }
    }

    public void BuildSDF()
    {
        sdfImage = MakeSDF.BuildSDF3DFromMeshes(worldRoot.gameObject, 
            width, height, depth, computeShader);
        sdfMaterial.SetVector(Shader.PropertyToID("_minPointCube"), sdfImage.Bounds.min);
        sdfMaterial.SetVector(Shader.PropertyToID("_maxPointCube"), sdfImage.Bounds.max);
        sdfMaterial.SetTexture(Shader.PropertyToID("_sdfMap"), sdfImage.Image);
        sdfMaterial.SetVector(Shader.PropertyToID("_invDims"), new Vector4(1.0f / sdfImage.Image.width, 1.0f / sdfImage.Image.height, 1.0f / sdfImage.Image.depth, 0));
        reassignMaterials();
    }

    // Use this for initialization
    void Start () {
		
	}

    void drawDebugCube(Bounds bounds)
    {
        Vector3 extent = bounds.max-bounds.min;
        Vector3 v0 = bounds.min;
        Vector3 v1 = bounds.min + Vector3.right * extent.x;
        Vector3 v2 = bounds.min + Vector3.forward * extent.z;
        Vector3 v3 = bounds.min + Vector3.right * extent.x + Vector3.forward * extent.z;
        Vector3 v4 = v0 + Vector3.up * extent.y;
        Vector3 v5 = v1 + Vector3.up * extent.y;
        Vector3 v6 = v2 + Vector3.up * extent.y;
        Vector3 v7 = v3 + Vector3.up * extent.y;

        Color color = Color.red;
        Debug.DrawLine(v0, v1, color);
        Debug.DrawLine(v0, v2, color);
        Debug.DrawLine(v0, v4, color);
        Debug.DrawLine(v4, v5, color);
        Debug.DrawLine(v4, v6, color);
        Debug.DrawLine(v3, v1, color);
        Debug.DrawLine(v3, v2, color);
        Debug.DrawLine(v3, v7, color);
        Debug.DrawLine(v7, v5, color);
        Debug.DrawLine(v7, v6, color);
        Debug.DrawLine(v1, v5, color);
        Debug.DrawLine(v2, v6, color);
    }
	
	// Update is called once per frame
	void Update () {
        sdfMaterial.SetVector(Shader.PropertyToID("_lightDir"), new Vector4(sun.transform.forward.x, sun.transform.forward.y, sun.transform.forward.z));
        drawDebugCube(sdfImage.Bounds);
    }
}
