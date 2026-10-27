using UnityEngine;

public class OwlEnemy : MonoBehaviour
{
    public int Hp = 80;
    public bool Flyer;
    public bool Boss;

    Rigidbody2D body;
    SpriteRenderer view;
    FrameAnim anim;
    Sprite[] idle, hop, fly, attack, hit, die;
    float dir = 1f;
    float homeX;
    float range = 2.4f;
    float attackCooldown;
    float hitFlash;
    bool dead;
    OverheadHp hpBar;

    public static OwlEnemy Spawn(Vector3 feet, bool flyer, bool boss = false)
    {
        var go = new GameObject(boss ? "BossOwl" : flyer ? "OwlFlyer" : "Owl");
        go.layer = 8;
        go.transform.position = feet;
        var owl = go.AddComponent<OwlEnemy>();
        owl.Flyer = flyer;
        owl.Boss = boss;
        if (!boss)
            owl.Hp = Random.Range(10, 31);
        return owl;
    }

    void Awake()
    {
        body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0f;

        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.85f, 0.85f);
        box.offset = new Vector2(0f, 0.48f);

        view = gameObject.AddComponent<SpriteRenderer>();
        view.sortingOrder = 16;
        anim = gameObject.AddComponent<FrameAnim>();
        anim.Target = view;

        idle = ArtBank.Owls(ArtBank.OwlIdle);
        hop = ArtBank.Owls(ArtBank.OwlHop);
        fly = ArtBank.Owls(ArtBank.OwlFly);
        attack = ArtBank.Owls(ArtBank.OwlAttack);
        hit = ArtBank.Owls(ArtBank.OwlHit);
        die = ArtBank.Owls(ArtBank.OwlDie);
        transform.localScale = Vector3.one * 0.86f;
        homeX = transform.position.x;
    }

    void Start()
    {
        hpBar = OverheadHp.Attach(transform, Hp, 1.42f);
    }

    void Update()
    {
        if (dead)
            return;

        attackCooldown -= Time.deltaTime;
        var player = PlayerKnight.Instance;
        Vector2 self = transform.position;
        bool seesPlayer = player != null && !player.Dead && Vector2.Distance(self, player.transform.position) < (Flyer ? 8f : 6.5f);

        if (Flyer)
            Fly(player, seesPlayer);
        else
            Walk(player, seesPlayer);

        if (hitFlash > 0f)
        {
            hitFlash -= Time.deltaTime;
            view.color = Color.Lerp(Color.white, new Color(0.7f, 0.9f, 1f), hitFlash * 4f);
        }
        else
        {
            view.color = Color.white;
        }

        view.flipX = dir < 0f;
        if (hpBar != null)
            hpBar.Set(Hp);
    }

    void Walk(PlayerKnight player, bool seesPlayer)
    {
        float speed = seesPlayer ? 3.15f : 1.55f;
        if (seesPlayer)
            dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        if (dir == 0f)
            dir = 1f;

        if (!HasGround(dir))
        {
            if (seesPlayer)
                dir = 0f;
            else
                dir *= -1f;
        }

        if (Mathf.Abs(dir) > 0.1f)
        {
            transform.position += Vector3.right * dir * speed * Time.deltaTime;
            anim.Play(seesPlayer ? attack : hop, seesPlayer ? 12f : 10f, true, false);
        }
        else
        {
            anim.Play(idle, 7f, true, false);
        }

        TryHit(player, 1.15f);
    }

    void Fly(PlayerKnight player, bool seesPlayer)
    {
        Vector3 target;
        if (seesPlayer)
        {
            target = player.transform.position + new Vector3(0f, 0.7f, 0f);
            dir = Mathf.Sign(target.x - transform.position.x);
        }
        else
        {
            float x = homeX + Mathf.Sin(Time.time * 0.85f + homeX) * range;
            target = new Vector3(x, transform.position.y + Mathf.Sin(Time.time * 2.2f) * 0.15f, 0f);
            dir = Mathf.Sign(target.x - transform.position.x);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, (seesPlayer ? 3.4f : 1.8f) * Time.deltaTime);
        anim.Play(seesPlayer ? attack : fly, seesPlayer ? 12f : 11f, true, false);
        TryHit(player, 1.05f);
    }

    bool HasGround(float look)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(look * 0.42f, 0.2f);
        return Physics2D.Raycast(origin, Vector2.down, 1.1f, 1 << 6).collider != null;
    }

    void TryHit(PlayerKnight player, float reach)
    {
        if (player == null || player.Dead || attackCooldown > 0f)
            return;
        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance > reach)
            return;

        int damage = Random.Range(30, 51);
        player.TakeDamage(damage, transform.position);
        attackCooldown = 1.05f;
    }

    public void TakeHit(int damage, int fromFacing)
    {
        if (dead)
            return;
        Hp -= damage;
        hitFlash = 0.18f;
        dir = -fromFacing;
        transform.position += Vector3.right * fromFacing * 0.18f;
        if (Hp <= 0)
        {
            dead = true;
            var box = GetComponent<Collider2D>();
            if (box != null)
                box.enabled = false;
            anim.Play(die, 8f, false, true);
            if (hpBar != null)
                hpBar.gameObject.SetActive(false);
            if (Boss)
                GameRoot.Instance?.ClearStage();
            Destroy(gameObject, 0.7f);
        }
        else
        {
            anim.Play(hit, 12f, false, true);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (dead)
            return;
        var player = other.GetComponent<PlayerKnight>();
        if (player != null)
            TryHit(player, 1.4f);
    }
}
