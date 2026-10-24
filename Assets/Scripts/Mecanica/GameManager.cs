using UnityEngine;
using UnityEngine.SceneManagement;

/// ============================================================================
/// TEMA: Administrador Global con Control de Estados y Misión Sincronizada
/// ============================================================================
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum EstadoJuego { Jugando, Pausa, Victoria, Derrota }
    [Header("--- ESTADO GENERAL DEL JUEGO ---")]
    [SerializeField] private EstadoJuego estadoActual = EstadoJuego.Jugando;

    public enum EstadoMision { EnCurso, FaltaDestruirDron, FaltaColeccionables, Completada }
    [Header("--- ESTADO ACTUAL DE LA MISIÓN ---")]
    [SerializeField] private EstadoMision estadoMisionActual = EstadoMision.EnCurso;

    [Header("--- CONFIGURACIÓN DE JUGADOR Y SALUD ---")]
    [SerializeField] private int saludMaxima = 100;
    [SerializeField] private int saludActual;

    [Header("--- OBJETIVOS REQUERIDOS PARA GANAR ---")]
    [SerializeField] private int totalColeccionablesRequeridos = 4; 
    [SerializeField] private int coleccionablesActuales = 0;
    [SerializeField] private bool dronDerrotado = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        saludActual = saludMaxima;
    }

    private void Start()
    {
        estadoMisionActual = EstadoMision.EnCurso;
        CambiarEstado(EstadoJuego.Jugando);
        Debug.Log($"[GameManager] 🎮 ¡Misión iniciada! Estado actual: <b>{estadoMisionActual}</b>");
    }

    private void Update()
    {
        LeerEntradaPausa();
    }

    private void LeerEntradaPausa()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (estadoActual == EstadoJuego.Jugando) CambiarEstado(EstadoJuego.Pausa);
            else if (estadoActual == EstadoJuego.Pausa) ReanudarJuego();
        }
    }

    public EstadoJuego ObtenerEstadoActual() => estadoActual;

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;
        Debug.Log($"[GameManager] 🎮 <b>Estado del Juego cambiado a: {estadoActual}</b>");

        switch (estadoActual)
        {
            case EstadoJuego.Jugando:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case EstadoJuego.Pausa:
            case EstadoJuego.Victoria:
            case EstadoJuego.Derrota:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    public void ReanudarJuego() => CambiarEstado(EstadoJuego.Jugando);

    public void ModificarSalud(int cantidad)
    {
        if (estadoActual == EstadoJuego.Victoria || estadoActual == EstadoJuego.Derrota) return;

        int nuevaSalud = saludActual + cantidad;

        if (nuevaSalud <= 0)
        {
            saludActual = 0;
            Debug.Log($"[GameManager] ❤️ Salud del Jugador: <b>{saludActual}/{saludMaxima}</b>");
            Debug.Log("[GameManager] 💀 <b>¡DERROTA!</b> El jugador se ha quedado sin salud.");
            CambiarEstado(EstadoJuego.Derrota);
            return;
        }

        saludActual = Mathf.Clamp(nuevaSalud, 0, saludMaxima);
        Debug.Log($"[GameManager] ❤️ Salud del Jugador: <b>{saludActual}/{saludMaxima}</b>");
    }

    public void ActualizarTotalColeccionables(int nuevoTotal)
    {
        totalColeccionablesRequeridos = nuevoTotal;
        Debug.Log($"[GameManager] 📊 Meta de coleccionables sincronizada: <b>{totalColeccionablesRequeridos}</b>");
    }

    public void RegistrarColeccionableRecolectado()
    {
        if (estadoActual != EstadoJuego.Jugando) return;

        coleccionablesActuales++;
        Debug.Log($"[GameManager] 📦 Coleccionable obtenido: <b>{coleccionablesActuales}/{totalColeccionablesRequeridos}</b>");

        EvaluarProgresoMision();
    }

    public void RegistrarDronDestruido()
    {
        if (estadoActual != EstadoJuego.Jugando) return;

        dronDerrotado = true;
        Debug.Log("[GameManager] 🤖 <b>¡Dron Centinela eliminado!</b>");

        EvaluarProgresoMision();
    }

    private void EvaluarProgresoMision()
    {
        bool coleccionablesCompletos = (coleccionablesActuales >= totalColeccionablesRequeridos);

        if (!dronDerrotado && !coleccionablesCompletos) 
        {
            estadoMisionActual = EstadoMision.EnCurso;
        }
        else if (dronDerrotado && !coleccionablesCompletos) 
        {
            estadoMisionActual = EstadoMision.FaltaColeccionables;
            Debug.Log($"[GameManager] ⚠️ <b>¡Dron derrotado!</b> Pero faltan coleccionables ({coleccionablesActuales}/{totalColeccionablesRequeridos}). Estado misión: {estadoMisionActual}");
        }
        else if (!dronDerrotado && coleccionablesCompletos) 
        {
            estadoMisionActual = EstadoMision.FaltaDestruirDron;
            Debug.Log($"[GameManager] ⚠️ <b>¡Coleccionables completos!</b> Pero el Dron sigue activo. Estado misión: {estadoMisionActual}");
            NotificarDronColeccionablesListos(); 
        }
        else if (dronDerrotado && coleccionablesCompletos) 
        {
            estadoMisionActual = EstadoMision.Completada;
            Debug.Log($"[GameManager] 🏆 <b>¡MISIÓN CUMPLIDA!</b> Estado misión: {estadoMisionActual}");
            CambiarEstado(EstadoJuego.Victoria);
        }
    }

    private void NotificarDronColeccionablesListos()
    {
        DronCentinela dron = Object.FindFirstObjectByType<DronCentinela>();
        if (dron != null)
        {
            dron.ColeccionablesCompletadosAccion();
        }
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}