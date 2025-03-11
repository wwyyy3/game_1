using InfimaGames.LowPolyShooterPack;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum AgentActionType { GoalReached, MonsterKilled }

public class ShooterAgent : Agent
{
    #region Configuration
    [Header("Core Settings")]
    [SerializeField] private GameObject goal;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Camera agentCamera;

    [Header("Training Phase")]
    public bool pathfindingOnlyPhase = true;

    #endregion

    #region Internal State
    private Character character;
    private Rigidbody rb;
    private int maxHealth = 10;
    private int currentHealth;
    private float speedWalking = 7f;
    private List<MonsterController> monsters = new List<MonsterController>();
    private static readonly object spawnLock = new object();
    private float previousDistanceToGoal;
    #endregion

    #region Initialization
    public override void Initialize()
    {
        character = GetComponent<Character>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
    }
    #endregion

    #region Episode Handling
    public override void OnEpisodeBegin()
    {
        ResetAgent();
        previousDistanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
        StartCoroutine(DelayedSpawn());
        Debug.Log("episodeBegin");
    }

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForEndOfFrame();  
        SpawnObjects();                       
    }

    #endregion

    #region Observation System
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(goal.transform.localPosition);
        sensor.AddObservation(currentHealth / (float)maxHealth);
    }
    #endregion

    #region Action System
    public override void OnActionReceived(ActionBuffers actions)
    {
        HandleMovement(actions);
        HandleRotation(actions);
        HandleShooting(actions);
        ApplyBehaviorPenalty();
        CheckBoundary();
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        var movement = transform.forward * moveZ + transform.right * moveX;
        movement *= speedWalking;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void HandleRotation(ActionBuffers actions)
    {
        Vector2 lookInput = new Vector2(
            actions.ContinuousActions[2],
            actions.ContinuousActions[3]
        );
        cameraLook.pendingLookInput = lookInput;
    }

    private void HandleShooting(ActionBuffers actions)
    {
        Debug.DrawRay(shootingPoint.position, transform.forward * 30f, Color.green, 2f);
        if (!pathfindingOnlyPhase && actions.DiscreteActions[0] == 1)
        {
            var layerMask = 1 << LayerMask.NameToLayer("Enemy");
            if (Physics.Raycast(shootingPoint.position, transform.forward, out RaycastHit hit, 20f, layerMask))
            {
                var monster = hit.collider.GetComponent<MonsterController>();
                if (monster != null)
                {
                    character.AgentFire();
                    AddReward(1f);

                    if (monster.IsDead && monster.hitCount >= monster.maxHits)
                    {
                        AddReward(10f);
                        Debug.Log("I killed the monster");
                    }
                }
            }
            else
            {
                AddReward(-0.1f);
            }
        }
    }

    private void ApplyBehaviorPenalty()
    {
        float currentDistanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
        float distanceDiff = previousDistanceToGoal - currentDistanceToGoal;

        AddReward(distanceDiff * 1f);
        previousDistanceToGoal = currentDistanceToGoal;

        AddReward(-0.001f);
    }
    #endregion

    #region Environment Interaction

    private void CheckBoundary()
    {       
        var wallLayer = 1 << LayerMask.NameToLayer("Wall"); 
        if (Physics.CheckSphere(transform.localPosition, 0.1f, wallLayer))
        {
            AddReward(-5f);
            Debug.Log("Collided with wall");
            EndEpisode();
        }
    }
    #endregion

    #region Spawn System
    private void SpawnObjects()
    {
        lock (spawnLock)
        {
            gameManager.SpawnMonsters(transform.parent.localPosition);
            monsters.AddRange(UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
            
        }
    }
    #endregion

    #region Health System
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        AddReward(-8f);

        if (currentHealth <= 0)
        {
            AddReward(-100f);
            EndEpisode();
        }
    }
    #endregion

    #region Reset System
    private void ResetAgent()
    {
        currentHealth = maxHealth;
        rb.linearVelocity = Vector3.zero;
        transform.localPosition = new Vector3(50.869f, -8.451f, 27.96287f);
        transform.localRotation = Quaternion.Euler(0f, -174.29f, 0f);
        Physics.SyncTransforms();
    }
    #endregion

    #region Goal System
    private void GoalReached()
    {
        AddReward(100f);
       // NotifyTeamAction(AgentActionType.GoalReached);
        EndEpisode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
            GoalReached();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f);
            AddReward(-0.05f);
        }
        if (collision.gameObject.CompareTag("Building"))
        {
            AddReward(-0.05f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f * Time.fixedDeltaTime);
        }
        if (collision.gameObject.CompareTag("Building"))
        {
            AddReward(-0.01f * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region Debug
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        continuous[0] = Input.GetAxisRaw("Horizontal");
        continuous[1] = Input.GetAxisRaw("Vertical");
        continuous[2] = Input.GetAxis("Mouse X");
        continuous[3] = Input.GetAxis("Mouse Y");

        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
    #endregion
}