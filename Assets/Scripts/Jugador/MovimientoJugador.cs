using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class MovimientoJugador : MonoBehaviour
{
    private enum EstadoJugador { Normal, EmpujadoPorDron }

    [Header("--- 1. MOVIMIENTO Y FÍSICA ---")]
    [SerializeField] private float velocidadCaminata = 5f;
    [SerializeField] private float velocidadCorrer = 10f;
    [SerializeField] private float suavizadoAceleracion = 0.12f;
    [SerializeField] private float velocidadRotacion = 14f;
    [SerializeField] private float fuerzaSalto = 5f;
    [SerializeField] private float gravedad = -20f;

    [Header("--- 2. CHEQUEO DE SUELO ---")]
    [SerializeField] private Transform transformPies;           
    [SerializeField] private float distanciaRaycastSuelo = 0.3f; 
    [SerializeField] private LayerMask capaSuelo; 

    [Header("--- 3. INTERACCIÓN FRONTAL (E) ---")]
    [SerializeField] private float distanciaInteraccion = 5f; 
    [SerializeField] private LayerMask capaInteractiva; 
    [SerializeField] private Transform camaraPrincipal;

    [Header("--- 4. DESLIZAMIENTO EN PAREDES ---")]
    [SerializeField] private string tagObstaculoPared = "Pared";
    [SerializeField] private float fuerzaDeslizamiento = 6f;
    [SerializeField] private float suavidadResbalon = 5f;

    private EstadoJugador estadoActual = EstadoJugador.Normal;
    private CharacterController controller;
    private CamaraSeguimiento camaraSeguimientoScript;
    
    private Vector2 inputMovimiento;
    private bool saltoSolicitado;
    private bool estaCorriendo;
    private bool estaEnSuelo;
    private float velocidadY;
    private float tiempoDesdeUltimoSalto = 0f;
    
    private Vector3 velocidadMovimientoSmooth;
    private Vector3 velocidadRef;

    private float multiplicadorVelocidadExterno = 1f;
    private bool saltoBloqueadoPorZona = false; 

    private Vector3 velocidadDeslizamientoActual = Vector3.zero;
    private bool estaResbalando = false;
    private Vector3 velocidadDeslizamientoTarget = Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (camaraPrincipal == null && Camera.main != null) camaraPrincipal = Camera.main.transform;
        if (camaraPrincipal != null) camaraSeguimientoScript = camaraPrincipal.GetComponent<CamaraSeguimiento>();
        if (transformPies == null) transformPies = transform;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.ObtenerEstadoActual() == GameManager.EstadoJuego.Pausa) return;

        LeerEntradasTeclado();
        ComprobarSueloPorRaycast(); 

        switch (estadoActual)
        {
            case EstadoJugador.Normal:
                ProcesarMovimiento();
                ProcesarSalto();
                RotarHaciaDireccion();
                break;

            case EstadoJugador.EmpujadoPorDron:
                if (estaEnSuelo && velocidadY < 0f) velocidadY = -2f;
                else velocidadY += gravedad * Time.deltaTime;
                break;
        }

        ProcesarInteraccionFrontal(); 
    }

    private void LeerEntradasTeclado()
    {
        if (Keyboard.current == null || estadoActual == EstadoJugador.EmpujadoPorDron) return;
        var teclado = Keyboard.current;

        float movX = 0f; float movZ = 0f;
        if (teclado.dKey.isPressed) movX += 1f;
        if (teclado.aKey.isPressed) movX -= 1f;
        if (teclado.wKey.isPressed) movZ += 1f;
        if (teclado.sKey.isPressed) movZ -= 1f;

        inputMovimiento = new Vector2(movX, movZ).normalized;
        if (teclado.spaceKey.wasPressedThisFrame) saltoSolicitado = true;
        estaCorriendo = teclado.wKey.isPressed;
    }

    private void ComprobarSueloPorRaycast()
    {
        if (tiempoDesdeUltimoSalto > 0f) 
        { 
            tiempoDesdeUltimoSalto -= Time.deltaTime; 
            estaEnSuelo = false; 
            return; 
        }
        
        estaEnSuelo = Physics.Raycast(transformPies.position, Vector3.down, distanciaRaycastSuelo, capaSuelo) && velocidadY <= 0.1f;
        Debug.DrawRay(transformPies.position, Vector3.down * distanciaRaycastSuelo, estaEnSuelo ? Color.green : Color.red);
    }

    private void ProcesarInteraccionFrontal()
    {
        if (camaraSeguimientoScript == null || Keyboard.current == null) return;
        
        Ray rayoCamara = camaraSeguimientoScript.ObtenerRayoInteraccion();
        Debug.DrawRay(rayoCamara.origin, rayoCamara.direction * distanciaInteraccion, Color.cyan);

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (Physics.Raycast(rayoCamara, out RaycastHit hit, distanciaInteraccion, capaInteractiva))
            {
                if (hit.collider.TryGetComponent<ObjetoInteractuable>(out var objetoInteractivo))
                {
                    objetoInteractivo.Interactuar();
                }
            }
        }
    }

    private void ProcesarMovimiento()
    {
        float velocidadBase = estaCorriendo ? velocidadCorrer : velocidadCaminata;
        float velocidadObjetivo = velocidadBase * multiplicadorVelocidadExterno;
        
        Vector3 direccionMovimiento = Vector3.zero;

        if (camaraPrincipal != null && inputMovimiento.sqrMagnitude > 0.0025f)
        {
            Vector3 frenteCam = camaraPrincipal.forward; frenteCam.y = 0f; frenteCam.Normalize();
            Vector3 derechaCam = camaraPrincipal.right; derechaCam.y = 0f; derechaCam.Normalize();
            direccionMovimiento = (frenteCam * inputMovimiento.y + derechaCam * inputMovimiento.x).normalized;
        }

        Vector3 velocidadDeseada = direccionMovimiento * velocidadObjetivo;
        velocidadMovimientoSmooth = Vector3.SmoothDamp(velocidadMovimientoSmooth, velocidadDeseada, ref velocidadRef, suavizadoAceleracion);

        if (estaEnSuelo && velocidadY < 0f) velocidadY = -2f;
        else velocidadY += gravedad * Time.deltaTime;

        Vector3 movimientoFinal;
        if (estaResbalando)
        {
            velocidadDeslizamientoActual = Vector3.Lerp(velocidadDeslizamientoActual, velocidadDeslizamientoTarget, suavidadResbalon * Time.deltaTime);
            movimientoFinal = velocidadDeslizamientoActual + new Vector3(0f, velocidadY, 0f);
            estaResbalando = false; 
        }
        else
        {
            velocidadDeslizamientoActual = Vector3.Lerp(velocidadDeslizamientoActual, Vector3.zero, suavidadResbalon * Time.deltaTime);
            movimientoFinal = new Vector3(velocidadMovimientoSmooth.x, velocidadY, velocidadMovimientoSmooth.z) + velocidadDeslizamientoActual;
        }

        controller.Move(movimientoFinal * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag(tagObstaculoPared))
        {
            Vector3 normalSuperficie = hit.normal;
            velocidadDeslizamientoTarget = new Vector3(normalSuperficie.x, -1f, normalSuperficie.z).normalized * fuerzaDeslizamiento;
            estaResbalando = true;
        }
    }

    private void ProcesarSalto()
    {
        if (estaEnSuelo && saltoSolicitado)
        {
            if (!saltoBloqueadoPorZona)
            {
                velocidadY = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
                estaEnSuelo = false;
                tiempoDesdeUltimoSalto = 0.2f; 
            }
            else
            {
                Debug.Log("[MovimientoJugador] ⚠️ <b>¡No puedes saltar! Estás atrapado en el barro.</b>");
            }
        }
        
        saltoSolicitado = false;
    }

    private void RotarHaciaDireccion()
    {
        Vector3 direccionGiro = velocidadMovimientoSmooth; 
        direccionGiro.y = 0f;
        
        if (direccionGiro.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccionGiro);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime);
        }
    }

    public void RecibirEmpujeDron(Vector3 fuerzaEmpuje)
    {
        StopAllCoroutines();
        StartCoroutine(RutinaEmpujeControlado(fuerzaEmpuje));
    }

    private IEnumerator RutinaEmpujeControlado(Vector3 fuerzaEmpuje)
    {
        estadoActual = EstadoJugador.EmpujadoPorDron;
        float tiempoTranscurrido = 0f;
        float duracionEmpuje = 0.2f;

        while (tiempoTranscurrido < duracionEmpuje)
        {
            Vector3 impulsoActual = Vector3.Lerp(fuerzaEmpuje, Vector3.zero, tiempoTranscurrido / duracionEmpuje);
            controller.Move((impulsoActual + new Vector3(0f, velocidadY, 0f)) * Time.deltaTime);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        estadoActual = EstadoJugador.Normal;
    }

    public void ConfigurarEstadoZona(float multiplicadorVelocidad, bool bloquearSalto)
    {
        multiplicadorVelocidadExterno = Mathf.Max(0.1f, multiplicadorVelocidad);
        saltoBloqueadoPorZona = bloquearSalto;
    }
}