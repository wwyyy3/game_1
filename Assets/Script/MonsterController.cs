using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics.Internal;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    [Header("Health")]
    public int maxHits = 1;
    public int hitCount;

    [Header("Navigation Settings")]
    public NavMeshAgent agent;
    public float wanderRadius = 10f;
    public float wanderInterval = 5f;
    public float chaseRadius = 15f;

    [Header("Monster Animations")]
    public Animator animator;

    [Header("Monster Movement")]
    public float walkingSpeed = 2f;
    public float runningSpeed = 4f;
    private Transform player;

    [Header("Attack Settings")]
    public float attackRange = 1.5f; 
    public float attackCooldown = 2.16f;
    private bool canAttack = true;

    private bool isDead = false;
    private float lastWanderTime;
    public ShooterAgent Agent;

    [Header("Step Climbing Settings")]
    [Tooltip("Maximum step height allowed for climbing")]
    public float stepHeight = 0.5f;
    [Tooltip("Forward distance to check for steps")]
    public float stepCheckDistance = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastWanderTime = Time.time;
    }

    void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        //  Make the monster face the direction of movement
        Vector3 moveDirection = agent.velocity;
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
        }

        // Check if the player is within the chase range
        if (distanceToPlayer <= chaseRadius)
        {
            agent.speed = runningSpeed;
            agent.SetDestination(player.position);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", true);

            // If within attack range
            if (distanceToPlayer <= attackRange)
            {
                agent.isStopped = true;
                if (canAttack)
                {
                    StartCoroutine(Attack());
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }
        else
        {
            // Random wandering mode
            agent.speed = walkingSpeed;
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);

            if (Time.time - lastWanderTime >= wanderInterval && agent.remainingDistance < 0.5f)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(newPos);
                lastWanderTime = Time.time;
            }
        }

        //  Step climbing
        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0, agent.velocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            HandleStepClimb(horizontalVelocity.normalized);
        }
    }

    /// <summary>
    /// Generates a random navigation point within the specified radius.
    /// </summary>
    /// <param name="origin">The center position</param>
    /// <param name="dist">Search radius</param>
    /// <param name="layermask">Navigation layer mask</param>
    /// <returns>A valid position on the navigation mesh</returns>
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
        Vector3 knockback = hitDirection.normalized * 2f;
        transform.position += new Vector3(knockback.x, 0, knockback.z);
        Debug.Log("Monster hit " + hitCount + " times.");

        animator.SetTrigger("getHit");

        if (hitCount >= maxHits)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        Debug.Log("Monster Die");
        isDead = true;

        animator.SetBool("die", true);
        agent.isStopped = true;

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
        agent.isStopped = true;
        animator.SetTrigger("isAttacking");

        if (player != null)
        {
            ShooterAgent agent = player.GetComponent<ShooterAgent>();
            if (agent != null)
            {
                agent.TakeDamage(1);
                Debug.Log("Player took damage from monster!");
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        Debug.Log("Monster is attacking the player!");
        canAttack = true;
    }

    // Check if there is a climbable step in front;
    // if conditions are met, move the monster upward
    private void HandleStepClimb(Vector3 moveDirection)
    {
        // Cast a ray from slightly above the monster's base to detect low obstacles
        Vector3 lowerOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(lowerOrigin, moveDirection, out RaycastHit lowerHit, stepCheckDistance))
        {
            float obstacleHeight = lowerHit.point.y;
            // If the difference between the obstacle height and the monster's current position
            // is within the allowed range
            if (obstacleHeight - transform.position.y <= stepHeight)
            {
                // Cast a ray from a higher position to check if the area above is clear
                Vector3 upperOrigin = transform.position + Vector3.up * (stepHeight + 0.1f);
                if (!Physics.Raycast(upperOrigin, moveDirection, out RaycastHit upperHit, stepCheckDistance))
                {
                    // Calculate the amount to move upward and adjust the monster's position accordingly
                    float stepUpAmount = (obstacleHeight + 0.05f) - transform.position.y;
                    transform.position += new Vector3(0, stepUpAmount, 0);
                }
            }
        }
    }
}
