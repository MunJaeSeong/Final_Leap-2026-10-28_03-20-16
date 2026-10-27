using UnityEngine;
using UnityEngine.InputSystem;

public enum RoomId
{
    Entrance = 0,
    Passage,
    Plaza,
    Platforms,
    Hidden,
    Altar,
    Abyss,
    BossGate,
    Boss
}

public static class WorldProgress
{
    public static bool HasEmber;
}

public struct RoomLink
{
    public RoomId Target;
    public string Label;
    public bool Back;
    public bool NeedsEmber;
}

public class RoomInfo
{
    public RoomId Id;
    public string Code;
    public string Name;
    public string Kind;
    public Vector2 MapAnchor;
    public RoomLink[] Links;
}

public static class WorldCatalog
{
    public static readonly RoomInfo[] Rooms =
    {
        Room(RoomId.Entrance, "1-1", "잊혀진 입구", "일반", new Vector2(0.08f, 0.68f),
            Link(RoomId.Passage, "1-2 동굴 통로", false)),
        Room(RoomId.Passage, "1-2", "동굴 통로", "일반", new Vector2(0.24f, 0.68f),
            Link(RoomId.Entrance, "1-1 잊혀진 입구", true),
            Link(RoomId.Plaza, "1-3 버려진 광장", false)),
        Room(RoomId.Plaza, "1-3", "버려진 광장", "일반", new Vector2(0.42f, 0.68f),
            Link(RoomId.Passage, "1-2 동굴 통로", true),
            Link(RoomId.Platforms, "1-4 공중의 발판", false)),
        Room(RoomId.Platforms, "1-4", "공중의 발판", "점프", new Vector2(0.32f, 0.40f),
            Link(RoomId.Plaza, "1-3 버려진 광장", true),
            Link(RoomId.Hidden, "1-5 숨겨진 동굴", false)),
        Room(RoomId.Hidden, "1-5", "숨겨진 동굴", "점프", new Vector2(0.52f, 0.40f),
            Link(RoomId.Platforms, "1-4 공중의 발판", true),
            Link(RoomId.Altar, "1-6 잊혀진 제단", false),
            Link(RoomId.BossGate, "보스 진입", false)),
        Room(RoomId.Altar, "1-6", "잊혀진 제단", "일반", new Vector2(0.16f, 0.14f),
            Link(RoomId.Hidden, "1-5 숨겨진 동굴", true),
            Link(RoomId.Abyss, "1-7 불의 심연", false)),
        Room(RoomId.Abyss, "1-7", "불의 심연", "아이템", new Vector2(0.36f, 0.14f),
            Link(RoomId.Altar, "1-6 잊혀진 제단", true)),
        Room(RoomId.BossGate, "관문", "보스 진입", "트리거", new Vector2(0.70f, 0.50f),
            Link(RoomId.Hidden, "1-5 숨겨진 동굴", true),
            Link(RoomId.Boss, "1-8 최후의 전장", false, true)),
        Room(RoomId.Boss, "1-8", "최후의 전장", "보스", new Vector2(0.90f, 0.68f))
    };

    public static RoomInfo Get(RoomId id)
    {
        return Rooms[(int)id];
    }

    static RoomInfo Room(RoomId id, string code, string name, string kind, Vector2 anchor, params RoomLink[] links)
    {
        return new RoomInfo
        {
            Id = id,
            Code = code,
            Name = name,
            Kind = kind,
            MapAnchor = anchor,
            Links = links
        };
    }

    static RoomLink Link(RoomId target, string label, bool back, bool needsEmber = false)
    {
        return new RoomLink { Target = target, Label = label, Back = back, NeedsEmber = needsEmber };
    }
}

public class StagePortal : MonoBehaviour
{
    public RoomId Target;
    public string Label;
    public bool NeedsEmber;
    public Vector3 Feet;
    bool inside;
    TextMesh caption;
    Transform swirl;
    Transform core;
    Transform haze;
    static Sprite vortex;

