using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
    public float moveSpeed = 1f;

    public float collisionOffset = 0.05f;

    public ContactFilter2D movementFilter;
    public SwordAttack swordAttack;

    [Header("Can")]
    public float maxHealth = 100f;
    public HealthBar healthBar;
    public float invincibleTime = 0.5f;   // Hasar aldıktan sonra kaç sn dokunulmaz
    public Color hitColor = Color.red;    // Hasar alınca kısa süre bu renge boyanır

    Vector2 movementInput;

    SpriteRenderer spriteRenderer;
    Rigidbody2D rb;

    Animator animator;

    Color originalColor;

    // Animator'da olmayan parametreler için hata/uyarı basmamak için
    readonly HashSet<string> animatorParams = new HashSet<string>();

    List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();

    bool canMove = true;
    bool isAlive = true;
    bool inputLocked;
    float health;
    float invincibleUntil;

    // Oyuncu öldüğünde bir kez tetiklenir (Game Over ekranı bunu dinleyecek).
    public event System.Action Died;

    // IDamageable
    public bool IsAlive
    {
        get { return isAlive; }
    }

    public float Health
    {
        get { return health; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            animatorParams.Add(p.name);
        }

        health = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

        // Bool parametrelerin varsayılanı false olduğu için, ölüm geçişi
        // oyun başlar başlamaz tetiklenmesin diye açıkça true yapıyoruz
        SetBool("isAlive", true);
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            // If movement input is not 8, try to move
            if (movementInput != Vector2.zero)
            {
                bool success = TryMove(movementInput);
                if (!success)
                {
                    success = TryMove(new Vector2(movementInput.x, 0));

                    if (!success)
                    {
                        success = TryMove(new Vector2(0, movementInput.y));
                    }
                }
                animator.SetBool("isMoving", success);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
            //set direction of sprite to movement direction
            if (movementInput.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (movementInput.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    private bool TryMove(Vector2 direction)
    {
        // Check for potential collisions
        int count = rb.Cast(
        direction, // X and Y values between -1 and 1 that represent the direction from the body to look for collisions
        movementFilter, // The settings that determine where a collision can occur on such as Layers to collide with
        castCollisions, // List of collisions to store the found collisions into after the Cast is finished
        moveSpeed * Time.fixedDeltaTime + collisionOffset); // The amount to cast equal to the movement plus an offset

        if (count == 0)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            return true;
        }
        else
        {
            return false;
        }
    }

    void OnMove(InputValue movementValue)
    {
        // Diyalog açıkken (inputLocked) WASD ile yazı yazarken karakter yürümesin
        movementInput = inputLocked ? Vector2.zero : movementValue.Get<Vector2>();
    }

    void OnFire()
    {
        if (!isAlive || inputLocked) return;
        animator.SetTrigger("swordAttack");
    }

    public void SwordAttack()
    {
        if (!isAlive) return;

        LockMovement();
        if (spriteRenderer.flipX)
        {
            swordAttack.AttackLeft();
        }
        else
        {
            swordAttack.AttackRight();
        }
    }

    public void EndSwordAttack()
    {
        UnlockMovement();
        swordAttack.StopAttack();
    }

    public void LockMovement()
    {
        canMove = false;
    }

    public void UnlockMovement()
    {
        // Ölüyken hareket tekrar açılmasın
        if (isAlive)
        {
            canMove = true;
        }
    }

    // Diyalog paneli gibi arayüzler açılırken oyuncu girdisini (hareket + saldırı) kapatır/açar.
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            movementInput = Vector2.zero;
            SetBool("isMoving", false);
        }
    }

    // IDamageable - Slime (veya başka bir düşman) tarafından çağrılır
    public void TakeDamage(float damage)
    {
        if (!isAlive || Time.time < invincibleUntil) return;

        invincibleUntil = Time.time + invincibleTime;
        health -= damage;

        if (health < 0)
        {
            health = 0;
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }

        if (health <= 0)
        {
            Die();
        }
        else
        {
            SetTrigger("hit");
            StartCoroutine(HitFlash());
        }
    }

    void Die()
    {
        isAlive = false;
        canMove = false;
        movementInput = Vector2.zero;

        if (swordAttack != null)
        {
            swordAttack.StopAttack();
        }

        SetBool("isMoving", false);
        SetBool("isAlive", false); // Animator bu parametreyle player_death animasyonuna geçer

        Debug.Log("Oyuncu öldü!");

        if (Died != null)
        {
            Died();
        }
    }

    IEnumerator HitFlash()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    void SetTrigger(string paramName)
    {
        if (animatorParams.Contains(paramName)) animator.SetTrigger(paramName);
    }

    void SetBool(string paramName, bool value)
    {
        if (animatorParams.Contains(paramName)) animator.SetBool(paramName, value);
    }
}
