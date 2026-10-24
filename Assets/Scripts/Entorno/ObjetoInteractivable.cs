using UnityEngine;

public class ObjetoInteractuable : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN DE INTERACCIÓN ---")]
    [SerializeField] private bool estaActivado = false; 

    public void Interactuar()
    {
        estaActivado = !estaActivado;

        string estadoTexto = estaActivado ? "ACTIVADO / ABIERTO" : "DESACTIVADO / CERRADO";
        string icono = estaActivado ? "🔓" : "🔒";
        
        Debug.Log($"[Objeto] {icono} <b>{gameObject.name}</b> ha sido {estadoTexto}. (Valor actual: {estaActivado})");
    }

    public bool ObtenerEstado() => estaActivado;
}