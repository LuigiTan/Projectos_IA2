using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using JetBrains.Annotations;
using TMPro;
using System.Runtime.CompilerServices;

public class BobSPA_Incomplete : MonoBehaviour
{
    private float health = 50;
    private float maxHealth = 100;

    public Transform player;

    private float distanceToPlayer;
    public float fleeDistance = 4f;
    private bool lineOfSight = false;

    private Dictionary<string, float> actionScores;

    public Transform[] patrolPoints = new Transform[4];
    private int patrolIndex = 0;
    public float distanceCheck = 1;

    private NavMeshAgent agentSmith;

    public TextMeshProUGUI fleeText;
    public TextMeshProUGUI chaseText;

    public float viewDistance = 10f;
    public float viewAngle = 45f;

    public float criticalHealthLimit = 0.3f;
    private void Start()
    {
        actionScores = new Dictionary<string, float>()
        {
            { "Flee", 0f},
            {"Chase", 0f},
            {"Patrol", 0f },
        };

        health = maxHealth;
        gameObject.TryGetComponent(out agentSmith);
    }

    private void Update()
    {
        //Sense
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
       
        lineOfSight = PlayerInFOV();
        if (Vector3.Distance(patrolPoints[patrolIndex].position, transform.position) < distanceCheck)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        //Plan 
        float healthRatio = Mathf.Clamp01(health / maxHealth);
        float distanceRatio = Mathf.Clamp01(distanceToPlayer / fleeDistance);

        if (healthRatio <= criticalHealthLimit) { distanceRatio = 0; }

        float riskFactor = (1 - healthRatio) * (1 - distanceRatio);

        float aggroFactor = healthRatio * distanceRatio;

        float total = riskFactor + aggroFactor;

        riskFactor /= total;
        aggroFactor /= total;
        aggroFactor *= healthRatio > criticalHealthLimit ? 1 : 0;
        
        actionScores["Flee"] = (distanceToPlayer < fleeDistance ? 10 : 0) + (health < (health * 0.5) ? 5f :0) * (lineOfSight == true ? 1 : 0);
        actionScores["Chase"] = (distanceCheck >= fleeDistance ? 8f : 0f) * (health > (health * 0.5) ? 5f : 0) * (lineOfSight == true ? 1 : 0);
        actionScores["Patrol"] = 3f;

        fleeText.text = "FLEE = " + actionScores["Flee"];
        chaseText.text = "CHASE = " + actionScores["Chase"];

        //ACT
        string chosenAction = actionScores.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        switch (chosenAction)
        {
            case "Flee":
                Flee();

                break;

            case "Chase":
                Chase();

                break;

            case "Patrol":
                Patrol();

                break;

            default: 
                break;
        }
    }

    private bool PlayerInFOV()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        float angletoPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        if (angletoPlayer > viewAngle / 2)
        {
            return false;
        }

        if (Physics.Raycast(transform.position, dirToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            if (hit.collider.gameObject.TryGetComponent(out PlayerMovement _))
            {
                return true;
            }
            return false;
        }
        return false;
    }
    public void GetHit()
    {
        health -= 9;
        Debug.Log("Was hit! Only have" + health + " hp left.");
    }
    private void Flee()
    {
        Vector3 fleeDir = transform.position + (transform.position - player.position).normalized * 2;
        if (NavMesh.SamplePosition(fleeDir, out NavMeshHit hit, 1, NavMesh.AllAreas)) 
        {
            agentSmith.SetDestination(fleeDir);
        }
        else
        {
            agentSmith.SetDestination(FindFleeAlternative(fleeDir));
        }
    }
    private void Chase()
    {
        //agentSmith.SetDestination(predictedPlayerpos);
    }
    private void Patrol()
    {
        agentSmith.SetDestination(patrolPoints[patrolIndex].position);
    }

    public float maxDistFromDirection = 100;
    public float step = 10f;
    public float fleeLength = 3f;
    

    private Vector3 FindFleeAlternative(Vector3 fleeDirection)
    {
        float maxDistanceFromPlayer = 0;
        Vector3 bestPosition = transform.position;

        for (float angle = -maxDistFromDirection; angle <= maxDistFromDirection; angle++)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * fleeDirection;
            Vector3 candidate = transform.position + dir * fleeLength;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer > maxDistanceFromPlayer) 
                {
                     maxDistanceFromPlayer = distToPlayer;
                    bestPosition =hit.position;
                }
            }

        }
        return bestPosition;
        }

    #region Predcit
    Vector3 lastPlayer = new Vector3();
    Vector3 predicPlayerpos = new Vector3();

    private void UpdatePrediciton()
    {
        float predictionDistante = distanceToPlayer * 0.5f;
    }
    #endregion


}
