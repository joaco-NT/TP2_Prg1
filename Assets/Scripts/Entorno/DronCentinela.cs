using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class DronCentinela : MonoBehaviour
{
    [Header("Componentes")]
    public NavMeshAgent agent;

    [Header("Jugador")]
    public Transform player; // Arrastra a tu jugador aquí

    [Header("Patrulla (Waypoints)")]
    public List<Transform> patrolPoints; // Arrastra los puntos vacíos aquí
    private int currentIndex = 0;

    [Header("Detección")]
    public float detectionRange = 8f;
    
    private enum State { Patrolling, Chasing }
    private State currentState = State.Patrolling; // <-- CORREGIDO AQUÍ

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            GotoNextPatrolPoint();
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrolling:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = State.Chasing;
                    break;
                }

                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    GotoNextPatrolPoint();
                }
                break;

            case State.Chasing:
                agent.destination = player.position;

                if (distanceToPlayer > detectionRange * 1.5f)
                {
                    currentState = State.Patrolling;
                    GotoNextPatrolPoint();
                }
                break;
        }
    }

    void GotoNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        agent.destination = patrolPoints[currentIndex].position;
        currentIndex = (currentIndex + 1) % patrolPoints.Count;
    }

    private void OnDrawGizmos()
    {
        if (currentState == State.Chasing)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            var path = agent.path;
            
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}