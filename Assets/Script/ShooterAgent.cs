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
    private float distanceDiffOfPrevious;
    private Vector3 previousPosition;
    private float timer;
    private float comparisonTime = 1f;
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
        previousPosition = transform.localPosition;
        SpawnObjects();
        //StartCoroutine(DelayedSpawn());
        distanceDiffOfPrevious = 0f;
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
        sensor.AddObservation(currentHealth / maxHealth);
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
        //Check();
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
        Debug.DrawRay(shootingPoint.position, -shootingPoint.up * 100f, Color.green, 1f);
        if (!pathfindingOnlyPhase && actions.DiscreteActions[0] == 1)
        {
            var wallMask = 1 << LayerMask.NameToLayer("Wall");
            if (Physics.Raycast(shootingPoint.position, -shootingPoint.up, out RaycastHit detection, 100f, wallMask))
            {
                AddReward(-0.005f);
                return;
            }

            var enemyMask = 1 << LayerMask.NameToLayer("Enemy");
            if (Physics.Raycast(shootingPoint.position, -shootingPoint.up, out RaycastHit hit, 100f, enemyMask))
            {
                var monster = hit.collider.GetComponent<MonsterController>();
                if (monster != null)
                {
                    character.AgentFire();
                    AddReward(1f);
                    Debug.Log("对怪物开枪了，他死了没有 "+monster.IsDead);
                    ////if (monster.IsDead && monster.hitCount >= monster.maxHits)
                    //if (monster.IsDead)
                    //{

                    //    AddReward(20f);
                    //    Debug.Log("我杀了怪物");
                    //}
                }                   
            }
            else
            {
                AddReward(-0.005f);
            }
        }
    }

    private void ApplyBehaviorPenalty()
    {       
        float currentDistanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
        float distanceDiffOfGoal = previousDistanceToGoal - currentDistanceToGoal;

        AddReward(distanceDiffOfGoal * 0.1f);
        previousDistanceToGoal = currentDistanceToGoal;

        distanceDiffOfPrevious = Vector3.Distance(transform.localPosition, previousPosition);
        timer += Time.deltaTime;
        if (timer >= comparisonTime)
        {
            previousPosition = transform.localPosition;

            if (distanceDiffOfPrevious > 1)
            {
                AddReward(distanceDiffOfPrevious * 0.25f);

            }
            else if (distanceDiffOfPrevious > 3)
            {
                AddReward(distanceDiffOfPrevious * 0.5f);
            }
            else
            {
                AddReward((distanceDiffOfPrevious - 1) * 0.001f);
            }
            timer = 0f;
        }

        AddReward(-0.001f);
    }
    #endregion

    #region Environment Interaction

    private void CheckBoundary()
    {       
        var wallLayer = 1 << LayerMask.NameToLayer("Wall"); 
        if (Physics.CheckSphere(transform.localPosition, 0.1f, wallLayer))
        {
            AddReward(-0.3f);
            Debug.Log("Collided with wall");
        }
        if (transform.localPosition.y < -13.5f)
        {
            Debug.Log("fall down");
            AddReward(-5f);
            EndEpisode();
        }

    }
    #endregion

    #region Spawn System
    private void SpawnObjects()
    {
        if (!pathfindingOnlyPhase) 
        {
            lock (spawnLock)
            {
                gameManager.SpawnMonsters(transform.parent.localPosition);
                monsters.AddRange(UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
            }
        }       
    }
    #endregion

    #region Health System
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        AddReward(-5f);

        if (currentHealth <= 0)
        {
            //AddReward(-10f);
            EndEpisode();
        }
    }
    #endregion

    #region Reset System
    private void ResetAgent()
    {
        currentHealth = maxHealth;
        rb.linearVelocity = Vector3.zero;
        transform.localPosition = new Vector3(50f, -7f, 27f);
        transform.rotation = Quaternion.Euler(0f, -174.29f, 0f);
        cameraLook = GetComponentInChildren<CameraLook>();
        if (cameraLook != null)
        {
            cameraLook.InitCamera();
        }

        Physics.SyncTransforms();
    }
    #endregion

    #region Goal System
    private void GoalReached()
    {
        AddReward(100f);
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
            //transform.Rotate(0, UnityEngine.Random.Range(0f, 90f), 0);
        }
        if (collision.gameObject.CompareTag("Building"))
        {
            AddReward(-0.05f);
            //transform.Rotate(0, UnityEngine.Random.Range(0f, 90f), 0);
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

    private void Check() 
    {
        if (pathfindingOnlyPhase)
        {
            if (GetCumulativeReward() < -50f)
            {
                Debug.Log("cumulative reward < -50 end episode。");
                EndEpisode();
            }
        }
    }


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