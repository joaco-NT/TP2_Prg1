using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador principal del personaje (EVE) en 3ª persona.
/// Implementa movimiento basado en Rigidbody, detección de suelo por pivote y 
/// un sistema de interacción a distancia libre de vibraciones desde la cámara.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MovimientoJugador : MonoBehaviour
{
    // ==========================================
    // 1. CONFIGURACIÓN EN EL INSPECTOR
    // ==========================================
    [Header("Configuración de Movimiento")]
    [SerializeField] private float velocidadCaminata = 5f;
    [SerializeField] private float velocidadCorrer = 10f;
    [SerializeField] private float suavizadoAceleracion = 0.12f;
    [SerializeField] private float velocidadRotacion = 14f;

    [Header("Configuración de Salto y Suelo")]
    [SerializeField] private float fuerzaSalto = 7f;
    [SerializeField] private Transform transformPies;         // Pivote en la base de los pies
    [SerializeField] private float distanciaRaycastSuelo = 0.3f; 
    [SerializeField] private LayerMask capaSuelo = ~0; 

    [Header("Interacción a Distancia (Cámara 3ª Persona)")]
    [SerializeField] private float distanciaInteraccion = 5f; // Distancia de alcance adecuada
    [SerializeField] private LayerMask capaInteractiva;       // Filtro exclusivo (ej. "Interactable")

    [Header("Referencias Externas")]
    [SerializeField] private Transform camaraPrincipal;

    // ==========================================
    // 2. ESTADO INTERNO Y COMPONENTES
    // ==========================================
    private Rigidbody rb;
    
    private Vector2 inputMovimiento;
    private bool saltoSolicitado;
    private bool estaCorriendo;
    private bool estaEnSuelo;

    // Sistema Doble-W para correr
    private float tiempoUltimaPulsacionW = 0f;
    private const float UMBRAL_DOBLE_CLICK = 0.3f;

    // Variables de suavizado de física (SmoothDamp)
    private Vector3 velocidadMovimientoSmooth;
    private Vector3 velocidadRef;

    // ==========================================
    // 3. CICLO DE VIDA DE UNITY
    // ==========================================
    private void Awake()
    {
        InicializarComponentes();
    }

    private void Update()
    {
        LeerEntradas();
    }

    private void FixedUpdate()
    {
        ComprobarSuelo();
        ProcesarMovimiento();
        ProcesarSalto();
        RotarHaciaDireccion();
    }

    private void LateUpdate()
    {
        // Se procesa en LateUpdate para evitar temblores al sincronizarse con el renderizado final de la cámara
        ProcesarInteraccion();
    }

    // ==========================================
    // 4. INICIALIZACIÓN
    // ==========================================
    private void InicializarComponentes()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Previene que las fuerzas físicas vuelquen al personaje

        if (camaraPrincipal == null && Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }

        if (transformPies == null)
        {
            transformPies = transform;
        }
    }

    // ==========================================
    // 5. LÓGICA DE ENTRADAS (INPUT)
    // ==========================================
    private void LeerEntradas()
    {
        if (Keyboard.current == null) return;

        var teclado = Keyboard.current;

        float movX = 0f;
        float movZ = 0f;

        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) movX += 1f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) movX -= 1f;
        if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) movZ += 1f;
        if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) movZ -= 1f;

        inputMovimiento = new Vector2(movX, movZ).normalized;

        // Detección de Salto
        if (teclado.spaceKey.wasPressedThisFrame)
        {
            saltoSolicitado = true;
        }

        // Detección de Doble-W para Correr
        if (teclado.wKey.wasPressedThisFrame || teclado.upArrowKey.wasPressedThisFrame)
        {
            float tiempoActual = Time.time;
            if (tiempoActual - tiempoUltimaPulsacionW < UMBRAL_DOBLE_CLICK)
            {
                estaCorriendo = true;
            }
            tiempoUltimaPulsacionW = tiempoActual;
        }

        if (!teclado.wKey.isPressed && !teclado.upArrowKey.isPressed)
        {
            estaCorriendo = false;
        }
    }

    // ==========================================
    // 6. LÓGICA FÍSICA Y MOVIMIENTO
    // ==========================================
    private void ProcesarMovimiento()
    {
        float velocidadObjetivo = estaCorriendo ? velocidadCorrer : velocidadCaminata;
        Vector3 direccionMovimiento = Vector3.zero;

        if (camaraPrincipal != null && inputMovimiento.magnitude > 0.05f)
        {
            Vector3 frenteCam = camaraPrincipal.forward;
            Vector3 derechaCam = camaraPrincipal.right;
            frenteCam.y = 0f;
            derechaCam.y = 0f;
            frenteCam.Normalize();
            derechaCam.Normalize();

            direccionMovimiento = (frenteCam * inputMovimiento.y + derechaCam * inputMovimiento.x).normalized;
        }

        Vector3 velocidadDeseada = direccionMovimiento * velocidadObjetivo;

        // Transición de aceleración suave evitando cambios bruscos de velocidad
        velocidadMovimientoSmooth = Vector3.SmoothDamp(
            velocidadMovimientoSmooth,
            velocidadDeseada,
            ref velocidadRef,
            suavizadoAceleracion
        );

        rb.linearVelocity = new Vector3(velocidadMovimientoSmooth.x, rb.linearVelocity.y, velocidadMovimientoSmooth.z);
    }

    private void ProcesarSalto()
    {
        if (estaEnSuelo && saltoSolicitado)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
            saltoSolicitado = false;
        }

        if (!estaEnSuelo)
        {
            saltoSolicitado = false;
        }
    }

    private void RotarHaciaDireccion()
    {
        Vector3 direccionGiro = velocidadMovimientoSmooth;
        direccionGiro.y = 0f;

        if (direccionGiro.magnitude > 0.1f)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccionGiro);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.fixedDeltaTime);
        }
    }

    // ==========================================
    // 7. CHEQUEO DE SUELO (PIVOTE EN LOS PIES)
    // ==========================================
    private void ComprobarSuelo()
    {
        Vector3 origenRayo = transformPies.position;
        estaEnSuelo = Physics.Raycast(origenRayo, Vector3.down, distanciaRaycastSuelo, capaSuelo);
        Debug.DrawRay(origenRayo, Vector3.down * distanciaRaycastSuelo, estaEnSuelo ? Color.green : Color.red);
    }

    // ==========================================
    // 8. INTERACCIÓN A DISTANCIA 
    // ==========================================
    private void ProcesarInteraccion()
    {
        if (camaraPrincipal == null) return;

        // Dibuja el rayo en Scene totalmente firme sin temblores ni desalineación de fotogramas
        Debug.DrawRay(camaraPrincipal.position, camaraPrincipal.forward * distanciaInteraccion, Color.cyan);

        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            RaycastHit hit;

            // Raycast preciso filtrado por LayerMask
            if (Physics.Raycast(camaraPrincipal.position, camaraPrincipal.forward, out hit, distanciaInteraccion, capaInteractiva))
            {
                Debug.Log("¡Objeto interactuado con éxito: " + hit.collider.gameObject.name + "!");
            }
        }
    }
}