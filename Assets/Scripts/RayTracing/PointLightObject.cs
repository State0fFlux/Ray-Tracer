using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointLightObject
{
    public Vector3 LightPos;
    public readonly float Intensity;
    public Color Color;

    public PointLightObject(Vector3 lightPos, float intensity, Color color)
    {
        LightPos = lightPos;
        Intensity = intensity;
        Color = color;
    }
}