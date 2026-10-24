using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class DronCentinela : MonoBehaviour
{
    public enum EstadoDron { Patrullando, Persiguiendo, Congelado, Destruido }

    [Header("--- 1. COMPONENTES Y MOVIMIENTO ---")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float velocidadDron = 3.5f;

    [Header("--- 2. OBJETIVO (JUGADOR) ---")]
    [SerializeField] private Transform player; 

    [Header("--- 3. SALUD Y COMBATE ---")]
    [SerializeField] private int vidaDron = 100;
    [SerializeField] private int dañoAlJugador = 20;
    [SerializeField] private float fuerzaEmpujeDron = 8f;

    [Header("--- 4. EFECTOS VISUALES (HIELO) ---")]
    [SerializeField] private GameObject prefabHielo;            
    [SerializeField] private Transform puntoSpawnHielo;      

    [Header("--- 5. PATRULLA (Waypoints) ---")]
    [SerializeField] private List<Transform> patrolPoints; 
    private int currentIndex = 0;

    [Header("--- 6. DETECCIÓN Y RANGOS ---")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float perdidaRangoMultiplier = 1.5f;
    [SerializeField] private float distanciaDetencionAtaque = 1.2f;

    [Header("--- ESTADO ACTUAL ---")]
    [SerializeField] private EstadoDron estadoActual = EstadoDron.Patrullando;

    private Rigidbody rb;
    private BoxCollider boxColliderDron;
    private GameObject objetoHieloInstanciado; 

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        boxColliderDron = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        agent.speed = velocidadDron;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.freezeRotation = true;
        boxColliderDron.isTrigger = false;

        if (player == null)
        {
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null) player = jugadorObj.transform;
        }

        if (prefabHielo != null)
        {
            Transform pivote = puntoSpawnHielo != null ? puntoSpawnHielo : transform;
            objetoHieloInstanciado = Instantiate(prefabHielo, pivote.position, pivote.rotation, pivote);
            objetoHieloInstanciado.SetActive(false);
        }

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            currentIndex = 0;
            IrAlSiguienteWaypointEstricto();
        }
    }

    private void Update()
    {
        if (estadoActual == EstadoDron.Destruido || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (estadoActual)
        {
            case EstadoDron.Patrullando:
                ManejarPatrulla(distanceToPlayer);
                break;

            case EstadoDron.Persiguiendo:
                ManejarPersecucion(distanceToPlayer);
                break;

            case EstadoDron.Congelado:
                ManejarCongelamiento();
                break;
        }
    }

    private void ManejarPatrulla(float distanceToPlayer)
    {
        agent.isStopped = false;
        agent.stoppingDistance = 0.2f;

        if (distanceToPlayer <= detectionRange)
        {
            estadoActual = EstadoDron.Persiguiendo;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                AvanzarAlSiguientePunto();
            }
        }
    }

    private void ManejarPersecucion(float distanceToPlayer)
    {
        agent.isStopped = false;
        agent.stoppingDistance = distanciaDetencionAtaque;
        agent.destination = player.position;

        if (distanceToPlayer > detectionRange * perdidaRangoMultiplier)
        {
            estadoActual = EstadoDron.Patrullando;
            RetornarAPatrullaMasCercana();
        }
    }

    private void ManejarCongelamiento()
    {
        if (agent.isOnNavMesh && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private void IrAlSiguienteWaypointEstricto()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;
        if (patrolPoints[currentIndex] != null)
        {
            agent.destination = patrolPoints[currentIndex].position;
        }
    }

    private void AvanzarAlSiguientePunto()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;
        currentIndex = (currentIndex + 1) % patrolPoints.Count;
        IrAlSiguienteWaypointEstricto();
    }

    private void RetornarAPatrullaMasCercana()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        int indiceMasCercano = 0;
        float menorDistancia = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] != null)
            {
                float distancia = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    indiceMasCercano = i;
                }
            }
        }

        currentIndex = indiceMasCercano;
        IrAlSiguienteWaypointEstricto();
    }

    public void AturdirDron(float duracion, int dañoRecibido = 25)
    {
        if (estadoActual == EstadoDron.Congelado || estadoActual == EstadoDron.Destruido) return;

        vidaDron = Mathf.Max(0, vidaDron - dañoRecibido);
        Debug.Log($"[DronCentinela] 🤖 Salud del Dron: <b>{vidaDron}/100</b> (Daño recibido: {dañoRecibido})");

        if (vidaDron <= 0)
        {
            DestruirDron();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RutinaCongelamiento(duracion));
    }

    public void ColeccionablesCompletadosAccion()
    {
        if (estadoActual == EstadoDron.Destruido) return;
        velocidadDron *= 1.5f;
        if (agent != null) agent.speed = velocidadDron;
    }

    private IEnumerator RutinaCongelamiento(float duracion)
    {
        EstadoDron estadoAnterior = estadoActual;
        estadoActual = EstadoDron.Congelado;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (objetoHieloInstanciado != null) objetoHieloInstanciado.SetActive(true);

        yield return new WaitForSeconds(duracion);

        if (estadoActual != EstadoDron.Destruido)
        {
            if (objetoHieloInstanciado != null) objetoHieloInstanciado.SetActive(false);
            estadoActual = estadoAnterior; 
        }
    }

    private void DestruirDron()
    {
        estadoActual = EstadoDron.Destruido;
        
        if (objetoHieloInstanciado != null)
        {
            objetoHieloInstanciado.SetActive(false);
            Destroy(objetoHieloInstanciado);
        }

        if (agent.isOnNavMesh) agent.isStopped = true;
        if (boxColliderDron != null) boxColliderDron.enabled = false;
        
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            rend.enabled = false;
        }

        if (GameManager.Instance != null) GameManager.Instance.RegistrarDronDestruido();

        Destroy(gameObject, 0.1f);
    }

    public bool EstaCongelado() => estadoActual == EstadoDron.Congelado;

    private void OnCollisionEnter(Collision collision)
    {
        if (estadoActual == EstadoDron.Congelado || estadoActual == EstadoDron.Destruido) return;

        if (collision.gameObject.CompareTag("Pared"))
        {
            if (agent.isOnNavMesh) agent.velocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.ModificarSalud(-dañoAlJugador);

            Vector3 direccionEmpuje = (collision.transform.position - transform.position);
            direccionEmpuje.y = 0f;
            direccionEmpuje.Normalize();

            if (direccionEmpuje == Vector3.zero) direccionEmpuje = -transform.forward;

            if (collision.gameObject.TryGetComponent<MovimientoJugador>(out var scriptJugador))
            {
                scriptJugador.RecibirEmpujeDron(direccionEmpuje * fuerzaEmpujeDron);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = estadoActual switch
        {
            EstadoDron.Persiguiendo => Color.red,
            EstadoDron.Congelado => Color.blue,
            _ => Color.yellow
        };
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}