    public static StagePortal Create(Transform parent, Vector3 feet, RoomLink link)
    {
        var go = new GameObject("Portal");
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var portal = go.AddComponent<StagePortal>();
        portal.Target = link.Target;
        portal.Label = link.Label;
        portal.NeedsEmber = link.NeedsEmber;
        portal.Feet = feet;

        portal.haze = Disc(go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(3.2f, 4.1f, 1f), ArtBank.SoftCircle, new Color(0.5f, 0.12f, 1f, 0.45f), 12);
        Disc(go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.3f, 0.62f, 1f), ArtBank.SoftCircle, new Color(0.62f, 0.18f, 1f, 0.9f), 13);
        Disc(go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.72f, 1.05f, 1f), VortexSprite(), new Color(0.16f, 0.02f, 0.26f, 0.95f), 14);
        portal.swirl = Disc(go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.95f, 1.28f, 1f), VortexSprite(), new Color(0.78f, 0.32f, 1f, 1f), 15);
        portal.core = Disc(go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.7f, 0.95f, 1f), ArtBank.SoftCircle, new Color(0.92f, 0.7f, 1f, 0.95f), 16);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 2.85f, 0f);
        portal.caption = textGo.AddComponent<TextMesh>();
        portal.caption.font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕" }, 48);
        portal.caption.text = "↑  " + link.Label;
        portal.caption.characterSize = 0.1f;
        portal.caption.anchor = TextAnchor.MiddleCenter;
        portal.caption.alignment = TextAlignment.Center;
        portal.caption.color = new Color(0.86f, 0.72f, 1f);
        portal.caption.fontSize = 48;
        textGo.GetComponent<MeshRenderer>().sortingOrder = 30;

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.35f, 2.5f);
        trigger.offset = new Vector2(0f, 1.2f);
        return portal;
    }

    static Transform Disc(Transform parent, Vector3 localPos, Vector3 scale, Sprite sprite, Color color, int order)
    {
        var go = new GameObject("Disc");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.color = color;
        view.sortingOrder = order;
        return go.transform;
    }

    static Sprite VortexSprite()
    {
        if (vortex != null)
            return vortex;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float ring = Mathf.Exp(-Mathf.Pow((radius - 0.72f) / 0.11f, 2f));
                float band = Mathf.Exp(-Mathf.Pow((radius - 0.48f) / 0.08f, 2f)) * 0.65f;
                float well = Mathf.Clamp01((0.42f - radius) / 0.42f);
                float arm = 0.5f + 0.5f * Mathf.Sin(angle * 4f + radius * 14f);
                float alpha = Mathf.Clamp01(ring + band + well * 0.35f);
                Color color = Color.Lerp(new Color(0.42f, 0.08f, 0.85f, 1f), new Color(0.95f, 0.72f, 1f, 1f), arm);
                color.a = alpha * (radius > 1f ? 0f : 1f);
                pixels[y * size + x] = color;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        vortex = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        return vortex;
    }

    void Update()
    {
        float time = Time.time;
        if (swirl != null)
        {
            swirl.localRotation = Quaternion.Euler(0f, 0f, time * 70f);
            float pulse = 1f + 0.07f * Mathf.Sin(time * 4.5f);
            swirl.localScale = new Vector3(0.95f * pulse, 1.28f * pulse, 1f);
        }
        if (core != null)
            core.localScale = new Vector3(0.7f, 0.95f, 1f) * (1f + 0.12f * Mathf.Sin(time * 6f));
        if (haze != null)
            haze.localScale = new Vector3(3.2f, 4.1f, 1f) * (1f + 0.06f * Mathf.Sin(time * 3f));

        if (!inside || GameRoot.Instance == null || GameRoot.Instance.Paused || GameRoot.Instance.MapOpen || GameRoot.Instance.PortalHold)
            return;
        var knight = PlayerKnight.Instance;
        if (knight == null || knight.InputLocked || knight.Dead)
            return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.upArrowKey.wasPressedThisFrame)
            GameRoot.Instance.EnterRoom(Target, NeedsEmber);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Touch(other, true);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Touch(other, true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Touch(other, false);
    }

    void Touch(Collider2D other, bool entered)
    {
        if (other.GetComponent<PlayerKnight>() == null)
            return;
        inside = entered;
        if (caption != null)
            caption.color = entered ? new Color(0.95f, 0.85f, 1f) : new Color(0.86f, 0.72f, 1f);
    }
}

public class EmberPickup : MonoBehaviour
{
    public int Hp = 100;
    bool broken;
    float flash;
    Transform flame;
    OverheadHp hpBar;

    public static void Create(Transform parent, Vector3 feet)
    {
        var go = new GameObject("Ember");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var ember = go.AddComponent<EmberPickup>();

        ember.flame = new GameObject("Flame").transform;
        ember.flame.SetParent(go.transform, false);
        Blob(ember.flame, new Vector3(0f, 0.16f, 0f), new Vector3(0.85f, 0.28f, 1f), new Color(0.55f, 0.12f, 0.04f, 0.95f), 21);
        Blob(ember.flame, new Vector3(0f, 0.62f, 0f), new Vector3(0.48f, 0.7f, 1f), new Color(1f, 0.38f, 0.08f, 0.98f), 22);
        Blob(ember.flame, new Vector3(0f, 1.15f, 0f), new Vector3(0.26f, 0.48f, 1f), new Color(1f, 0.86f, 0.4f, 1f), 23);
        Blob(ember.flame, new Vector3(0f, 0.7f, 0f), new Vector3(1.15f, 1.35f, 1f), new Color(0.35f, 0.72f, 1f, 0.28f), 20);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        var caption = textGo.AddComponent<TextMesh>();
        caption.font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕" }, 48);
        caption.text = "심연의 불씨";
        caption.characterSize = 0.08f;
        caption.anchor = TextAnchor.MiddleCenter;
        caption.alignment = TextAlignment.Center;
        caption.color = new Color(1f, 0.86f, 0.55f);
        caption.fontSize = 48;
        textGo.GetComponent<MeshRenderer>().sortingOrder = 41;

        ember.hpBar = OverheadHp.Attach(go.transform, ember.Hp, 1.72f);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.4f, 1.8f);
        box.offset = new Vector2(0f, 0.85f);
    }

    static void Blob(Transform parent, Vector3 local, Vector3 scale, Color color, int order)
    {
        var go = new GameObject("Fire");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = ArtBank.SoftCircle;
        view.color = color;
        view.sortingOrder = order;
    }

    void Update()
    {
        if (flame != null)
        {
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 7f);
            flame.localScale = new Vector3(pulse, 1f + 0.08f * Mathf.Sin(Time.time * 9f), 1f);
        }
        if (flash <= 0f)
            return;
        flash -= Time.deltaTime;
    }

    public void TakeHit(int damage)
    {
        if (broken || damage <= 0)
            return;
        Hp -= damage;
        flash = 0.16f;
        if (hpBar != null)
            hpBar.Set(Hp);
        if (Hp > 0)
            return;
        broken = true;
        WorldProgress.HasEmber = true;
        AudioFx.Play("checkpoint");
        GameRoot.Instance?.ShowBanner("심연의 불씨를 얻었다. 보스 진입에서 사용할 수 있다.", 2.4f);
        Destroy(gameObject);
    }
}
