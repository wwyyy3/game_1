using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics.Internal;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    #region FIELDS SERIALIZED
    [Header("Health")]
    public int maxHits = 1;
    public int hitCount;

    [Header("Navigation Settings")]
    public NavMeshAgent monsterAgent;
    public float wanderRadius = 10f;
    public float wanderInterval = 5f;
    public float chaseRadius = 15f;

    [Header("Monster Animations")]
    public Animator animator;

    [Header("Monster Movement")]
    public float walkingSpeed = 4f;
    public float runningSpeed = 6f;
    private Transform player;

    [Header("Attack Settings")]
    public float attackRange = 1.5f; 
    public float attackCooldown = 2.16f;
    private bool canAttack = true;

    [Header("Step Climbing Settings")]
    [Tooltip("Maximum step height allowed for climbing")]
    public float stepHeight = 0.5f;
    [Tooltip("Forward distance to check for steps")]
    public float stepCheckDistance = 0.5f;
    #endregion

    #region Internal State
    private bool isDead = false;
    private float lastWanderTime;
    private ShooterAgent playerAgent;
    #endregion

    void Start()
    {
        monsterAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastWanderTime = Time.time;
        playerAgent = player.GetComponent<ShooterAgent>();
    }

    void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        //  Make the monster face the direction of movement
        Vector3 moveDirection = monsterAgent.velocity;
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
        }

        // Check if the player is within the chase range
        if (distanceToPlayer <= chaseRadius)
        {
            monsterAgent.speed = runningSpeed;
            monsterAgent.SetDestination(player.position);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", true);

            // If within attack range
            if (distanceToPlayer <= attackRange)
            {
                monsterAgent.isStopped = true;
                if (canAttack)
                {
                    StartCoroutine(Attack());
                }
            }
            else
            {
                monsterAgent.isStopped = false;
            }
        }
        else
        {
            // Random wandering mode
            monsterAgent.speed = walkingSpeed;
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", monsterAgent.velocity.magnitude > 0.1f);

            if (Time.time - lastWanderTime >= wanderInterval && monsterAgent.remainingDistance < 0.5f)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                monsterAgent.SetDestination(newPos);
                lastWanderTime = Time.time;
            }
        }

        //  Step climbing
        Vector3 horizontalVelocity = new Vector3(monsterAgent.velocity.x, 0, monsterAgent.velocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            HandleStepClimb(horizontalVelocity.normalized);
        }
    }

    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // Generate a random direction
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        // Find the nearest valid point on the navigation mesh
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    //public void GetShot(int damage, ShooterAgent shooter)
    //{
    //    ApplyDamage(damage, shooter);
    //}

    //private void ApplyDamage(int damage, ShooterAgent shooter)
    //{
    //    CurrentHealth -= damage;

    //    if (CurrentHealth <= 0)
    //    {
    //        Die(shooter);
    //    }
    //}

    public void TakeDamage(int damage = 1, Vector3 hitDirection = default)
    {
        if (isDead) return;

        hitCount += damage;
        playerAgent.AddReward(20f);
        Vector3 knockback = hitDirection.normalized * 2f;
        transform.position += new Vector3(knockback.x, 0, knockback.z);
        animator.SetTrigger("getHit");

        if (hitCount >= maxHits)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetBool("die", true);
        monsterAgent.isStopped = true;

        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    IEnumerator Attack()
    {
        canAttack = false;
        monsterAgent.isStopped = true;
        animator.SetTrigger("isAttacking");

        if (player != null)
        {
            if (playerAgent != null)
            {
                playerAgent.TakeDamage(1);
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        monsterAgent.isStopped = false;
        animator.SetBool("isWalking", true);
        canAttack = true;
    }

    private void HandleStepClimb(Vector3 moveDirection)
    {
        Vector3 lowerOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(lowerOrigin, moveDirection, out RaycastHit lowerHit, stepCheckDistance))
        {
            float obstacleHeight = lowerHit.point.y;

            if (obstacleHeight - transform.position.y <= stepHeight)
            {
                Vector3 upperOrigin = transform.position + Vector3.up * (stepHeight + 0.1f);
                if (!Physics.Raycast(upperOrigin, moveDirection, out RaycastHit upperHit, stepCheckDistance))
                {
                    float stepUpAmount = (obstacleHeight + 0.05f) - transform.position.y;
                    transform.position += new Vector3(0, stepUpAmount, 0);
                }
            }
        }
    }
}
