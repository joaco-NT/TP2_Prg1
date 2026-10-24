using UnityEngine;
using UnityEngine.InputSystem;

/// ==========================================
/// TEMA: Arma de Aturdimiento por Raycast (Opción A)
/// ==========================================
public class ArmaAturdimiento : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN DE DISPARO ---")]
    [SerializeField] private Transform firePoint;            
    [SerializeField] private float rangoDisparo = 12f;
    [SerializeField] private float duracionAturdimiento = 4f;
    [SerializeField] private int dañoDisparo = 25;
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private float tiempoEntreDisparos = 0.5f;

    private CamaraSeguimiento camaraSeguimientoScript;
    private float tiempoProximoDisparo = 0f;

    private void Start()
    {
        if (Camera.main != null)
        {
            camaraSeguimientoScript = Camera.main.GetComponent<CamaraSeguimiento>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.ObtenerEstadoActual() == GameManager.EstadoJuego.Pausa) return;

        LeerEntradaDisparo();
    }

    private void LeerEntradaDisparo()
    {
        if (Keyboard.current == null || camaraSeguimientoScript == null) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (Time.time < tiempoProximoDisparo) return;
            tiempoProximoDisparo = Time.time + tiempoEntreDisparos;

            DispararRayo();
        }
    }

    private void DispararRayo()
    {
        Ray rayoDisparo = camaraSeguimientoScript.ObtenerRayoInteraccion();
        Vector3 origenRayo = (firePoint != null) ? firePoint.position : rayoDisparo.origin;
        
        Debug.DrawRay(origenRayo, rayoDisparo.direction * rangoDisparo, Color.magenta, 2f);

        if (Physics.Raycast(rayoDisparo, out RaycastHit hit, rangoDisparo, capaEnemigos))
        {
            DronCentinela dron = hit.collider.GetComponentInParent<DronCentinela>();
            if (dron != null)
            {
                if (dron.EstaCongelado())
                {
                    Debug.Log("[ArmaAturdimiento] ⚠️ <b>¡El Dron ya está congelado!</b>");
                    return;
                }

                dron.AturdirDron(duracionAturdimiento, dañoDisparo);
            }
        }
    }
}