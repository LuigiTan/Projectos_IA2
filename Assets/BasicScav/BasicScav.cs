using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using TMPro;

public class BasicScav : MonoBehaviour
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

    //Variables para loot
    public float lootDetectionRadius = 5f; // Rango de deteccion. Tal vez lo tenga que bajar
    private LootItem targetLoot; // loot a la que va a ir

    public float lootPickUpRange = 1.2f;

    [SerializeField] private LootItem carryingLoot;// Antes usaba esto, ahora solo esta para debug
    [SerializeField] private bool hasLoot = false;
    [SerializeField] private string carriedLootName = "";//Redundante
    [SerializeField] private int carriedLootValue = 0;

    //Debug
    [SerializeField] private string inventoryStatus = "Inventario: [Vacio]";

    private void Start()
    {
        actionScores = new Dictionary<string, float>()
        {
            {"Flee", 0f },
            {"Chase", 0f },
            {"Patrol" ,0f },
            {"Collect",0f }//Nueva accion
        };

        health = maxHealth;

        gameObject.TryGetComponent(out agentSmith);
    }

    private void Update()
    {
        //SENSE
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        lineOfSight = PlayerInFOV();
        if (Vector3.Distance(patrolPoints[patrolIndex].position, transform.position) < distanceCheck)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        //----> Es probable que haya otra forma de hacer esto, lo iba a hacer con una esfera como hija del enemigo pero este tambien jala
        //Podria hacer la esfera en otro metodo para que no sea tan pesado con una rutina, pero hay muy pocos objetos asi que creo que por ahora no deberia haber problema
        // Buscar loot si no estamos cargando nada
        if (!hasLoot) //Esto se podria cambiar por una comparacion de inventory space si lo quisiera usar despues
        {
            // Una esfera con el radio de deteccion
            Collider[] hits = Physics.OverlapSphere(transform.position, lootDetectionRadius);

            LootItem bestLoot = null;
            int bestValue = -1;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out LootItem loot))
                {
                    if (loot.value > bestValue)
                    {
                        bestValue = loot.value;
                        bestLoot = loot;
                    }
                }
            }

            targetLoot = bestLoot;
        }
        else
        {
            targetLoot = null; // si ya tiene loot, ignora lo que se encuentre
        }

        //------------->   PLAN 

        UpdatePrediction();
        float healthRatio = Mathf.Clamp01(health / maxHealth);
        float distanceRatio = Mathf.Clamp01(distanceToPlayer / fleeDistance);

        if (healthRatio <= criticalHealthLimit) { distanceRatio = 0; }

        float riskFactor = (1 - healthRatio) * (1 - distanceRatio);

        float aggroFactor = healthRatio * distanceRatio;

        float total = riskFactor + aggroFactor;

        riskFactor /= total;
        aggroFactor /= total;
        aggroFactor *= healthRatio > criticalHealthLimit ? 1 : 0;

        actionScores["Flee"] = riskFactor * 10 * (lineOfSight == true ? 1 : 0);
        actionScores["Chase"] = aggroFactor * 10 * (lineOfSight == true ? 1 : 0);
        actionScores["Patrol"] = 3f;

        //Accion para lootear 
        //Solo entra si tiene un target para lootear y no esta cargando nada
        if (targetLoot != null && hasLoot == false)
        {
            actionScores["Collect"] = 5f;//Esto deberia de ser prioridad intermedia. Para que nunca se ponga a lootear en medio del chase
        }
        else
        {
            actionScores["Collect"] = 0f;
        }

        fleeText.text = "FLEE = " + actionScores["Flee"];
        chaseText.text = "CHASE = " + actionScores["Chase"];

        string chosenAction = actionScores.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        switch (chosenAction)
        {
            //ACT
            case "Flee":
                Flee();
                break;
            case "Chase":
                Chase();
                break;
            case "Patrol":
                Patrol();
                break;
            case "Collect":
                Collect();
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
        Debug.Log("Was Hit! Only have " +  health + " hp left.");
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
        agentSmith.SetDestination(predictedPlayerPos);
    }

    private void Patrol()
    {
        agentSmith.SetDestination(patrolPoints[patrolIndex].position);
    }

    #region FleeAlternative
    public float maxDistFromDirection = 100;
    public float step = 10f;
    public float fleeLength = 3f;
    

    private Vector3 FindFleeAlternative(Vector3 fleeDirection)
    {
        float maxDistanceFromPlayer = 0;
        Vector3 bestoPosition = transform.position;

        for(float angle = -maxDistFromDirection; angle <= maxDistFromDirection; angle += step)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * fleeDirection;
            Vector3 candidate = transform.position + dir * fleeLength;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                float distToPlayer = Vector3.Distance(candidate, player.position);
                if (distToPlayer > maxDistanceFromPlayer)
                {
                    maxDistanceFromPlayer = distToPlayer;
                    bestoPosition = hit.position;
                }
            }
        }
        return bestoPosition;
    }

    #endregion


    #region Predict
    Vector3 lastPlayerPosition = new Vector3();
    Vector3 predictedPlayerPos = new Vector3();

    private void UpdatePrediction()
    {
        Vector3 currentPlayerPos = player.position;
        Vector3 moveDirection = (currentPlayerPos - lastPlayerPosition).normalized;

        float predictionDistance = distanceToPlayer * 0.5f;

        predictedPlayerPos = currentPlayerPos + moveDirection * predictionDistance;

        lastPlayerPosition = currentPlayerPos;
    }

    #endregion


    private bool isCollecting = false;

    private void Collect()
    {
        if (targetLoot == null) return;//Tal vez son demasiados failsafes

        //Ir a la loot
        agentSmith.SetDestination(targetLoot.transform.position);

        //Checar si ya esta ahi
        if (!isCollecting && Vector3.Distance(transform.position, targetLoot.transform.position) < lootPickUpRange) //<-- Bajar si es necesario [UPDATE]: Mejor lo hago variable, es 1.2 originalmente
        {
            StartCoroutine(CollectLootRoutine());
        }
    }
    private System.Collections.IEnumerator CollectLootRoutine()
    {
        isCollecting = true;
        agentSmith.ResetPath(); // A ver si funciona como deberia

        yield return new WaitForSeconds(1f); // Deberia de simular que agarra el objeto

        //Datos de la loot robada
        if (targetLoot != null)
        {
            carriedLootName = targetLoot.lootName;
            carriedLootValue = targetLoot.value;
            hasLoot = true;// Aqui se cambiaria a incrementar el espacio en inventario
            inventoryStatus = "Inventario: [" + carriedLootName + "]";

            Destroy(targetLoot.gameObject);
            targetLoot = null;
        }
            
        isCollecting = false;
    }

}
