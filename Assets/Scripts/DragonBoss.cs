using UnityEngine;

public class DragonBoss : MonoBehaviour
{
    public const int MaxHp = 1000;
    public int Hp = MaxHp;
    public int Phase { get; private set; } = 1;
    public bool PatternPending { get; private set; }
    public bool Dead => dead;
    public static DragonBoss Active { get; private set; }

    enum Act { Fly, Dash, Roar, Breath }

    SpriteRenderer view;
    FrameAnim anim;
    Sprite[] flyFrames;
    Sprite[] dashFrames;
    Sprite[] roarFrames;
    Sprite[] breathFrames;
    Act act = Act.Fly;
    float actTime;
    float dashTimer;
    float roarTimer = 8f;
    float breathTimer = 7f;
    float meleeTimer = 1.4f;
    float meleeActive;
    bool skillHit;
    bool meleeHit;
    bool sealedBoss;
    float stagger;
    SpriteRenderer breathWarn;
    float dir = -1f;
    float hitFlash;
    bool dead;
    Vector3 wander;
    float wanderTime;
    float minX;
    float maxX;
    float minY;
    float maxY;
    Transform visual;

    public static DragonBoss Spawn(Transform parent, Vector3 point, float airMinX, float airMaxX, float airMinY, float airMaxY)
    {
        var go = new GameObject("Dragon");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.position = point;
        var dragon = go.AddComponent<DragonBoss>();
        dragon.minX = airMinX;
        dragon.maxX = airMaxX;
        dragon.minY = airMinY;
        dragon.maxY = airMaxY;
        return dragon;
    }

    void Awake()
    {
        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.7f, 1.05f);
        box.offset = new Vector2(0f, -0.08f);

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        view = visual.gameObject.AddComponent<SpriteRenderer>();
        view.sortingOrder = 18;
        anim = visual.gameObject.AddComponent<FrameAnim>();
        anim.Target = view;

        flyFrames = ArtBank.Dragons(ArtBank.DragonFly);
        dashFrames = ArtBank.Dragons(ArtBank.DragonDash);
        roarFrames = ArtBank.Dragons(ArtBank.DragonRoar);
        breathFrames = ArtBank.Dragons(ArtBank.DragonBreath);
        if (flyFrames.Length > 0 && flyFrames[0] != null)
            view.sprite = flyFrames[0];

        transform.localScale = Vector3.one * 1.2f;
        dashTimer = Random.Range(20f, 30f);
        PickWander();

