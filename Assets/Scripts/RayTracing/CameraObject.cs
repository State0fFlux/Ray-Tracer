using UnityEngine;

////////////////////////////////////////////////
// PLEASE DO NOT MODIFY THIS FILE
////////////////////////////////////////////////

/// <summary>
/// Since Unity does not allow to make calls to UnityEngine API while
/// multithreading, we need to create a custom CameraObject from a
/// given Camera GameObject to use its functionalities
/// </summary>
public class CameraObject
{
    private readonly float _screenWidth;
    private readonly float _screenHeight;
    private readonly Matrix4x4 _cameraToWorld;
    private readonly Matrix4x4 _cameraProjectionInverse;
    private readonly Vector3 _cameraOrigin;

    public CameraObject(float screenWidth,
        float screenHeight,
        Matrix4x4 cameraToWorld,
        Matrix4x4 cameraProjectionInverse,
        Vector3 cameraOrigin)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _cameraToWorld = cameraToWorld;
        _cameraProjectionInverse = cameraProjectionInverse;
        _cameraOrigin = cameraOrigin;
    }

    public Ray ScreenToWorldRay(Vector2 screenPosition)
    {
        screenPosition = new Vector2(screenPosition.x, _screenHeight - screenPosition.y);

        Vector4 clipSpace = new Vector4(((screenPosition.x * 2.0f) / _screenWidth) - 1.0f,
            (1.0f - (2.0f * screenPosition.y) / _screenHeight), 0.0f, 1.0f);

        Vector4 viewSpace = (_cameraProjectionInverse * clipSpace);
        viewSpace /= viewSpace.w;

        Vector4 worldSpace = _cameraToWorld * viewSpace;

        Vector3 worldDirection =
            Vector3.Normalize(new Vector3(worldSpace.x, worldSpace.y, worldSpace.z) - _cameraOrigin);

        return new Ray(_cameraOrigin, worldDirection);
    }
}