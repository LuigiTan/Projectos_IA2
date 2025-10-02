using UnityEngine;
using UnityEngine.AI;

public class AISpa : MonoBehaviour
{
    public Transform player;
    public float fleeRange = 3f;
    private NavMeshAgent agent;

    private void Start()
    {
        gameObject.TryGetComponent(out agent);
    }

    private void Update()
    {
        //Empieza Percepcion
        float distance = Vector3.Distance(transform. position, player.position);
        //Empieza Planeacion (plan)
        if(distance < fleeRange)
            //Empieza la accion
        {
            Vector3 dir = (transform.position - player.position).normalized;
            Vector3 fleePos = transform.position + dir * 5f;
            agent.SetDestination(fleePos);
        }
        else
        {
            agent.SetDestination(player.position);
        }

    }

}
