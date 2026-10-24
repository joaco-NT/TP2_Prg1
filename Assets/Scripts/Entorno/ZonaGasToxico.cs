using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class ZonaGasToxico : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN DE DAÑO POR GAS ---")]
    [SerializeField] private int dañoPorTick = 10;            
    [SerializeField] private float intervaloDaño = 1.5f;      
    [SerializeField] private string mensajeAviso = "¡Alerta! Zona de gas tóxico: Perdiendo salud.";

    private Coroutine rutinaGasActiva;

    private void Awake()
    {
        if (TryGetComponent<BoxCollider>(out var colliderZona))
        {
            colliderZona.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[ZonaGasToxico] ⚠️ <b>{mensajeAviso}</b>");

            if (rutinaGasActiva == null)
            {
                rutinaGasActiva = StartCoroutine(RutinaAplicarDañoContinuo());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ZonaGasToxico] 🟢 Has salido de la zona tóxica.");

            if (rutinaGasActiva != null)
            {
                StopCoroutine(rutinaGasActiva);
                rutinaGasActiva = null;
            }
        }
    }

    private IEnumerator RutinaAplicarDañoContinuo()
    {
        while (true)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ModificarSalud(-dañoPorTick);
            }

            yield return new WaitForSeconds(intervaloDaño);
        }
    }
}