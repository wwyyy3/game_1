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

    private CharacterBehaviour playerCharacter;
    private Character character;
    private List<MonsterController> monsters = new List<MonsterController>();
    private GameManager gameManager;
    private Rigidbody rb;
    private Movement Movement;
    private CameraLook cameraLook;

    private int maxHealth = 10;
    private int currentHealth;
    private float speedRunning = 15f;
    private float speedWalking = 10f;

    public Transform shootingPoint;
    public Vector3 Velocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }

    public override void Initialize()
    {
        character = GetComponent<Character>();
        cameraLook = GetComponent<CameraLook>();
        rb = GetComponent<Rigidbody>();
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
        currentHealth = maxHealth;
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
        //SpawnObjects();
    }

    private void shoot()
    {
        var layerMask = 1 << LayerMask.NameToLayer("Enemy");
        var direction = transform.forward;

        Debug.DrawRay(transform.position, direction, Color.blue, 1f);

        if (Physics.Raycast(shootingPoint.position, direction, out var hit, 200f, layerMask))
        {
            // Hit the enemy and call the enemy's GetShot method
            //hit.transform.GetComponent<MonsterController>().GetShot(damage, this);
        }
        else
        {
            AddReward(-0.033f);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(goal.transform.position);


    }

    private void FixedUpdate()
    {
        //AddReward(-1f / MaxStep);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        var movement = transform.forward * moveZ + transform.right * moveX;
        movement *= speedRunning;
        Velocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        //Shoot Discrete
        int fireAction = actions.DiscreteActions[0];
        if (fireAction == 1)
        {
            character.AgentFire();
        }

        Vector2 lookInput = new Vector2(actions.ContinuousActions[2], actions.ContinuousActions[3]);
        lookInput *= cameraLook.sensitivity;

        Quaternion rotationYaw = Quaternion.Euler(0f, lookInput.x, 0f);
        Quaternion rotationPitch = Quaternion.Euler(-lookInput.y, 0f, 0f);

        cameraLook.rotationCamera *= rotationPitch;
        cameraLook.rotationCharacter *= rotationYaw;

        Quaternion localRotation = transform.localRotation;

        if (cameraLook.smooth)
        {
            localRotation = Quaternion.Slerp(localRotation, cameraLook.rotationCamera, Time.deltaTime * cameraLook.interpolationSpeed);
            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, cameraLook.rotationCharacter, Time.deltaTime * cameraLook.interpolationSpeed)
            );
        }
        else
        {
            localRotation *= rotationPitch;
            localRotation = cameraLook.Clamp(localRotation);
            rb.MoveRotation(rb.rotation * rotationYaw);
        }

        transform.localRotation = localRotation;

        // Action -0.01 -----
        // Hit the enemy 10 

        // Receive hit -8 -----

        // goal 60-----

        // Die -100-----

        // bullet miss -0,3 

        // wall -1 ------ 

    }

    private void ResetAgent()
    {
        currentHealth = maxHealth;
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
    }

    //private void SpawnObjects()
    //{
    //    gameManager.SpawnMonsters(Vector3.zero);
    //    monsters.AddRange(Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
    //}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var _actionsOut = actionsOut.ContinuousActions;
        _actionsOut[0] = Input.GetAxisRaw("Horizontal");
        _actionsOut[1] = Input.GetAxisRaw("Vertical");

        _actionsOut[2] = Input.GetAxis("Mouse X");
        _actionsOut[3] = Input.GetAxis("Mouse Y");

        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        AddReward(-8f);
        Debug.Log("takeDamage -5");
        Debug.Log("Player took damage: " + damage + ", Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        AddReward(-100f);
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
        if (other.CompareTag("Wall"))
        {
            AddReward(-1f);
            Debug.Log("Hit wall -1");
        }
    }
}
