using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AISpaV2 : MonoBehaviour
{
    public Transform player;
    public float fleeRange = 3f;
    private NavMeshAgent agent;

    public Transform[] patrolPoints = new Transform[3];
    private int patrolIndex = 0;

    private void Start()
    {
        gameObject.TryGetComponent(out agent);
    }
    private void Update()
    {
        //SENSE
        float distance = Vector3.Distance(transform.position, player.position);
        Ray ray  =  new Ray(transform.position + (Vector3.up * 0.5f), player.position - transform.position);
        Physics.Raycast(ray, out RaycastHit hit);
        bool los = false;
        if(hit.collider.gameObject.TryGetComponent(out PlayerMovement playah))
        {
            los = true;
        }
        float patrolPointDistance = Vector3.Distance(transform.position, patrolPoints[patrolIndex].position);
        if ( los == false)
        {
            Debug.Log("WHUT");
            if(patrolPointDistance < 1f)
            {
                patrolIndex =  (patrolIndex + 1)% patrolPoints.Length;
            }
        }

        //Plan
        if (los == true)
        {
            if(distance > fleeRange)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                Vector3 dir = (transform.position - player.position).normalized;
                Vector3 fleePos = transform.position + dir * 5f;
                agent.SetDestination(fleePos);
            }
        }
        else
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }
}
