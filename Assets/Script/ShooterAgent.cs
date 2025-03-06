using InfimaGames.LowPolyShooterPack;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ShooterAgent : Agent
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject goal;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private Transform shootingPoint;

    private Character character;
    private List<MonsterController> monsters = new List<MonsterController>();
    private Rigidbody rb;

    private int maxHealth = 10;
    private int currentHealth;
    //private float speedRunning = 15f;
    private float speedWalking = 5f;

    //public Transform shootingPoint;
    public Vector3 Velocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }

    public override void Initialize()
    {
        character = GetComponent<Character>();
        rb = GetComponent<Rigidbody>();
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
        currentHealth = maxHealth;
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
        SpawnObjects();
    }

    private void shoot()
    {
        var layerMask = 1 << LayerMask.NameToLayer("Enemy");
        var direction = transform.forward;

        Debug.DrawRay(transform.position, direction, Color.blue, 1f);

        if (Physics.Raycast(shootingPoint.position, direction, out var hit, 200f, layerMask))
        {
            character.AgentFire();
        }
        else
        {
            AddReward(-0.033f);
            Debug.Log("did not hit -0.033");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(goal.transform.position);
        //monster -

    }

    private void FixedUpdate()
    {
        AddReward(-0.1f / MaxStep);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int fireAction = actions.DiscreteActions[0];
        if (fireAction == 1)
        {
            shoot();
        }

        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        var movement = transform.forward * moveZ + transform.right * moveX;
        movement *= speedWalking;
        Velocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        Vector2 lookInput = new Vector2(actions.ContinuousActions[2], actions.ContinuousActions[3]);
        cameraLook.pendingLookInput = lookInput;

        // Action -0.01 -----
        // Hit the enemy 10 ---

        // Receive hit -8 -----

        // goal 60-----

        // Die -100-----

        // bullet miss -0,3 ---

        // wall -1 ------ 

        // Building -1 ------

    }

    private void ResetAgent()
    {
        currentHealth = maxHealth;
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
    }

    private void SpawnObjects()
    {
        gameManager.SpawnMonsters(Vector3.zero);
        monsters.AddRange(Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
    }

    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    var _actionsOut = actionsOut.ContinuousActions;
    //    _actionsOut[0] = Input.GetAxisRaw("Horizontal");
    //    _actionsOut[1] = Input.GetAxisRaw("Vertical");

    //    _actionsOut[2] = Input.GetAxis("Mouse X");
    //    _actionsOut[3] = Input.GetAxis("Mouse Y");

    //    var discrete = actionsOut.DiscreteActions;
    //    discrete[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    //}

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        AddReward(-8f);
        Debug.Log("takeDamage -8");
        Debug.Log("Player took damage: " + damage + ", Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        AddReward(-100f);
        Debug.Log("die -100");

        EndEpisode();
    }

    private void GoalReached()
    {
        AddReward(100f);
        //cumulativeReward = GetCumulativeReward();

        EndEpisode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            GoalReached();
            Debug.Log("goal +100");
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("agent collision" + other.gameObject.name);
        if (other.collider.CompareTag("Wall"))
        {
            AddReward(-1f);
            Debug.Log("Hit wall -1");
        }

        if (other.collider.CompareTag("Building"))
        {
            AddReward(-1f);
            Debug.Log("Hit Building -1");
        }
    }
}
