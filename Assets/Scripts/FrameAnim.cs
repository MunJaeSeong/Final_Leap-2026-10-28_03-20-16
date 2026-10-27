using UnityEngine;

public class FrameAnim : MonoBehaviour
{
    public SpriteRenderer Target;
    public bool Finished { get; private set; }
    public float Normalized
    {
        get
        {
            if (frames == null || frames.Length == 0 || fps <= 0f)
                return 0f;
            return Mathf.Clamp01(time / (frames.Length / fps));
        }
    }

    Sprite[] frames;
    float fps = 10f;
    bool loop = true;
    float time;
    int index = -1;

    void Awake()
    {
        if (Target == null)
            Target = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || Target == null)
            return;

        time += Time.deltaTime;
        int next = Mathf.FloorToInt(time * fps);
        if (loop)
        {
            next %= frames.Length;
            Finished = false;
        }
        else if (next >= frames.Length)
        {
            next = frames.Length - 1;
            Finished = true;
        }

        if (next != index && frames[next] != null)
        {
            index = next;
            Target.sprite = frames[next];
        }
    }

    public void Play(Sprite[] nextFrames, float nextFps, bool nextLoop, bool restart)
    {
        if (!restart && frames == nextFrames && Mathf.Approximately(fps, nextFps) && loop == nextLoop)
            return;

        frames = nextFrames;
        fps = Mathf.Max(1f, nextFps);
        loop = nextLoop;
        time = 0f;
        index = -1;
        Finished = false;
        if (Target != null && frames != null && frames.Length > 0 && frames[0] != null)
        {
            index = 0;
            Target.sprite = frames[0];
        }
    }
}
