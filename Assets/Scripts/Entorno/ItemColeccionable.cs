using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemColeccionable : MonoBehaviour
{
    [Header("--- DATOS DE ESTE ÍTEM ---")]
    [SerializeField] private string nombreItem = "Moneda";
    [SerializeField] private string tagJugador = "Player";

    private void Awake()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            if (GestorColeccionablesOrdenado.Instancia != null)
            {
                GestorColeccionablesOrdenado.Instancia.RegistrarRecoleccion(nombreItem);
            }
            else
            {
                Debug.LogWarning("[ItemColeccionable] ⚠️ ¡Falta el GestorColeccionablesOrdenado en la escena!");
            }

            Destroy(gameObject);
        }
    }
}