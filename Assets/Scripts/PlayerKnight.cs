using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKnight : MonoBehaviour
{
    public const int MaxHp = 1000;
    public const int RegenPerSecond = 3;
    public const int PotionHeal = 100;
    public const float PotionInterval = 30f;

    public static PlayerKnight Instance { get; private set; }

    public int Hp { get; private set; } = MaxHp;
    public const float DashCooldownMax = 5f;
    public const float RiseCooldownMax = 7f;
    public float DashCooldown => Mathf.Max(0f, dashCooldown);
    public float PotionCooldown { get; private set; }
    public float RiseCooldown => Mathf.Max(0f, riseCooldown);

    public void RestoreVitals(int hp, float dashRemaining, float potionRemaining, float riseRemaining = 0f)
    {
        Hp = Mathf.Clamp(hp, 1, MaxHp);
        dashCooldown = Mathf.Max(0f, dashRemaining);
        PotionCooldown = Mathf.Max(0f, potionRemaining);
        riseCooldown = Mathf.Max(0f, riseRemaining);
    }
    public bool Dead { get; private set; }
    public bool InputLocked;
    public string DeathReason { get; private set; }
    public int Facing { get; private set; } = 1;

    public event System.Action<int, bool> Damaged;
    public event System.Action<int, bool> Healed;
    public event System.Action Died;

    Rigidbody2D body;
    CapsuleCollider2D capsule;
    SpriteRenderer view;
    FrameAnim anim;
    Sprite[] idle, run, jump, land, attack, hit, die;

    float invuln;
    float attackTime = -1f;
    float attackLength;
    int attackDamage;
    float dashTime;
    float dashCooldown;
    float riseTime;
    float riseCooldown;
    int riseDamage;
    bool riseSwung;
    float landTime;
    float dropTimer;
    float regenCarry;
    bool grounded;
    bool wasGrounded;
    float jumpBuffer;
    float coyote;
    float stun;
    readonly HashSet<OwlEnemy> swingHits = new HashSet<OwlEnemy>();
    readonly HashSet<EmberPickup> emberHits = new HashSet<EmberPickup>();
    readonly HashSet<DragonBoss> dragonHits = new HashSet<DragonBoss>();
    readonly HashSet<IceSeal> sealHits = new HashSet<IceSeal>();
    string hurtKey;
    bool artFacesRight;

    const float MoveSpeed = 7.1f;
    const float JumpSpeed = 15.4f;
    const float DashSpeed = 13.5f;

    public static PlayerKnight Spawn(Vector3 feet)
    {
        var go = new GameObject("Player");
        go.layer = 7;
        go.transform.position = feet;
        return go.AddComponent<PlayerKnight>();
    }

    void Awake()
    {
        Instance = this;
        body = gameObject.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.gravityScale = 1f;

        capsule = gameObject.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.4f, 1.2f);
        capsule.offset = new Vector2(0f, 0.62f);
        capsule.direction = CapsuleDirection2D.Vertical;

        view = gameObject.AddComponent<SpriteRenderer>();
        view.sortingOrder = 20;
        anim = gameObject.AddComponent<FrameAnim>();
        anim.Target = view;

        var shadowGo = new GameObject("Shadow");
        shadowGo.transform.SetParent(transform, false);
        shadowGo.transform.localPosition = new Vector3(0.05f, 0.02f, 0f);
        shadowGo.transform.localScale = new Vector3(1.05f, 0.28f, 1f);
        var shadow = shadowGo.AddComponent<SpriteRenderer>();
        shadow.sprite = ArtBank.SoftCircle;
        shadow.color = new Color(0f, 0f, 0f, 0.45f);
        shadow.sortingOrder = 8;

        idle = ArtBank.Knights(ArtBank.KnightIdle);
        run = ArtBank.Knights(ArtBank.KnightRun);
        jump = ArtBank.Knights(ArtBank.KnightJump);
        land = ArtBank.Knights(ArtBank.KnightLand);
        attack = ArtBank.Knights(ArtBank.KnightAttack);
        hit = ArtBank.Knights(ArtBank.KnightHit);
        die = ArtBank.Knights(ArtBank.KnightDie);
        anim.Play(idle, 7f, true, true);
        Facing = 1;
        view.flipX = true;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || Dead)
        {
            TickRecovery();
            return;
        }

        if (InputLocked || Time.timeScale == 0f)
            return;

        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        bool down = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpBuffer = 0.12f;
        if (keyboard.spaceKey.wasReleasedThisFrame && body.linearVelocity.y > 0f && dashTime <= 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.45f);
        if (keyboard.zKey.wasPressedThisFrame)
            BeginAttack();
        if (keyboard.cKey.wasPressedThisFrame)
            UsePotion();
        if (keyboard.xKey.wasPressedThisFrame)
            BeginDash();
        if (keyboard.vKey.wasPressedThisFrame)
            BeginRise();

        float axis = 0f;
        if (left) axis -= 1f;
        if (right) axis += 1f;
        if (Mathf.Abs(axis) > 0.1f && stun <= 0f && dashTime <= 0f)
            Facing = axis > 0f ? 1 : -1;

        Move(axis, down);
        TickCombat();
        TickRecovery();
        UpdateAnim();
        view.flipX = artFacesRight ? Facing < 0 : Facing > 0;
    }

    void Move(float axis, bool down)
    {
        grounded = CheckGround();
        if (grounded)
            coyote = 0.12f;
        else
            coyote -= Time.deltaTime;

        if (jumpBuffer > 0f)
            jumpBuffer -= Time.deltaTime;

        var velocity = body.linearVelocity;
        if (dashTime > 0f)
        {
            dashTime -= Time.deltaTime;
            velocity.x = Facing * DashSpeed;
            velocity.y = Mathf.Max(velocity.y, 0.4f);
        }
        else if (stun <= 0f)
        {
            float target = axis * MoveSpeed;
            if (attackTime > 0f)
                target *= 0.35f;
            float air = grounded ? 1f : 0.72f;
            velocity.x = Mathf.MoveTowards(velocity.x, target, MoveSpeed * 8f * air * Time.deltaTime);
        }

        if (jumpBuffer > 0f && coyote > 0f && stun <= 0f && attackTime <= 0f)
        {
            if (down && grounded)
            {
                dropTimer = 0.28f;
                jumpBuffer = 0f;
            }
            else
            {
                velocity.y = JumpSpeed;
                jumpBuffer = 0f;
                coyote = 0f;
                grounded = false;
                AudioFx.Play("jump");
            }
        }

        if (!wasGrounded && grounded && velocity.y <= 0.1f)
        {
            landTime = 0.18f;
            AudioFx.Play("land");
        }
        wasGrounded = grounded;

        if (dropTimer > 0f)
        {
            dropTimer -= Time.deltaTime;
            IgnoreGround(true);
        }
        else
        {
            IgnoreGround(false);
        }

        body.linearVelocity = velocity;
        stun -= Time.deltaTime;

        if (StageBuilder.FallWrap)
        {
            if (transform.position.y < StageBuilder.WrapMinY)
            {
                float x = Mathf.Clamp(transform.position.x, StageBuilder.WrapMinX, StageBuilder.WrapMaxX);
                transform.position = new Vector3(x, StageBuilder.WrapMaxY, transform.position.z);
                body.linearVelocity = new Vector2(body.linearVelocity.x, -3.2f);
            }
        }
        else if (transform.position.y < -7.5f)
            Kill("낭떠러지로 떨어졌다");
    }

    bool CheckGround()
    {
        var origin = (Vector2)transform.position + Vector2.up * 0.08f;
        var hit = Physics2D.BoxCast(origin, new Vector2(0.32f, 0.08f), 0f, Vector2.down, 0.16f, 1 << 6);
        return hit.collider != null;
    }

    void IgnoreGround(bool ignore)
    {
        var hits = Physics2D.OverlapBoxAll((Vector2)transform.position + Vector2.up * 0.4f, new Vector2(0.7f, 1.4f), 0f, 1 << 6);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponent<SolidFloor>() != null)
                continue;
            Physics2D.IgnoreCollision(capsule, hits[i], ignore);
        }
    }

    void BeginAttack()
    {
        if (attackTime > 0f || stun > 0f)
            return;
        attackTime = 0.34f;
        attackLength = attackTime;
        attackDamage = Random.Range(5, 11);
        artFacesRight = true;
        swingHits.Clear();
        emberHits.Clear();
        dragonHits.Clear();
        sealHits.Clear();
        anim.Play(attack, 16f, false, true);
        AudioFx.Play("attack");
    }

    void UsePotion()
    {
        if (Dead || PotionCooldown > 0f || Hp >= MaxHp)
            return;
        PotionCooldown = PotionInterval;
        Heal(PotionHeal, true);
    }

    void BeginDash()
    {
        if (dashCooldown > 0f || Dead || stun > 0f)
            return;
        dashTime = 0.16f;
        dashCooldown = DashCooldownMax;
        AudioFx.Play("jump");
    }

    void TickCombat()
    {
        dashCooldown -= Time.deltaTime;
        riseCooldown -= Time.deltaTime;
        invuln -= Time.deltaTime;

        if (riseTime > 0f)
        {
            riseTime -= Time.deltaTime;
            if (!riseSwung && riseTime < 0.24f)
            {
                riseSwung = true;
                SwingBox((Vector2)transform.position + new Vector2(Facing * 0.55f, 1.65f), new Vector2(1.7f, 1.75f), riseDamage);
            }
        }

        if (attackTime > 0f)
        {
            attackTime -= Time.deltaTime;
            float through = 1f - Mathf.Clamp01(attackTime / attackLength);
            if (through > 0.28f && through < 0.82f)
                Swing();
        }

        if (invuln > 0f)
        {
            float blink = Mathf.PingPong(Time.time * 16f, 1f);
            view.color = new Color(1f, 0.75f, 0.75f, 0.45f + blink * 0.55f);
        }
        else
        {
            view.color = Color.white;
        }
    }

    void BeginRise()
    {
        if (riseCooldown > 0f || riseTime > 0f || stun > 0f || Dead)
            return;
        riseTime = 0.32f;
        riseCooldown = RiseCooldownMax;
        riseDamage = Random.Range(12, 19);
        riseSwung = false;
        swingHits.Clear();
        emberHits.Clear();
        dragonHits.Clear();
        sealHits.Clear();
        artFacesRight = true;
        body.linearVelocity = new Vector2(Facing * 2.4f, 9.2f);
        anim.Play(attack, 18f, false, true);
        AudioFx.Play("attack");
    }

    void Swing()
    {
        SwingBox((Vector2)transform.position + new Vector2(Facing * 0.95f, 0.82f), new Vector2(1.5f, 1.1f), attackDamage);
    }

    void SwingBox(Vector2 center, Vector2 size, int damage)
    {
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, 1 << 8);
        for (int i = 0; i < hits.Length; i++)
        {
            var owl = hits[i].GetComponent<OwlEnemy>();
            if (owl != null)
            {
                if (!swingHits.Add(owl))
                    continue;
                owl.TakeHit(damage, Facing);
                continue;
            }

            var ember = hits[i].GetComponent<EmberPickup>();
            if (ember != null && emberHits.Add(ember))
                ember.TakeHit(damage);

            var seal = hits[i].GetComponent<IceSeal>();
            if (seal != null && sealHits.Add(seal))
                seal.TakeHit(damage);

            var dragon = hits[i].GetComponent<DragonBoss>();
            if (dragon != null && dragonHits.Add(dragon))
                dragon.TakeHit(damage, Facing);
        }
    }

    void TickRecovery()
    {
        if (Dead)
            return;

        regenCarry += Time.deltaTime;
        if (regenCarry >= 1f)
        {
            regenCarry -= 1f;
            Heal(RegenPerSecond, false);
        }

        if (PotionCooldown > 0f)
            PotionCooldown -= Time.deltaTime;
    }

    public void Heal(int amount, bool potion)
    {
        if (Dead || amount <= 0 || Hp >= MaxHp)
            return;
        int before = Hp;
        Hp = Mathf.Min(MaxHp, Hp + amount);
        int gained = Hp - before;
        if (gained <= 0)
            return;
        if (potion)
            AudioFx.Play("heal");
        Healed?.Invoke(gained, potion);
    }

    public void TakeDamage(int amount, Vector2 source)
    {
        float dir = transform.position.x >= source.x ? 1f : -1f;
        TakeDamage(amount, source, new Vector2(dir * 5.5f, 7.2f), 0.16f);
    }

    public void TakeDamage(int amount, Vector2 source, Vector2 launch, float launchStun, string key = null)
    {
        if (Dead || amount <= 0)
            return;
        if (invuln > 0f && (key == null || key == hurtKey))
            return;

        Hp -= amount;
        hurtKey = key;
        invuln = key == null ? 0.75f : 0.28f;
        stun = launchStun;
        attackTime = 0f;
        artFacesRight = false;
        body.linearVelocity = launch;
        anim.Play(hit, 12f, false, true);
        AudioFx.Play("hurt");
        Damaged?.Invoke(amount, false);
        if (Hp <= 0)
            Kill("체력이 바닥났다");
    }

    public void Kill(string reason)
    {
        if (Dead)
            return;
        Dead = true;
        DeathReason = reason;
        Hp = 0;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = reason.Contains("낭떠러지") ? 1f : 0f;
        artFacesRight = false;
        anim.Play(die, 8f, false, true);
        AudioFx.Play("die");
        Died?.Invoke();
    }

    void UpdateAnim()
    {
        if (attackTime > 0f || stun > 0f || riseTime > 0f)
            return;

        if (!grounded)
        {
            artFacesRight = true;
            anim.Play(jump, 12f, false, false);
            return;
        }

        if (landTime > 0f)
        {
            landTime -= Time.deltaTime;
            artFacesRight = false;
            anim.Play(land, 16f, false, false);
            return;
        }

        if (Mathf.Abs(body.linearVelocity.x) > 0.4f)
        {
            artFacesRight = true;
            anim.Play(run, 14f, true, false);
        }
        else
        {
            artFacesRight = false;
            anim.Play(idle, 7f, true, false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
