using System;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class DynamicPlayfieldBounds : MonoBehaviour
{
    public enum PaddingMode
    {
        WorldUnits,
        ScreenPercent
    }

    static DynamicPlayfieldBounds _instance;
    [Header("Camera")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float playfieldZ = 0f;

    [Header("Boundary Planes")]
    [SerializeField] Transform leftBoundary;
    [SerializeField] Transform rightBoundary;
    [SerializeField] Transform topBoundary;
    [SerializeField] Transform bottomBoundary;

    [Header("Sizing")]
    [SerializeField] PaddingMode paddingMode = PaddingMode.WorldUnits;

    [Tooltip("World units if Padding Mode is WorldUnits. 0.05 = 5% if Padding Mode is ScreenPercent.")]
    [SerializeField] Vector2 padding = new Vector2(0.5f, 0.5f);

    [SerializeField] float boundaryThickness = 0.25f;

    public static Rect PlayfieldRect { get; private set; }

    public static event Action<Rect> BoundsChanged;

    static int lastScreenWidth;
    static int lastScreenHeight;
    Rect lastRect;
    public static void Initialize(Action<bool> onInitialized)
    {
        var rebuildSuccessful = _instance.TryRebuild();
        onInitialized?.Invoke(rebuildSuccessful);
    }
    void Reset()
    {
        targetCamera = Camera.main;
    }

    void OnEnable()
    {
        TryRebuild();
        _instance = this;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            return;

        // Cheap enough to check every frame, useful for editor resizing too.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || !Application.isPlaying)
        {
            TryRebuild();
        }
    }

    void OnValidate()
    {
        TryRebuild();
    }

    public bool TryRebuild()
    {
        if (targetCamera == null)
        {
            return false;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Rect cameraRect = GetCameraWorldRectAtZ(playfieldZ);
        PlayfieldRect = ApplyPadding(cameraRect);

        PositionBoundaries(PlayfieldRect);

        if (PlayfieldRect != lastRect)
        {
            lastRect = PlayfieldRect;
            BoundsChanged?.Invoke(PlayfieldRect);
        }
        return true;
    }

    Rect GetCameraWorldRectAtZ(float z)
    {
        Vector3 bottomLeft = ViewportToWorldPointOnZ(new Vector2(0f, 0f), z);
        Vector3 topRight = ViewportToWorldPointOnZ(new Vector2(1f, 1f), z);

        return Rect.MinMaxRect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x,
            topRight.y
        );
    }

    Vector3 ViewportToWorldPointOnZ(Vector2 viewportPosition, float z)
    {
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(viewportPosition.x, viewportPosition.y, 0f));

        Plane playPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));

        if (playPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    Rect ApplyPadding(Rect cameraRect)
    {
        float xPadding = padding.x;
        float yPadding = padding.y;

        if (paddingMode == PaddingMode.ScreenPercent)
        {
            xPadding = cameraRect.width * padding.x;
            yPadding = cameraRect.height * padding.y;
        }

        xPadding = Mathf.Max(0f, xPadding);
        yPadding = Mathf.Max(0f, yPadding);

        float maxXPadding = cameraRect.width * 0.49f;
        float maxYPadding = cameraRect.height * 0.49f;

        xPadding = Mathf.Min(xPadding, maxXPadding);
        yPadding = Mathf.Min(yPadding, maxYPadding);

        return Rect.MinMaxRect(
            cameraRect.xMin + xPadding,
            cameraRect.yMin + yPadding,
            cameraRect.xMax - xPadding,
            cameraRect.yMax - yPadding
        );
    }

    void PositionBoundaries(Rect rect)
    {
        float t = boundaryThickness;

        SetBoundary(
            leftBoundary,
            new Vector2(rect.xMin - t * 0.5f, rect.center.y),
            new Vector2(t, rect.height + t * 2f)
        );

        SetBoundary(
            rightBoundary,
            new Vector2(rect.xMax + t * 0.5f, rect.center.y),
            new Vector2(t, rect.height + t * 2f)
        );

        SetBoundary(
            topBoundary,
            new Vector2(rect.center.x, rect.yMax + t * 0.5f),
            new Vector2(rect.width + t * 2f, t)
        );

        SetBoundary(
            bottomBoundary,
            new Vector2(rect.center.x, rect.yMin - t * 0.5f),
            new Vector2(rect.width + t * 2f, t)
        );
    }

    void SetBoundary(Transform boundary, Vector2 center, Vector2 size)
    {
        if (boundary == null)
            return;

        boundary.position = new Vector3(center.x, center.y, playfieldZ);
        boundary.localRotation = Quaternion.Euler(90f, 0f, 0f);
        boundary.localScale = new Vector3(size.x / 10f, boundary.localScale.y, size.y / 10f);
    }

}