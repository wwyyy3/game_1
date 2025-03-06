using InfimaGames.LowPolyShooterPack;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Agents : Agent
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject goal;

    private CharacterBehaviour playerCharacter;
    private List<MonsterController> monsters = new List<MonsterController>();
    private GameManager gameManager;

    private int maxHealth = 10;
    private int currentHealth;
    private Vector2 move;
    private float moveSpeed = 10f;

    public override void Initialize() 
    {
        transform.position = new Vector3(83.8f, 1.8f, 13.8f);
        currentHealth = maxHealth;

    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
        SpawnObjects();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(goal.transform.position);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float move_x = actions.ContinuousActions[0];
        float move_y = actions.ContinuousActions[1];
        transform.position += new Vector3(move_x, 0, move_y) * Time.deltaTime * moveSpeed;

    }

    private void ResetAgent()
    {
        currentHealth = maxHealth;
        transform.position = new Vector3(83.8f, 1.8f, 13.8f); 
    }

    private void SpawnObjects()
    {
        gameManager.SpawnMonsters(Vector3.zero);
        monsters.AddRange(Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var _actionsOut = actionsOut.ContinuousActions;
        _actionsOut[0] = move.x;
        _actionsOut[1] = move.y;
        
    }
}
