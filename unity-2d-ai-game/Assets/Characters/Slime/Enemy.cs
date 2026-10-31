using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{

    Animator animator;

    bool isAlive = true;


    public float Health
    {
        set
        {
         
            if (value < health)
            {
                animator.SetTrigger("hit");
            }
            health = value;
            if (health <= 0)
            {
                animator.SetBool("isAlive", false);
            }
        }
        get
        {
            return health;
        }
    }

    public float health = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetBool("isAlive", isAlive);
    }

    void OnHit(float damage)
    {
        Health -= damage;
    }

    public void Defeated()
    {
        animator.SetTrigger("defeated");
    }
    public void RemoveEnemy()
    {
        Destroy(gameObject);
    }



        // Update is called once per frame
        void Update()
        {

        }
    }

