using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Can")]
    public float health = 3;

    [Header("Takip")]
    public float moveSpeed = 0.05f;
    public float detectionRange = 2f;    // Oyuncu bu mesafeye girince takip başlar
    public float loseTargetRange = 6f;   // Oyuncu bu mesafeden fazla uzaklaşırsa takip biter
    public bool spriteFacesRight = true; // Slime sprite'ı varsayılan olarak sağa mı bakıyor?

    [Header("Saldırı (temas)")]
    public float attackDamage = 1f;      // Slime'ın collider'ı oyuncuya değince verilen hasar
    public float attackCooldown = 1.2f;  // Temas devam ederken iki hasar arası bekleme

    [Header("Hasar Alma")]
    public float hitStunTime = 0.3f;     // Vurulunca kaç sn duraksar (bu sürede hasar da veremez)

    Animator animator;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Transform player;
    PlayerController playerController;

    // Animator'da olmayan parametreler için hata/uyarı basmamak için
    readonly HashSet<string> animatorParams = new HashSet<string>();

    bool isAlive = true;
    bool isChasing;
    float nextAttackTime;
    float stunUntil;

    // Düşman öldüğünde bir kez tetiklenir (örn. boss ölünce oyunu bitirmek için dinlenir).
    public event System.Action<Enemy> Died;

    // IDamageable
    public bool IsAlive
    {
        get { return isAlive; }
    }

    // IDamageable
    public void TakeDamage(float amount)
    {
        Health -= amount;
    }

    public float Health
    {
        set
        {
            if (!isAlive) return; // Ölü slime tekrar hasar almasın

            if (value < health)
            {
                SetTrigger("hit");
                stunUntil = Time.time + hitStunTime;
            }

            health = value;

            if (health <= 0)
            {
                isAlive = false;
                SetBool("isMoving", false);
                SetBool("isAlive", false);

                if (Died != null)
                {
                    Died(this);
                }
            }
        }
        get
        {
            return health;
        }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            animatorParams.Add(p.name);
        }
    }

    void Start()
    {
        SetBool("isAlive", isAlive);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        if (player == null || playerController == null)
        {
            Debug.LogWarning("Enemy: 'Player' tag'li ve PlayerController scripti olan obje bulunamadı. Slime takip edemez.");
        }
    }

    void FixedUpdate()
    {
        if (!isAlive || player == null || playerController == null) return;

        // Oyuncu öldüyse ya da slime vurulup duraksadıysa hareket etme
        if (!playerController.IsAlive || Time.time < stunUntil)
        {
            SetBool("isMoving", false);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        // Takip başlat / bırak (iki ayrı mesafe olduğu için sınırda titremez)
        if (!isChasing && distance <= detectionRange)
        {
            isChasing = true;
        }
        else if (isChasing && distance > loseTargetRange)
        {
            isChasing = false;
        }

        if (!isChasing)
        {
            SetBool("isMoving", false);
            return;
        }

        FaceTarget(toPlayer.x);

        // Oyuncuya doğru yürü; hasar, collider temasıyla verilir
        rb.MovePosition(rb.position + toPlayer.normalized * moveSpeed * Time.fixedDeltaTime);
        SetBool("isMoving", true);
    }

    // Slime'ın kendi Collider2D'si oyuncunun Collider2D'sine değince hasar verir
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision);
    }

    void TryDamagePlayer(Collision2D collision)
    {
        if (!isAlive || playerController == null || !playerController.IsAlive) return;
        if (Time.time < stunUntil || Time.time < nextAttackTime) return;
        if (!collision.collider.CompareTag("Player")) return;

        nextAttackTime = Time.time + attackCooldown;
        SetTrigger("attack");
        playerController.TakeDamage(attackDamage);
    }

    void FaceTarget(float directionX)
    {
        if (spriteRenderer == null || Mathf.Abs(directionX) < 0.01f) return;

        bool targetIsLeft = directionX < 0;
        spriteRenderer.flipX = spriteFacesRight ? targetIsLeft : !targetIsLeft;
    }

    void SetTrigger(string paramName)
    {
        if (animatorParams.Contains(paramName)) animator.SetTrigger(paramName);
    }

    void SetBool(string paramName, bool value)
    {
        if (animatorParams.Contains(paramName)) animator.SetBool(paramName, value);
    }

    void OnHit(float damage)
    {
        Health -= damage;
    }

    // Ölüm animasyonundan Animation Event ile çağrılıyor
    public void Defeated()
    {
        SetTrigger("defeated");
    }

    public void RemoveEnemy()
    {
        Destroy(gameObject);
    }

    // Slime seçiliyken Scene ekranında algılama menzilini gösterir (sarı)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
