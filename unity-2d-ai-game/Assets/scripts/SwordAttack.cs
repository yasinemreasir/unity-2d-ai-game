using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public float damage = 1;
    public Collider2D swordCollider;

    Vector2 rightattackOffset;
    IDamageable owner;

    void Awake()
    {
        // Kılıcın sahibi (Player). Kendi kendimize hasar vermemek için tutuyoruz.
        owner = GetComponentInParent<IDamageable>();
    }

    void Start()
    {
        rightattackOffset = transform.localPosition;
    }

    public void AttackRight()
    {
        swordCollider.enabled = true;
        transform.localPosition = rightattackOffset;
    }

    public void AttackLeft()
    {
        swordCollider.enabled = true;
        transform.localPosition = new Vector3(rightattackOffset.x * -1, rightattackOffset.y);
    }

    public void StopAttack()
    {
        swordCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Somut Enemy sınıfına değil, hasar alabilen her şeye (IDamageable) vuruyoruz.
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null || ReferenceEquals(target, owner) || !target.IsAlive)
        {
            return;
        }

        target.TakeDamage(damage);
    }
}
