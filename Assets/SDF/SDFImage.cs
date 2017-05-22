using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SDFImage
{
    public Texture3D Image;
    public Vector2 EncodingRange;
}
 
public struct Image25D
{
    public Texture2D Image;
    public Vector3 MinPoint;
    public Vector3 MaxPoint;
}