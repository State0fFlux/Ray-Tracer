using UnityEngine;

////////////////////////////////////////////////
// PLEASE DO NOT MODIFY THIS FILE
////////////////////////////////////////////////

/// <summary>
/// A custom Material object with diffuse, specular, emissive, and transparency components
/// </summary>
public class Material
{
    public readonly float Shininess;
    public readonly float IndexOfRefraction;

    public Color Kd;
    public Color Ks;
    public Color Ke;
    public Color Kt;

    public Material(UnityEngine.Material mat)
    {
        Kd = mat.GetColor("_DiffuseColor");
        Ks = mat.GetColor("_SpecularColor");
        Ke = mat.GetColor("_EmissionColor");
        Kt = mat.GetColor("_TransparencyColor");

        Shininess = mat.GetFloat("_Shininess");
        IndexOfRefraction = mat.GetFloat("_TransparencyIndex");
    }
}