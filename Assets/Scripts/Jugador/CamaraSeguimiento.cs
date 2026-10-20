using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraSeguimiento : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform objetivo; // Arrastra aquí tu Jugador

    [Header("Configuración de Órbita y Mouse")]
    [SerializeField] private float distancia = 6f;          // Distancia respecto al jugador
    [SerializeField] private float sensibilidadMouse = 2f;  // Velocidad de giro con el mouse
    [SerializeField] private float pitchMin = -20f;         // Límite para no mirar muy abajo
    [SerializeField] private float pitchMax = 60f;          // Límite para no mirar muy arriba

    [Header("Configuración de Suavizado")]
    [SerializeField] private float velocidadSuavizado = 15f; // Qué tan fluida sigue la cámara

    private float rotacionX = 0f; // Inclinación vertical (Pitch)
    private float rotacionY = 0f; // Rotación horizontal (Yaw)

    void Start()
    {
        BuscarObjetivoAutomaticamente();
        ConfigurarCursorInicial();
        InicializarRotacion();
    }

    void Update()
    {
        ComprobarLiberacionCursor();

        if (objetivo == null) return;

        LeerEntradasMouse();
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        PosicionarYEnfocarCamara();
    }

    // ==========================================
    // MÉTODOS AUXILIARES
    // ==========================================
    private void BuscarObjetivoAutomaticamente()
    {
        if (objetivo == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            objetivo = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    private void ConfigurarCursorInicial()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InicializarRotacion()
    {
        Vector3 angulos = transform.eulerAngles;
        rotacionY = angulos.y;
        rotacionX = angulos.x;
    }

    private void ComprobarLiberacionCursor()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LeerEntradasMouse()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 deltaMouse = Mouse.current.delta.ReadValue();
            mouseX = deltaMouse.x * sensibilidadMouse * 0.1f;
            mouseY = deltaMouse.y * sensibilidadMouse * 0.1f;
        }

        rotacionY += mouseX;
        rotacionX -= mouseY;

        // Limitar la rotación vertical para que la cámara no dé la vuelta completa
        rotacionX = Mathf.Clamp(rotacionX, pitchMin, pitchMax);
    }

    private void PosicionarYEnfocarCamara()
    {
        // Calcular la rotación basada en los ejes del mouse
        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0);

        // Calcular la posición deseada alrededor del jugador (apuntando al torso, sumando Vector3.up * 1.5f)
        Vector3 puntoFoco = objetivo.position + Vector3.up * 1.5f;
        Vector3 posicionDeseada = puntoFoco - (rotacion * Vector3.forward * distancia);

        // Mover la cámara suavemente hacia la posición calculada
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, velocidadSuavizado * Time.deltaTime);

        // Hacer que la cámara siempre mire al jugador
        transform.LookAt(puntoFoco);
    }
}