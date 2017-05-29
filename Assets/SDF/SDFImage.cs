using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SDFImage
{
    public Texture3D Image;
    public Vector2 EncodingRange;
    public Bounds Bounds;
}

public struct TriangleMap
{
    public Texture2D v0;
    public Texture2D v1;
    public Texture2D v2;
    public int numTriangles;
    public int width;
    public int height;
}
 
public struct Image25D
{
    public Texture2D Image;
    public Vector3 MinPoint;
    public Vector3 MaxPoint;
}