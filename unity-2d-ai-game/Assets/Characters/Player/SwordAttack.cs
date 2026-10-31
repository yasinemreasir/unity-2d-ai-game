using UnityEngine;

public class SwordAttack : MonoBehaviour
{
   


    public float damage = 1;
    Vector2 rightattackOffset;

    public Collider2D swordCollider;


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

    


    void Start()
    {
        rightattackOffset = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Health -= damage;
            }
        }
    }
}
