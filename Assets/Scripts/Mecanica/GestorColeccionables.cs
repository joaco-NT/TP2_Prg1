using UnityEngine;
using System.Collections.Generic;

public class GestorColeccionablesOrdenado : MonoBehaviour
{
    public static GestorColeccionablesOrdenado Instancia { get; private set; }

    public enum EstadoColeccionables { FaltanItems, TodosCompletos }
    
    [Header("--- ESTADO DE COLECCIONABLES ---")]
    [SerializeField] private EstadoColeccionables estadoColeccionablesActual = EstadoColeccionables.FaltanItems;

    [Header("--- CARGAR LOS OBJETIVOS POR EL MAPA ---")]
    [Tooltip("Arrastra aquí los objetos del mapa. El total se calcula automáticamente.")]
    [SerializeField] private List<GameObject> itemsListaDesplegable = new List<GameObject>();
    
    public int TotalObjetivosRequeridos => itemsListaDesplegable.Count;

    [Header("Totales Recogidos")]
    [SerializeField] private int camposTotalesRecogidos = 0;

    [Header("Inventario Actual (En Juego)")]
    [SerializeField] private List<string> inventarioNombres = new List<string>();

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActualizarTotalColeccionables(TotalObjetivosRequeridos);
        }
        
        Debug.Log($"[Gestor] 🎯 Total de coleccionables detectados: {TotalObjetivosRequeridos}");
    }

    public void RegistrarRecoleccion(string nombreItem)
    {
        camposTotalesRecogidos++;
        inventarioNombres.Add(nombreItem);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegistrarColeccionableRecolectado();
        }

        EvaluarEstadoColeccionables();
        Debug.Log($"[Gestor] 🪙 Recogido: {nombreItem} | Progreso: {camposTotalesRecogidos}/{TotalObjetivosRequeridos}");
    }

    private void EvaluarEstadoColeccionables()
    {
        estadoColeccionablesActual = (camposTotalesRecogidos >= TotalObjetivosRequeridos) 
            ? EstadoColeccionables.TodosCompletos 
            : EstadoColeccionables.FaltanItems;

        switch (estadoColeccionablesActual)
        {
            case EstadoColeccionables.FaltanItems:
                Debug.Log($"[Gestor] 🔍 Faltan coleccionables. ({camposTotalesRecogidos}/{TotalObjetivosRequeridos})");
                break;

            case EstadoColeccionables.TodosCompletos:
                Debug.Log("[Gestor] ⭐ <b>¡Todos los coleccionables han sido reunidos!</b>");
                break;
        }
    }
}