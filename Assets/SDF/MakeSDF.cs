using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MakeSDF
{
    public static float[] ExtractHeightmap(Texture2D tex)
    {
        float[] vals = new float[tex.width * tex.height];
        Color[] pixels = tex.GetPixels();
        float maxVal = 0;
        for(int it = 0; it < pixels.Length; it++)
        {
            vals[it] = pixels[it].r;
            maxVal = Mathf.Max(vals[it], maxVal);
        }
        Debug.Log("Maximum value " + maxVal);
        return vals;
    }

    // Do a single pass over the data.
    // Start at (x,y,z) and walk in the direction (cx,cy,cz)
    // Combine each pixel (x,y,z) with the value at (x+dx,y+dy,z+dz)
    static void combine(ref Vector3[] sdf,
                 int dx, int dy, int dz,
                 int cx, int cy, int cz,
                 int x, int y, int z,
                 int width, int height, int depth)
    {
        int zStride = width * height;
        int yStride = width;
        int curStride = x + y * yStride + z * zStride;
        int nextStride = (x + dx) + (y + dy) * yStride + (z + dz) * zStride;
        int strideOffset = cx + cy * yStride + cz * zStride;
        Vector3 d = new Vector3(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));

        while ((x>=0 && x < width && y >=0 && y < height && z >= 0 && z < depth) &&
            (x+dx >= 0 && x+dx < width && y+dy >= 0 && y+dy < height && z+dz >= 0 && z+dz < depth))
        {
            Vector3 v1 = sdf[curStride];
            Vector3 v2 = sdf[nextStride] + d;

            if (v1.sqrMagnitude >
                v2.sqrMagnitude)
            {
                sdf[curStride] = v2;
            }

            curStride += strideOffset;
            nextStride += strideOffset;
            x += cx; y += cy; z += cz;
        }
    }
    
    //Builds a SDF3D and interprets hVals as a heightmap lying in the y axis
    static float[] BuildSDF3DFromHeightmap(float[] hVals, int width, int height, int depth)
    {
        Vector3[] sdf = new Vector3[width * height * depth];
        int stride = 0;
        //init the sdf
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                int imgStride = z * width;
                for (int x = 0; x < width; x++, imgStride++, stride++)
                {
                    float hValC = hVals[imgStride] * (height-1);
                    if(y < hValC || y < 1)
                    {
                        sdf[stride] = Vector3.zero;
                    }
                    else
                    {
                        //sdf[stride] = Vector3.one; //new Vector3(999, 999, 999);
                        sdf[stride] = Vector3.one * float.PositiveInfinity;
                    }
                }
            }
        }

        // Construct a 3d texture img and fill it with the magnitudes of the 
        // displacements in dmap, scaled appropriately
        /*
        float[] sdfFinal = new float[width * height * depth];
        stride = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, stride++)
                {
                    sdfFinal[stride] = sdf[stride].magnitude;
                }
            }
        }

        return sdfFinal;
        //*/
        //*
        //perform first pass
        for (int z = 1; z < depth; z++)
        {
            // combine with everything with dz = -1
            for (int y = 0; y < height; y++)
            {
                combine(ref sdf, 
                        0, 0, -1,
                        1, 0, 0,
                        0, y, z, 
                        width, height, depth);
            }

            for (int y = 1; y < height; y++)
            {
                combine(ref sdf,
                        0, -1, 0,
                        1, 0, 0,
                        0, y, z,
                        width, height, depth);
                combine(ref sdf, 
                        -1, 0, 0,
                        1, 0, 0,
                        1, y, z,
                        width, height, depth);
                combine(ref sdf,
                        +1, 0, 0,
                        -1, 0, 0,
                        width - 2, y, z,
                        width, height, depth);
            }

            for (int y = height - 2; y >= 0; y--)
            {
                combine(ref sdf,
                        0, +1, 0,
                        1, 0, 0,
                        0, y, z,
                        width, height, depth);
                combine(ref sdf,
                        -1, 0, 0,
                        1, 0, 0,
                        1, y, z,
                        width, height, depth);
                combine(ref sdf,
                        +1, 0, 0,
                        -1, 0, 0,
                        width - 2, y, z,
                        width, height, depth);
            }
        }

        //perform second pass
        for (int z = depth - 2; z >= 0; z--)
        {
            // combine with everything with dz = +1
            for (int y = 0; y < height; y++)
            {
                combine(ref sdf,
                        0, 0, +1,
                        1, 0, 0,
                        0, y, z,
                        width, height, depth);
            }

            for (int y = 1; y < height; y++)
            {
                combine(ref sdf,
                        0, -1, 0,
                        1, 0, 0,
                        0, y, z,
                        width, height, depth);
                combine(ref sdf,
                        -1, 0, 0,
                        1, 0, 0,
                        1, y, z,
                        width, height, depth);
                combine(ref sdf,
                        +1, 0, 0,
                        -1, 0, 0,
                        width - 2, y, z,
                        width, height, depth);
            }
            for (int y = height - 2; y >= 0; y--)
            {
                combine(ref sdf,
                        0, +1, 0,
                        1, 0, 0,
                        0, y, z,
                        width, height, depth);
                combine(ref sdf,
                        -1, 0, 0,
                        1, 0, 0,
                        1, y, z,
                        width, height, depth);
                combine(ref sdf,
                        +1, 0, 0,
                        -1, 0, 0,
                        width - 2, y, z,
                        width, height, depth);
            }
        }
        
        // Construct a 3d texture img and fill it with the magnitudes of the 
        // displacements in dmap, scaled appropriately
        float[] sdfFinal = new float[width * height * depth];
        stride = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, stride++)
                {
                    sdfFinal[stride] = sdf[stride].magnitude;
                }
            }
        }

        return sdfFinal;
        //*/
    }

    public static Color[] EncodeFloat(float[] values, out Vector2 range)
    {
        Vector2 minmax = new Vector2(values[0], values[0]);
        Color[] c = new Color[values.Length];
        for(int it = 0; it < values.Length; it++)
        {
            minmax.x = Mathf.Min(minmax.x, values[it]);
            minmax.y = Mathf.Max(minmax.y, values[it]);
        }

        float denom = 1.0f / (minmax.y - minmax.x);
        for (int it = 0; it < values.Length; it++)
        {
            float val = (values[it] - minmax.x) * denom;
            Vector4 enc = new Vector4(1.0f, 255.0f, 65025.0f, 160581375.0f) * val;
            enc -= new Vector4(Mathf.Floor(enc.x), Mathf.Floor(enc.y), Mathf.Floor(enc.z), Mathf.Floor(enc.w));
            enc -= new Vector4(enc.y / 255.0f, enc.z / 255.0f, enc.w / 255.0f, 0.0f);
            c[it] = new Color(enc.x, enc.y, enc.z, enc.w);
        }

        range = minmax;
        return c;
    }

    public static Color[] EncodeFloatSimple(float[] values, out Vector2 range)
    {
        Vector2 minmax = new Vector2(values[0], values[0]);
        Color[] c = new Color[values.Length];
        for (int it = 0; it < values.Length; it++)
        {
            minmax.x = Mathf.Min(minmax.x, values[it]);
            minmax.y = Mathf.Max(minmax.y, values[it]);
        }

        float denom = 1.0f / (minmax.y - minmax.x);
        for (int it = 0; it < values.Length; it++)
        {
            float val = (values[it] - minmax.x) * denom;
            c[it] = new Color(val, val, val, val);
        }

        range = minmax;
        return c;
    }

    static void FillRandom(float[] arr)
    {
        for(int it = 0; it < arr.Length; it++)
        {
            arr[it] = Random.value;
        }
    }

    static TriangleMap BuildTriangleMap(List<MeshFilter> mfs, Bounds bounds)
    {
        //Step 1 - compute the number of triangles we need
        int numTris = 0;
        for(int mesh_idx = 0; mesh_idx < mfs.Count; mesh_idx++)
        {
            Mesh mesh = mfs[mesh_idx].sharedMesh;
            numTris += mesh.triangles.Length;
        }

        //Step 2 - build the map
        //This boils down to storing some information for each pixel that corresponds to
        //the vertex positions + normals of each face
        //We need to compute a texture to store all of these triangles
        //We use the following formula to over-approximate the size of the triangles
        //and ensure we have a texture that can encapsulate them all
        //In practice, you'd want to do this differently since different hardware has different
        //max texture sizes, but we don't do that yet
        int ideal2DPixels = Mathf.CeilToInt(Mathf.Sqrt((float)numTris));
        int totalImageSize = ideal2DPixels * ideal2DPixels;
        Color[] triPixelsV0 = new Color[totalImageSize];
        Color[] triPixelsV1 = new Color[totalImageSize];
        Color[] triPixelsV2 = new Color[totalImageSize];

        Vector3 extent = bounds.max - bounds.min;
        Vector3 invBounds = new Vector3(1.0f / extent.x, 1.0f / extent.y, 1.0f / extent.z);
        int triPixelIdx = 0;
        //loop over each mesh
        for(int mesh_idx = 0; mesh_idx < mfs.Count; mesh_idx++)
        {
            Transform meshT = mfs[mesh_idx].transform;
            Mesh mesh = mfs[mesh_idx].sharedMesh;
            Vector3[] vertices = mesh.vertices;

            //transform vertices to the cube space
            for(int v_idx = 0; v_idx < vertices.Length; v_idx++)
            {
                Vector3 vertWorldSpace = meshT.TransformPoint(vertices[v_idx]);
                vertWorldSpace -= bounds.min;
                vertWorldSpace = Vector3.Scale(vertWorldSpace, invBounds);
                vertices[v_idx] = vertWorldSpace;
            }

            int[] indices = mesh.triangles;
            //loop over each triangle
            for(int f_idx = 0; f_idx < indices.Length; f_idx += 3, triPixelIdx++)
            {
                int i0 = indices[f_idx];
                int i1 = indices[f_idx + 1];
                int i2 = indices[f_idx + 2];
                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 n = Vector3.Cross(v1 - v0, v0 - v2);
                //Encode the vertices in the triangle map
                //We could also precompute other values and speed up the triangle tests
                //in our compute shader, but this will do for now
                //It would be nice to eventually push most of this computation to the compute
                //shader as well
                triPixelsV0[triPixelIdx] = new Color(v0.x, v0.y, v0.z, n.x);
                triPixelsV1[triPixelIdx] = new Color(v1.x, v1.y, v1.z, n.y);
                triPixelsV2[triPixelIdx] = new Color(v2.x, v2.y, v2.z, n.z);
            }
        }

        TriangleMap result = new TriangleMap();
        result.v0 = new Texture2D(ideal2DPixels, ideal2DPixels, TextureFormat.RGBAFloat, false, true);
        result.v1 = new Texture2D(ideal2DPixels, ideal2DPixels, TextureFormat.RGBAFloat, false, true);
        result.v2 = new Texture2D(ideal2DPixels, ideal2DPixels, TextureFormat.RGBAFloat, false, true);
        //Upload the data - this is a very expensive operation
        result.v0.SetPixels(triPixelsV0); result.v0.Apply();
        result.v1.SetPixels(triPixelsV1); result.v1.Apply();
        result.v2.SetPixels(triPixelsV2); result.v2.Apply();
        result.width = ideal2DPixels;
        result.height = ideal2DPixels;
        result.numTriangles = numTris;
        return result;
    }

    public static SDFImage BuildSDF3DFromMeshes(GameObject root,
        int width, int height, int depth,
        ComputeShader cs)
    {
        //Step 0 - compute the bounds of the SDF
        MeshRenderer[] rs = root.GetComponentsInChildren<MeshRenderer>();
        List<MeshFilter> mfs = new List<MeshFilter>();
        bool isAssigned = false;
        Bounds bounds = new Bounds();
        foreach (MeshRenderer r in rs)
        {
            //skip meshes without mesh filters (rare case, but could happen)
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null)
                continue;

            mfs.Add(mf);
            if (!isAssigned)
            {
                isAssigned = true;
                bounds.min = r.bounds.min;
                bounds.max = r.bounds.max;
            }
            else
            {
                bounds.min = Vector3.Min(bounds.min, r.bounds.min);
                bounds.max = Vector3.Max(bounds.max, r.bounds.max);
            }
        }
        //enlarge bounds slightly so objects at the extrema of the bounds get rendered properly
        Vector3 extent = bounds.max - bounds.min;
        Vector3 scaled = new Vector3(extent.x / (float)width, extent.y / (float)height, extent.z / (float)depth);
        bounds.min = bounds.min - scaled * 10;
        bounds.max = bounds.max + scaled * 10;

        //Step 2 - build the triangle maps
        //In the future, it'd be really good to reuse textures
        //or have some smart caching scheme, but for now we just recreate them every time the function
        //is called. This is likely one of the most expensive steps in this function
        //probably even more expensive than actually building the SDF
        //Most of this function could probably also be run on compute shaders with a little bit of cleverness
        TriangleMap tMap = BuildTriangleMap(mfs, bounds);

        //Step 3 - allocate the SDF texture
        //We use a big 2D render texture and use some modulo division to compute the 3D index we're at
        //Alternatives would be to render directly into the volume, but the sense I get from Unity's docs is
        //that the support for this is kinda limited and very hardware-dependent
        int numElems = (width * height * depth);
        int ideal2DPixels = Mathf.CeilToInt(Mathf.Sqrt((float)numElems));
        int texWidth = ideal2DPixels;
        int texHeight = ideal2DPixels;

        Debug.Log("Creating texture of size " + texWidth + "x" + texHeight);
        //TODO - this really should be a RFloat, but I'm not sure how the SetPixels()/GetPixels() operations work with those
        //and didn't bother to take the time to investigate that
        RenderTexture rt3D = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt3D.enableRandomWrite = true;
        rt3D.filterMode = FilterMode.Point;
        rt3D.Create();

        //Step 4 - build the SDF
        //This uses a brute-force triangle to point test to compute the SDF
        //but could likely be accelerated by using spatial data structures and pre-computing more of the
        //per-triangle equation terms
        int kernel = cs.FindKernel("SDFInit");
        uint threadX, threadY, threadZ;
        cs.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
        cs.SetInt(Shader.PropertyToID("_width"), width);
        cs.SetInt(Shader.PropertyToID("_height"), height);
        cs.SetInt(Shader.PropertyToID("_depth"), depth);
        cs.SetInt(Shader.PropertyToID("_texWidth"), texWidth);
        cs.SetInt(Shader.PropertyToID("_texHeight"), texHeight);
        cs.SetTexture(kernel, "_vertexMap0", tMap.v0);
        cs.SetTexture(kernel, "_vertexMap1", tMap.v1);
        cs.SetTexture(kernel, "_vertexMap2", tMap.v2);
        cs.SetInt(Shader.PropertyToID("_triMapWidth"), tMap.width);
        cs.SetInt(Shader.PropertyToID("_triMapHeight"), tMap.height);
        cs.SetInt(Shader.PropertyToID("_numTriangles"), tMap.numTriangles);
        cs.SetFloat(Shader.PropertyToID("_infinity"), float.PositiveInfinity);
        cs.SetVector(Shader.PropertyToID("_minBounds"), bounds.min);
        cs.SetVector(Shader.PropertyToID("_maxBounds"), bounds.max);
        cs.SetTexture(kernel, "_SDF", rt3D);
        //And now we dispatch it and pray our GPU doesn't explode!
        cs.Dispatch(kernel, Mathf.CeilToInt((float)texWidth / (float)threadX), Mathf.CeilToInt((float)texHeight / (float)threadY), (int)threadZ);

        //Step 4 - copy the SDF to a volume texture
        //This is also expensive, since we need to read the data to CPU and then re-upload to the GPU
        //but meh - I don't know of a better way and didn't want to take the time to investigate
        //Unity's limited render to volume support
        RenderTexture src = rt3D;
        Texture2D newTex = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false);
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = src;
        newTex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        newTex.Apply();
        RenderTexture.active = old;

        //Finally, release the rendertexture. It's had a good run, but all good things
        //must come to an end
        //Is Unity really so dumb that you need to manually release the old RenderTexture?
        //It's 2017, people! Ever heard of ref-counting?
        rt3D.Release();

        //Old pixels is slightly too large to be copied directly
        //so we copy into an array of the appropriate size
        Color[] oldPixels = newTex.GetPixels();
        Color[] newPixels = new Color[width * height * depth];
        int imgStride = 0;
        while (imgStride < newPixels.Length)
        {
            int x = imgStride % width;
            int y = (imgStride / width) % height;
            int z = (imgStride / (width * height));
            int volumeStride = x + y * width + z * (width * height);
            newPixels[volumeStride] = oldPixels[imgStride];
            imgStride++;
        }

        //And the best part is we can filter these volume textures in hardware!
        Texture3D tex = new Texture3D(width, height, depth, TextureFormat.RGBAFloat, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(newPixels);
        tex.Apply();
        
        SDFImage result = new SDFImage();
        result.Image = tex;
        result.EncodingRange = new Vector2(0, 1);
        result.Bounds = bounds;
        return result;
    }

    public static SDFImage BuildSDF3DCompute(Texture2D heightmap, int depth, ComputeShader cs)
    {
        int w = heightmap.width;
        int h = depth;
        int d = heightmap.height;
        int numElems = (w * h * d);
        int ideal2DPixels = Mathf.CeilToInt(Mathf.Sqrt((float)numElems));
        int texWidth = ideal2DPixels;
        int texHeight = ideal2DPixels;
        Debug.Log("Creating texture of size " + texWidth + "x" + texHeight);
        RenderTexture rt3D = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt3D.enableRandomWrite = true;
        rt3D.filterMode = FilterMode.Point;
        rt3D.Create();

        
        int kernel = cs.FindKernel("SDFInit");
        uint threadX, threadY, threadZ;
        cs.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
        cs.SetInt(Shader.PropertyToID("_width"), w);
        cs.SetInt(Shader.PropertyToID("_depth"), d);
        cs.SetInt(Shader.PropertyToID("_height"), h);
        cs.SetInt(Shader.PropertyToID("_texWidth"), texWidth);
        cs.SetInt(Shader.PropertyToID("_texHeight"), texHeight);
        cs.SetTexture(kernel, "Heightmap", heightmap);
        cs.SetTexture(kernel, "Result", rt3D);
        cs.Dispatch(kernel, Mathf.CeilToInt((float)texWidth / (float)threadX), Mathf.CeilToInt((float)texHeight / (float)threadY), (int)threadZ);

        RenderTexture src = rt3D;
        Texture2D newTex = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false);
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = src;
        newTex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        newTex.Apply();
        RenderTexture.active = old;

        Color[] oldPixels = newTex.GetPixels();
        Color[] newPixels = new Color[w * h * d];
        int imgStride = 0;
        while(imgStride < newPixels.Length)
        {
            int x = imgStride % w;
            int y = (imgStride / w) % h;
            int z = (imgStride / (w * h));
            int volumeStride = x + y * w + z * (w * h);
            newPixels[volumeStride] = oldPixels[imgStride];
            imgStride++;
            //Debug.Log(newPixels[volumeStride]);
        }

        Texture3D tex = new Texture3D(w, h, d, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(newPixels);
        tex.Apply();


        SDFImage result = new SDFImage();
        result.Image = tex;
        result.EncodingRange = new Vector2(0, 1);

        return result;
    }

    public static SDFImage BuildSDF3D(float[] hVals, int width, int height, int depth)
    {
        float[] values = BuildSDF3DFromHeightmap(hVals, width, height, depth);

        Vector2 range;
        //Color[] encoded = EncodeFloat(values, out range);
        Color[] encoded = EncodeFloatSimple(values, out range);
        Texture3D tex = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(encoded);
        tex.Apply();

        SDFImage img = new SDFImage();
        img.Image = tex;
        img.EncodingRange = range;
        return img;
    }
}