        var warnGo = new GameObject("BreathWarn");
        warnGo.transform.SetParent(transform, false);
        breathWarn = warnGo.AddComponent<SpriteRenderer>();
        breathWarn.sprite = ArtBank.SoftCircle;
        breathWarn.sortingOrder = 16;
        breathWarn.enabled = false;
    }

    void OnEnable()
    {
        Active = this;
    }

    void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    void Update()
    {
        if (dead)
            return;

        dashTimer -= Time.deltaTime;
        if (Phase >= 2)
            roarTimer -= Time.deltaTime;
        if (Phase >= 3)
            breathTimer -= Time.deltaTime;

        if (stagger > 0f)
        {
            stagger -= Time.deltaTime;
            act = Act.Fly;
            anim.Play(flyFrames, 6f, true, false);
        }
        else if (act == Act.Fly)
        {
            Fly();
            if (!sealedBoss)
                TryBeginSkill();
        }
        else if (act == Act.Dash)
            TickDash();
        else if (act == Act.Roar)
            TickRoar();
        else
            TickBreath();

        if (!sealedBoss && stagger <= 0f && (act == Act.Fly || act == Act.Dash))
            TickMelee();
        else
            meleeActive = 0f;

        if (breathWarn != null && act != Act.Breath)
            breathWarn.enabled = false;

        if (visual != null && act == Act.Fly)
            visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.4f) * 0.08f, 0f);
        else if (visual != null)
            visual.localPosition = Vector3.zero;

        if (hitFlash > 0f)
        {
            hitFlash -= Time.deltaTime;
            view.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), hitFlash * 4f);
        }
        else if (sealedBoss)
            view.color = new Color(0.62f, 0.86f, 1f);
        else if (act == Act.Roar && actTime > 0.52f)
            view.color = new Color(1f, 0.72f, 0.45f);
        else
            view.color = Color.white;

        view.flipX = dir > 0f;
    }

    void Fly()
    {
        wanderTime -= Time.deltaTime;
        if (wanderTime <= 0f || Vector2.Distance(transform.position, wander) < 0.45f)
            PickWander();

        Vector3 next = Vector3.MoveTowards(transform.position, wander, 3.15f * Time.deltaTime);
        Vector3 delta = next - transform.position;
        if (Mathf.Abs(delta.x) > 0.01f)
            dir = Mathf.Sign(delta.x);
        transform.position = next;
        anim.Play(flyFrames, 10f, true, false);
    }

    void PickWander()
    {
        var player = PlayerKnight.Instance;
        bool nearPlayer = player != null && !player.Dead && Random.value < 0.45f;
        if (nearPlayer)
        {
            wander = player.transform.position + new Vector3(Random.Range(-2.4f, 2.4f), Random.Range(1.2f, 3.2f), 0f);
        }
        else
        {
            wander = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        }
        wander.x = Mathf.Clamp(wander.x, minX, maxX);
        wander.y = Mathf.Clamp(wander.y, minY, maxY);
        wanderTime = Random.Range(1.6f, 3.2f);
    }

    void TickMelee()
    {
        if (meleeActive > 0f)
        {
            meleeActive -= Time.deltaTime;
            if (!meleeHit)
                StrikeClaw();
            return;
        }

        meleeTimer -= Time.deltaTime;
        if (meleeTimer > 0f)
            return;

        var player = PlayerKnight.Instance;
        if (player == null || player.Dead || Vector2.Distance(transform.position, player.transform.position) > 3.1f)
            return;

        FacePlayer();
        meleeActive = 0.22f;
        meleeHit = false;
        meleeTimer = Random.Range(1.7f, 2.5f);
    }

    void StrikeClaw()
    {
        var player = PlayerKnight.Instance;
        if (player == null || player.Dead)
            return;
        float forward = dir > 0f ? 1f : -1f;
        float rel = (player.transform.position.x - transform.position.x) * forward;
        if (rel < -0.2f || rel > 2.5f)
            return;
        if (Mathf.Abs(player.transform.position.y - transform.position.y) > 1.6f)
            return;
        meleeHit = true;
        player.TakeDamage(Random.Range(20, 31), transform.position, new Vector2(forward * 4.2f, 3.4f), 0.1f, "claw");
    }

    void TryBeginSkill()
    {
        if (Phase >= 3 && breathTimer <= 0f)
            BeginBreath();
        else if (Phase >= 2 && roarTimer <= 0f)
            BeginRoar();
        else if (dashTimer <= 0f)
            BeginDash();
    }

    void BeginDash()
    {
        act = Act.Dash;
        actTime = 0.48f;
        skillHit = false;
        var player = PlayerKnight.Instance;
        Vector2 aim = player != null && !player.Dead
            ? (Vector2)player.transform.position - (Vector2)transform.position
            : new Vector2(dir, 0f);
        if (aim.sqrMagnitude < 0.05f)
            aim = new Vector2(dir == 0f ? -1f : dir, 0f);
        dir = Mathf.Sign(aim.x == 0f ? dir : aim.x);
        wander = (Vector3)((Vector2)transform.position + aim.normalized * 16f);
        dashTimer = Random.Range(20f, 30f);
        anim.Play(dashFrames, 16f, false, true);
    }

    void TickDash()
    {
        actTime -= Time.deltaTime;
        Vector3 next = Vector3.MoveTowards(transform.position, wander, 15.5f * Time.deltaTime);
        transform.position = next;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minX - 0.4f, maxX + 0.4f),
            Mathf.Clamp(transform.position.y, minY - 0.4f, maxY + 0.4f),
            0f);
        var player = PlayerKnight.Instance;
        if (!skillHit && player != null && !player.Dead && Vector2.Distance(transform.position, player.transform.position) < 2.4f)
        {
            skillHit = true;
            player.TakeDamage(50, transform.position, new Vector2(Mathf.Sign(player.transform.position.x - transform.position.x) * 6.5f, 5.5f), 0.16f, "dash");
        }
        if (actTime <= 0f)
            act = Act.Fly;
    }

    void BeginRoar()
    {
        act = Act.Roar;
        actTime = 0.95f;
        skillHit = false;
        roarTimer = Random.Range(11f, 15f);
        FacePlayer();
        anim.Play(roarFrames, 10f, false, true);
    }

    void TickRoar()
    {
        actTime -= Time.deltaTime;
        if (!skillHit && actTime < 0.52f && actTime > 0.18f)
            StrikeAhead(100, 3.8f, 2.4f, 2.2f, true);
        if (actTime <= 0f)
            act = Act.Fly;
    }

    void BeginBreath()
    {
        act = Act.Breath;
        actTime = 1.25f;
        skillHit = false;
        breathTimer = Random.Range(9f, 13f);
        FacePlayer();
        anim.Play(breathFrames, 8f, false, true);
    }

    void TickBreath()
    {
        float forward = dir > 0f ? 1f : -1f;
        bool warning = actTime > 0.55f;
        if (breathWarn != null)
        {
            breathWarn.enabled = warning;
            breathWarn.flipX = dir > 0f;
            breathWarn.transform.localPosition = new Vector3(forward * 1.9f, 0.15f, 0f);
            breathWarn.transform.localScale = new Vector3(6.2f, 0.42f, 1f);
            breathWarn.color = new Color(0.55f, 0.9f, 1f, 0.28f + 0.22f * Mathf.PingPong(Time.time * 8f, 1f));
        }
        if (!skillHit && actTime < 0.55f && actTime > 0.12f)
            StrikeAhead(300, 5.6f, 1.35f, 2.6f, false);
        actTime -= Time.deltaTime;
        if (actTime <= 0f)
            act = Act.Fly;
    }

    void FacePlayer()
    {
        var player = PlayerKnight.Instance;
        if (player == null || player.Dead)
            return;
        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
            dir = Mathf.Sign(dx);
    }

    void StrikeAhead(int damage, float width, float height, float reach, bool roar)
    {
        var player = PlayerKnight.Instance;
        if (player == null || player.Dead)
            return;
        float forward = dir > 0f ? 1f : -1f;
        float rel = (player.transform.position.x - transform.position.x) * forward;
        if (rel < 0.15f)
            return;
        Vector2 center = (Vector2)transform.position + new Vector2(forward * reach, 0.35f);
        var hit = Physics2D.OverlapBox(center, new Vector2(width, height), 0f, 1 << 7);
        if (hit == null || hit.GetComponent<PlayerKnight>() == null)
            return;
        skillHit = true;
        if (roar)
        {
            float away = Mathf.Sign(player.transform.position.x - transform.position.x);
            if (away == 0f)
                away = forward;
            player.TakeDamage(damage, transform.position, new Vector2(away * 12.5f, 8.2f), 0.55f, "roar");
        }
        else
        {
            player.TakeDamage(damage, transform.position, new Vector2(forward * 8f, 4.5f), 0.35f, "breath");
        }
    }

    public void TakeHit(int damage, int fromFacing)
    {
        if (dead)
            return;
        if (sealedBoss)
        {
            hitFlash = 0.08f;
            return;
        }
        if (act == Act.Roar && actTime > 0.52f)
        {
            act = Act.Fly;
            actTime = 0f;
            stagger = 1.15f;
            skillHit = true;
            GameRoot.Instance?.ShowBanner("포효를 꺾었다", 1.1f);
        }
        Hp -= damage;
        hitFlash = 0.16f;
        if (act == Act.Fly)
            dir = -fromFacing;
        int next = PhaseFor(Hp);
        if (Hp > 0 && next != Phase)
            EnterPhase(next);
        if (Hp > 0)
            return;
        dead = true;
        var box = GetComponent<Collider2D>();
        if (box != null)
            box.enabled = false;
        GameRoot.Instance?.ClearStage();
        Destroy(gameObject, 0.8f);
    }

    public static int PhaseFor(int hp)
    {
        if (hp * 100 < MaxHp * 15)
            return 3;
        if (hp * 100 < MaxHp * 50)
            return 2;
        return 1;
    }

    void EnterPhase(int next)
    {
        Phase = next;
        PatternPending = true;
        sealedBoss = true;
        act = Act.Fly;
        SpawnSeals();
        GameRoot.Instance?.ShowBanner("머리 위의 얼음 봉인을 부숴라", 2.4f);
        if (next == 2)
        {
            roarTimer = 1.4f;
            GameRoot.Instance?.ShowPhaseLine("감히 내게 도전하려드는 것이냐.");
        }
        else if (next == 3)
        {
            breathTimer = 1.6f;
            GameRoot.Instance?.ShowPhaseLine("봐주지 않겠다. 죽어라.");
        }
    }

    public void ResolvePattern()
    {
        PatternPending = false;
        sealedBoss = false;
        string hint = Phase >= 3
            ? "봉인이 풀렸다. 파란 선을 피하라"
            : "봉인이 풀렸다. 주황빛일 때 치면 포효가 끊긴다";
        GameRoot.Instance?.ShowBanner(hint, 2.2f);
    }

    void SpawnSeals()
    {
        IceSeal.Clear();
        float[] xs = { Mathf.Lerp(minX, maxX, 0.22f), Mathf.Lerp(minX, maxX, 0.78f), Mathf.Lerp(minX, maxX, 0.48f) };
        float[] ys = { minY + 0.55f, (minY + maxY) * 0.55f, maxY - 0.55f };
        for (int i = 0; i < 3; i++)
            IceSeal.Spawn(transform.parent, new Vector3(xs[i], ys[i], 0f));
    }
}

