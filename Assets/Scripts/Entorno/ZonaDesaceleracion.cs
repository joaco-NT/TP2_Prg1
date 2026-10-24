using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ZonaDesaceleracion : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN DE ZONA DE BARRO ---")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float multiplicadorVelocidad = 0.4f; 
    [SerializeField] private bool bloquearSaltoEnBarro = true;      

    [Header("--- MENSAJE DE AVISO ---")]
    [SerializeField] private string mensajeAviso = "¡Cuidado! Zona de barro: Movimiento reducido y salto bloqueado.";

    private void Awake()
    {
        if (TryGetComponent<BoxCollider>(out var colliderZona))
        {
            colliderZona.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<MovimientoJugador>(out var jugador))
        {
            jugador.ConfigurarEstadoZona(multiplicadorVelocidad, bloquearSaltoEnBarro);
            Debug.Log($"[ZonaDesaceleracion] 🛑 <b>{mensajeAviso}</b>");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<MovimientoJugador>(out var jugador))
        {
            jugador.ConfigurarEstadoZona(1f, false);
            Debug.Log("[ZonaDesaceleracion] 🟢 Zona despejada. Velocidad normal y salto restaurados.");
        }
    }
}