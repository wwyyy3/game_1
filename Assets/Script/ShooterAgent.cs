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
    [SerializeField] private int wallLayer;
    [SerializeField] private float spawnRadius = 100f;
    [SerializeField] private float minMonsterDistance = 5f;

    [Header("Training Phase")]
    public bool pathfindingOnlyPhase = true;

    [Header("Cooperative Settings")]
    public ShooterAgent teammate;
    public event Action<AgentActionType> OnTeammateAction;
    #endregion

    #region Internal State
    private Character character;
    private Rigidbody rb;
    private int maxHealth = 10;
    private int currentHealth;
    private float speedWalking = 5f;
    private List<MonsterController> monsters = new List<MonsterController>();
    private static readonly object spawnLock = new object();
    #endregion

    #region Initialization
    public override void Initialize()
    {
        character = GetComponent<Character>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        Physics.IgnoreLayerCollision(gameObject.layer, wallLayer, false);

        if (gameManager == null)
            gameManager = GetComponentInParent<GameManager>();
    }
    #endregion

    #region Episode Handling
    public override void OnEpisodeBegin()
    {
        CleanLegacyObjects();
        ResetAgent();
        StartCoroutine(DelayedSpawn());
        Debug.Log("episodeBegin");
    }

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForEndOfFrame();  
        SpawnObjects();                       
    }

    private void CleanLegacyObjects()
    {
        
        foreach (var m in monsters.ToArray())
        {
            if (m != null) Destroy(m.gameObject);
        }
        monsters.Clear();

        
        var agents = GameObject.FindGameObjectsWithTag("Player");
        foreach (var a in agents)
            if (a != gameObject && a != teammate?.gameObject) Destroy(a);
    }
    #endregion

    #region Observation System
    public override void CollectObservations(VectorSensor sensor)
    {
        // Self state
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(goal.transform.localPosition);
        sensor.AddObservation(currentHealth / (float)maxHealth);

        // Teammate observation
        if (teammate != null)
        {
            sensor.AddObservation(teammate.transform.position);
            sensor.AddObservation(teammate.currentHealth / (float)maxHealth);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Monster positions
        foreach (var monster in monsters)
            sensor.AddObservation(monster != null ? monster.transform.position : Vector3.zero);
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

        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        if (!CheckWallCollision(moveDirection))
        {
            Vector3 targetVelocity = moveDirection * speedWalking;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            AddReward(-0.1f);
            rb.linearVelocity = Vector3.zero; 
        }
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
        Debug.DrawRay(shootingPoint.position, transform.forward * 200f, Color.green, 2f);
        if (!pathfindingOnlyPhase && actions.DiscreteActions[0] == 1)
        {
            var layerMask = 1 << LayerMask.NameToLayer("Enemy");
            if (Physics.Raycast(shootingPoint.position, transform.forward, out RaycastHit hit, 200f, layerMask))
            {         
                var monster = hit.collider.GetComponent<MonsterController>();
                if (monster != null)
                {
                    character.AgentFire();
                    AddReward(1f);
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

        AddReward(-0.001f);
    }
    #endregion

    #region Environment Interaction


    private bool CheckWallCollision(Vector3 direction)
    {
        return Physics.Raycast(transform.position, direction, 1f, wallLayer);
    }

    private void CheckBoundary()
    {
        if (Physics.CheckSphere(transform.position, 1f, wallLayer))
        {
            AddReward(-5f);
            Debug.Log("teach the wall");
            EndEpisode();
        }
    }
    #endregion

    #region Cooperative System
    public void NotifyTeamAction(AgentActionType actionType)
    {
        OnTeammateAction?.Invoke(actionType);
        float reward = actionType switch
        {
            AgentActionType.GoalReached => 30f,
            AgentActionType.MonsterKilled => 5f,
            _ => 0f
        };
        AddReward(reward);
    }
    #endregion

    #region Spawn System
    private void SpawnObjects()
    {
        lock (spawnLock)
        {
            //SpawnMonsters();
            gameManager.SpawnMonsters(Vector3.zero);
            monsters.AddRange(UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
            SpawnTeammate();
        }
    }

    private void SpawnTeammate()
    {
        if (teammate != null)
        {
            bool spawned = false;
            Vector3[] directions = { transform.right, transform.forward, -transform.right, -transform.forward };
            foreach (var dir in directions)
            {
                Vector3 spawnPos = transform.position + dir * 2f;
                if (!Physics.CheckSphere(spawnPos, 0.5f, wallLayer))
                {
                    teammate.transform.position = spawnPos;
                    teammate.ResetAgent();
                    spawned = true;
                    break;
                }
            }
            if (!spawned)
            {
                EndEpisode();  
            }
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
        transform.localPosition = new Vector3(1.4f, -9.63769f, -0.6f);
        Physics.SyncTransforms();
    }
    #endregion

    #region Goal System
    private void GoalReached()
    {
        AddReward(100f);
        NotifyTeamAction(AgentActionType.GoalReached);
        EndEpisode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
            GoalReached();
    }
    #endregion

    #region Debug
    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    var continuous = actionsOut.ContinuousActions;
    //    continuous[0] = Input.GetAxisRaw("Horizontal");
    //    continuous[1] = Input.GetAxisRaw("Vertical");
    //    continuous[2] = Input.GetAxis("Mouse X");
    //    continuous[3] = Input.GetAxis("Mouse Y");

    //    var discrete = actionsOut.DiscreteActions;
    //    discrete[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    //}
    #endregion
}