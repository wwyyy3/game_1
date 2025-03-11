using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

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
    #endregion

    public bool IsDead => isDead;


    void Start()
    {
        monsterAgent = GetComponent<NavMeshAgent>();
        lastWanderTime = Time.time;
    }

    void Update()
    {
        if (isDead) return;

        ShooterAgent targetPlayer = GetClosestPlayer();

        if (targetPlayer != null)
        {
            Transform targetTransform = targetPlayer.transform;
            float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

            Vector3 moveDirection = monsterAgent.velocity;
            if (moveDirection.sqrMagnitude > 0.1f)
            {
                transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
            }

            if (distanceToPlayer <= chaseRadius)
            {
                monsterAgent.speed = runningSpeed;
                monsterAgent.SetDestination(targetTransform.position);
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", true);

                if (distanceToPlayer <= attackRange)
                {
                    monsterAgent.isStopped = true;
                    if (canAttack)
                    {
                        StartCoroutine(Attack(targetPlayer));
                    }
                }
                else
                {
                    monsterAgent.isStopped = false;
                }
            }
            else
            {
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
        }
        else
        {
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

        Vector3 horizontalVelocity = new Vector3(monsterAgent.velocity.x, 0, monsterAgent.velocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            HandleStepClimb(horizontalVelocity.normalized);
        }
    }

    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    public void TakeDamage(int damage = 1, Vector3 hitDirection = default)
    {
        if (isDead) return;

        hitCount += damage;
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

    IEnumerator Attack(ShooterAgent target)
    {
        canAttack = false;
        monsterAgent.isStopped = true;
        animator.SetTrigger("isAttacking");

        if (target != null)
        {
            target.TakeDamage(1);
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

    private ShooterAgent GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
        {
            return null;
        }

        ShooterAgent closestAgent = players[0].GetComponent<ShooterAgent>();
        float closestDistance = Vector3.Distance(transform.position, players[0].transform.position);

        for (int i = 1; i < players.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, players[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAgent = players[i].GetComponent<ShooterAgent>();
            }
        }

        return closestAgent;
    }
}
