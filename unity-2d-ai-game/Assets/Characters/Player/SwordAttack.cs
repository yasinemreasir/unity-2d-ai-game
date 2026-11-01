using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public float damage = 1;
    public Collider2D swordCollider;

    Vector2 rightattackOffset;

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
        // Tag'e bakmadan, çarpılan objede ya da üst objelerinde Enemy scripti arıyoruz.
        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null)
        {
            return;
        }

        enemy.Health -= damage;
        
    }

}