public class IceSeal : MonoBehaviour
{
    public const int MaxHp = 40;
    int hp = MaxHp;
    OverheadHp bar;
    static readonly System.Collections.Generic.List<IceSeal> live = new System.Collections.Generic.List<IceSeal>();

    public static void Clear()
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (live[i] != null)
                Destroy(live[i].gameObject);
        }
        live.Clear();
    }

    public static void Spawn(Transform parent, Vector3 point)
    {
        var go = new GameObject("IceSeal");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.position = point;
        var seal = go.AddComponent<IceSeal>();
        live.Add(seal);
    }

    void Awake()
    {
        var body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.85f, 1.15f);
        box.offset = new Vector2(0f, 0.45f);

        var core = new GameObject("Core");
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        core.transform.localScale = new Vector3(0.7f, 0.95f, 1f);
        var view = core.AddComponent<SpriteRenderer>();
        view.sprite = ArtBank.SoftCircle;
        view.color = new Color(0.55f, 0.88f, 1f, 0.92f);
        view.sortingOrder = 17;

        var labelGo = new GameObject("Name");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        var label = labelGo.AddComponent<TextMesh>();
        label.text = "봉인";
        label.fontSize = 48;
        label.characterSize = 0.045f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(0.75f, 0.95f, 1f);
        label.font = GameFont.Get();
        var mesh = labelGo.GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.sortingOrder = 41;
    }

    void Start()
    {
        bar = OverheadHp.Attach(transform, hp, 1.55f);
    }

    public void TakeHit(int damage)
    {
        if (hp <= 0)
            return;
        hp -= damage;
        if (bar != null)
            bar.Set(Mathf.Max(0, hp));
        if (hp > 0)
            return;
        live.Remove(this);
        Destroy(gameObject);
        live.RemoveAll(item => item == null);
        if (live.Count == 0)
            DragonBoss.Active?.ResolvePattern();
    }
}
