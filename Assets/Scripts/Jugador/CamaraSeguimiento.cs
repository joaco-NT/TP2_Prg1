using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField] private float distancia = 6f;          
    [SerializeField] private float sensibilidadMouse = 2f;   
    [SerializeField] private float pitchMin = -20f;         
    [SerializeField] private float pitchMax = 60f;          
    [SerializeField] private float velocidadSuavizado = 15f; 

    private float rotacionX = 0f, rotacionY = 0f; 
    private Camera camaraPrincipal;

    private void Awake()
    {
        camaraPrincipal = GetComponent<Camera>();
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
    }

    private void Start()
    {
        if (objetivo == null)
        {
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null) objetivo = jugadorObj.transform;
        }
        
        Vector3 angulos = transform.eulerAngles;
        rotacionY = angulos.y; 
        rotacionX = angulos.x;
    }

    private void Update()
    {
        if (objetivo == null || Mouse.current == null) return;
        
        Vector2 deltaMouse = Mouse.current.delta.ReadValue();
        rotacionY += deltaMouse.x * sensibilidadMouse * 0.1f;
        rotacionX = Mathf.Clamp(rotacionX - (deltaMouse.y * sensibilidadMouse * 0.1f), pitchMin, pitchMax);
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;
        
        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0);
        Vector3 puntoFoco = objetivo.position + Vector3.up * 1.5f;
        transform.position = Vector3.Lerp(transform.position, puntoFoco - (rotacion * Vector3.forward * distancia), velocidadSuavizado * Time.deltaTime);
        transform.LookAt(puntoFoco);
    }

    public Ray ObtenerRayoInteraccion()
    {
        if (camaraPrincipal != null)
        {
            return camaraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0);
        return new Ray(transform.position, rotacion * Vector3.forward);
    }
}