using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public float MinX = 4f;
    public float MaxX = 40f;
    public float MinY = -1.1f;
    public float MaxY = 3.2f;
    public bool TrackHeight;
    public float OrthoSize = 4.55f;

    float xPos;
    float yPos;
    float xVelocity;
    float yVelocity;
    bool framed;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = OrthoSize;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 40f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.07f, 0.1f);
        transform.position = new Vector3(0f, 1.2f, -10f);
    }

    void LateUpdate()
    {
        if (Target == null)
        {
            framed = false;
            return;
        }

        float x = Mathf.Clamp(Target.position.x + 1.35f, MinX, MaxX);
        if (!framed)
        {
            xPos = x;
            yPos = Mathf.Clamp(Target.position.y + 0.35f, MinY, MaxY);
            framed = true;
        }

        xPos = Mathf.SmoothDamp(xPos, x, ref xVelocity, 0.14f);

        float rise = Target.position.y - yPos;
        float up = TrackHeight ? 1.15f : 2.7f;
        float down = TrackHeight ? 1.4f : 2.15f;
        float goalY = yPos;
        if (rise > up)
            goalY = Target.position.y - up;
        else if (rise < -down)
            goalY = Target.position.y + down;
        goalY = Mathf.Clamp(goalY, MinY, MaxY);
        yPos = Mathf.SmoothDamp(yPos, goalY, ref yVelocity, 0.35f);
        transform.position = new Vector3(xPos, yPos, -10f);
    }

    public void Focus(Transform target, float minX, float maxX, float minY, float maxY, bool trackHeight)
    {
        Target = target;
        MinX = minX;
        MaxX = Mathf.Max(minX + 1f, maxX);
        MinY = minY;
        MaxY = Mathf.Max(minY + 1f, maxY);
        TrackHeight = trackHeight;
        framed = false;
    }
}
