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
