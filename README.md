
# Prototipo de Videojuego en Tercera Persona - CubeGame Studios

**Título del Prototipo:** Sistema Modular de Exploración y Combate Táctico  
**Asignatura:** Programación de Videojuegos I (Trabajo Práctico Nº 2)  
**Desarrollador:** Torrez Joaquin  
**Motor Gráfico:** Unity 6.3 LTS  
**Lenguaje:** C#  

---

## 1. Descripción General del Nivel
Este documento detalla la arquitectura del código, los patrones de diseño y la implementación técnica del prototipo desarrollado para CubeGame Studios. El proyecto simula un nivel de ciencia ficción donde el jugador controla a un personaje en tercera persona. El objetivo principal consiste en explorar un escenario modelado con desniveles, recolectar cuatro ítems de datos distribuidos en el mapa, evitar peligros ambientales (como zonas de barro y gas tóxico) y cazar a un Dron Centinela autónomo gestionado por inteligencia artificial. La condición de victoria se alcanza únicamente cuando se cumplen de manera sincronizada ambos objetivos (eliminación del enemigo y recolección total).

---

## 2. Guía de Controles del Jugador

El sistema de entradas opera mediante el paquete moderno Unity Input System, garantizando una lectura limpia y libre de conflictos en tiempo de ejecución:

| Acción | Entrada / Tecla | Descripción Funcional |
| :--- | :--- | :--- |
| **Movimiento** | Teclas `W`, `A`, `S`, `D` | Desplazamiento direccional relativo a la orientación de la cámara. |
| **Salto** | Tecla `Espacio` | Impulso vertical condicionado por una validación de suelo (`Raycast`). |
| **Interacción** | Tecla `E` | Activación de elementos del entorno a distancia mediante un rayo frontal filtrado por capas. |
| **Arma de Aturdimiento** | Tecla `R` | Disparo de un haz de energía frontal (*Opción A*) para congelar y dañar al centinela. |
| **Pausa Global** | Tecla `Escape` | Congela el tiempo del motor (`Time.timeScale = 0f`) y libera el cursor del mouse. |

---

## 3. Desglose Detallado por Grupos de Consignas

### A. Requisitos Obligatorios

1. **Detección e Interacción por Raycast + LayerMask**

- **Chequeo de Suelo:** Implementado dentro del bucle de control del personaje (`MovimientoJugador.cs`). Se proyecta un rayo constante desde la posición de los pies del personaje hacia el suelo utilizando un filtro estricto por `LayerMask` (`capaSuelo`). Esto previene saltos en el aire y garantiza una detección física precisa.

- **Interacción a Distancia:** Gestionado de forma conjunta entre `CamaraSeguimiento.cs` y el componente de entrada del jugador. Al presionar la tecla de interacción, se obtiene un vector de rayo desde la perspectiva de la cámara y se evalúa su impacto utilizando `LayerMask` para limitar el procesamiento únicamente a los objetos etiquetados como interactivos.


2. **Navegación e Inteligencia Artificial con NavMesh**
- **Malla de Navegación:** El escenario cuenta con una superficie de navegación (*NavMesh*) horneada sobre el terreno, permitiendo el cálculo de rutas óptimas para agentes en movimiento.

- **Dron Centinela (`DronCentinela.cs`):** Controlador basado en una máquina de estados finitos (`Patrullando`, `Persiguiendo`, `Congelado`, `Destruido`). El agente utiliza un `NavMeshAgent` para patrullar una lista configurable de waypoints fijos. Al detectar al jugador dentro de su radio de visión (`detectionRange`), transiciona automáticamente al estado de persecución.


3. **Sistema de Salud, Daño y Puntuación**
- **Salud Centralizada:** Administrada de forma global por el script `GameManager.cs`. El jugador pierde puntos de salud al colisionar físicamente con el centinela o al interactuar con peligros del mapa.

- **Sistema de Puntuación:** Los objetos distribuidos por el escenario (`ItemColeccionable.cs`) operan mediante colisiones de tipo *Trigger*, reportando de inmediato al gestor especializado (`GestorColeccionablesOrdenado.cs`) para actualizar el marcador y el progreso general de la misión.


4. **GameManager y Control de Estados**
- **Patrón Singleton:** El `GameManager.cs` asegura una única instancia persistente en la escena encargada de coordinar las reglas del juego.

- **Máquina de Estados Global:** Controla de forma estricta los estados del flujo de juego (`Jugando`, `Pausa`, `Victoria`, `Derrota`) y los estados lógicos de la misión (`EnCurso`, `FaltaDestruirDron`, `FaltaColeccionables`, `Completada`).

- **Gestión de Tiempos y Consola:** Modifica dinámicamente `Time.timeScale` para pausar el juego y emite mensajes enriquecidos con etiquetas HTML a la consola de Unity para facilitar la depuración técnica.


5. **Modelado de Escenario con Terreno**
- **Terrain Tool:** El nivel se encuentra construido utilizando las herramientas nativas de relieve de Unity, integrando desniveles, rampas de acceso y texturas diferenciadas que cumplen con los requisitos de transitabilidad para los agentes de navegación.


---

### B. Requisitos Opcionales Seleccionados

Para potenciar el prototipo ante CubeGame Studios, se implementaron tres extensiones avanzadas:

1. **Opción A: Arma por Raycast (`ArmaAturdimiento.cs`)**

- **Implementación Técnica:** Al pulsar la tecla `R`, el sistema calcula un rayo de disparo originado en la cámara y filtrado por la capa de enemigos (`capaEnemigos`). Si impacta al Dron Centinela, invoca el método público `AturdirDron()`, el cual resta salud al enemigo y ejecuta una corrutina (`RutinaCongelamiento`) que deshabilita temporalmente el agente de navegación y activa un indicador visual de hielo sin destruir el objeto en memoria.


2. **Opción D: Zona de Desaceleración / Barro (`ZonaDesaceleracion.cs`)**
- **Implementación Técnica:** Utiliza un `BoxCollider` configurado como `isTrigger`. Al detectar la entrada del jugador (`OnTriggerEnter`), se comunica con el script de movimiento para aplicar un multiplicador reductor de velocidad y bloquear de manera condicional la acción de salto. Al salir (`OnTriggerExit`), restaura los valores predeterminados de forma limpia.


3. **Opción F: Zona de Gas Tóxico (`ZonaGasToxico.cs`)**
- **Implementación Técnica:** Delimitada mediante un volumen de colisión en modo *Trigger*. Al ingresar el jugador, se inicia una corrutina cíclica basada en bucles eficientes y retardos controlados (`yield return new WaitForSeconds`), la cual descuenta salud de forma paulatina a través del `GameManager`. Al abandonar el área, la corrutina se detiene de forma inmediata para evitar pérdidas innecesarias de rendimiento o ciclos muertos.


---

## 4. Arquitectura de Archivos y Estructura de Directorios

La organización lógica del código fuente se encuentra estructurada bajo la siguiente jerarquía de directorios en el proyecto:

```text
\Scripts\
│
├── Entorno\
│   ├── DronCentinela.cs
│   ├── ItemColeccionable.cs
│   ├── ObjetoInteractuable.cs
│   ├── ZonaDesaceleracion.cs
│   └── ZonaGasToxico.cs
│
├── Jugador\
│   ├── CamaraSeguimiento.cs
│   └── MovimientoJugador.cs
│
└── Mecanica\
    ├── ArmaAturdimiento.cs
    ├── GameManager.cs
    └── GestorColeccionablesOrdenado.cs
```

---


## 5. Procesamiento Detallado por Script

### A. Carpeta `Entorno\`

#### 1. `DronCentinela.cs`

* **Propósito:** Controlar la inteligencia artificial del enemigo autónomo mediante una máquina de estados finitos (`Patrullando`, `Persiguiendo`, `Congelado`, `Destruido`).
* **Descripción Textual:** Este script utiliza un `NavMeshAgent` para recorrer de manera secuencial una lista configurable de waypoints en el mapa. Evalúa en cada fotograma la distancia euclidiana respecto al jugador utilizando la posición del transform objetivo; si esta distancia es menor o igual al rango de detección (`detectionRange`), el dron transiciona automáticamente al estado de persecución, persiguiendo al jugador hasta superar un multiplicador de pérdida de rango. En el aspecto de combate, administra una vida interna que, al recibir daño mediante el arma de aturdimiento, desencadena una corrutina (`RutinaCongelamiento`) la cual pausa temporalmente el movimiento del agente, activa un prefab visual de hielo como hijo del transform y desactiva el efecto al transcurrir el tiempo indicado sin destruir el objeto de inmediato. Asimismo, maneja colisiones físicas directas (`OnCollisionEnter`): si choca con el jugador, le resta salud a través del `GameManager` y le aplica un vector de empuje controlado llamando al script de movimiento del personaje. Al agotarse su salud, desactiva sus renderizadores, notifica al gestor principal y culmina su ciclo de vida en la escena."

#### 2. `ItemColeccionable.cs`

* **Propósito:** Representar los elementos de datos recolectables distribuidos por el escenario.
* **Descripción Textual:** Su funcionamiento se basa en configurar automáticamente su colisionador en modo *Trigger* durante la inicialización (`Awake`). Al momento en que un objeto con la etiqueta correspondiente al jugador entra en su volumen de colisión (`OnTriggerEnter`), el script valida la existencia en escena de la instancia de `GestorColeccionablesOrdenado`. Si está presente, le notifica que este ítem específico ha sido recolectado enviando su nombre como parámetro, lo que actualiza los marcadores globales del juego. Inmediatamente después de registrar la recolección, el componente destruye la instancia del objeto en la jerarquía para simular su desaparición física del mapa.

#### 3. `ObjetoInteractuable.cs`

* **Propósito:** Gestionar el estado binario y la interacción directa de elementos del entorno (como puertas, interruptores o botones).
* **Descripción Textual:** Este componente mantiene una variable booleana interna (`estaActivado`) que representa el estado del objeto. Al ser invocado externamente a través de su método público `Interactuar()`, invierte el valor booleano actual. Dependiendo de si el resultado es verdadero o falso, el script formatea un mensaje descriptivo en la consola de Unity utilizando etiquetas HTML enriquecidas (añadiendo iconos de candados abiertos o cerrados y el nombre exacto del GameObject) para facilitar la trazabilidad y depuración visual. También incluye un método de consulta pública (`ObtenerEstado()`) que permite a otros sistemas externos verificar si el objeto se encuentra activo o inactivo en un momento dado.

#### 4. `ZonaDesaceleracion.cs` (Opción D)

* **Propósito:** Simular un área ambiental de barro que reduce la velocidad de movimiento y bloquea la acción de salto del jugador.
* **Descripción Textual:** Utiliza un componente `BoxCollider` configurado programáticamente como disparador (`isTrigger`). Cuando el script detecta mediante la colisión por etiquetas (`OnTriggerEnter`) que el jugador ha entrado en la zona, obtiene el componente `MovimientoJugador` del objeto colisionador. A continuación, invoca el método de configuración del jugador pasándole un multiplicador menor a uno (por ejemplo, `0.4f`) y un parámetro booleano para bloquear el salto, imprimiendo un aviso detallado en la consola. Al momento en que el jugador abandona el volumen físico (`OnTriggerExit`), el script vuelve a comunicarse con el controlador del personaje para restablecer el multiplicador de velocidad a su valor neutral (`1f`) y rehabilitar la capacidad de salto.

#### 5. `ZonaGasToxico.cs` (Opción F)

* **Propósito:** Generar daño ambiental continuo y periódico sobre el jugador mientras permanezca dentro de un volumen delimitado de gas tóxico.
* **Descripción Textual:** Delimitado mediante un colisionador en modo *Trigger*, este script evalúa la entrada del jugador (`OnTriggerEnter`). Al confirmar el contacto con el tag de jugador, emite un mensaje de alerta en la consola e inicia una corrutina privada si esta no se encuentra activa previamente. Dicha corrutina ejecuta un bucle infinito controlado que, mediante un temporizador basado en `yield return new WaitForSeconds(intervaloDaño)`, resta puntos de salud de forma periódica llamando directamente al método `ModificarSalud()` del `GameManager`. Cuando el jugador sale del área de influencia (`OnTriggerExit`), el método detecta la salida, emite un mensaje informativo y detiene de forma inmediata la corrutina activa (`StopCoroutine`), evitando daños innecesarios o ciclos de ejecución muertos en memoria.

---

### B. Carpeta `Jugador\`

#### 1. `CamaraSeguimiento.cs`

* **Propósito:** Controlar la perspectiva orbital de la cámara en tercera persona y proveer el rayo central de interacción para el mundo.
* **Descripción Textual:** El script captura los valores de desplazamiento del mouse utilizando el paquete moderno *Unity Input System* (`Mouse.current.delta`). Acumula estos valores para calcular las rotaciones orbitales en los ejes horizontal y vertical, aplicando límites estrictos de ángulo (*Pitch*) mediante la función `Mathf.Clamp` para evitar inversiones indeseadas de la cámara. En el método de actualización tardía (`LateUpdate`), calcula la posición ideal de la cámara situándola a una distancia configurable detrás del objetivo y aplicando una interpolación lineal suave (`Vector3.Lerp`) para suavizar el desplazamiento visual. Adicionalmente, incluye el método público `ObtenerRayoInteraccion()`, el cual proyecta un rayo desde el punto central exacto de la pantalla (`ViewportPointToRay` en las coordenadas `0.5f, 0.5f`), garantizando que las acciones de disparo o activación frontal coincidan milimétricamente con el centro exacto de la pantalla

#### 2. `MovimientoJugador.cs`

* **Propósito:** Gestionar el desplazamiento fluido, los saltos condicionados y las respuestas físicas del personaje principal mediante un controlador optimizado.
* **Descripción Textual:** Para evitar los problemas de atasco típicos de los rigidbodies en terrenos irregulares, este script se apoya en un `CharacterController`. En cada fotograma lee las entradas de teclado del *Input System* para calcular la dirección del movimiento basándose en la orientación de la cámara principal. Aplica una aceleración suave mediante `Vector3.SmoothDamp` y combina la velocidad horizontal resultante con la fuerza de gravedad vertical. Cuenta con un sistema de chequeo de suelo constante proyectando un `Raycast` hacia abajo desde la posición de los pies, lo que previene saltos en el aire y valida las restricciones impuestas por zonas ambientales (como el bloqueo de salto en el barro). Asimismo, procesa colisiones contra las paredes del escenario (`OnControllerColliderHit`) para generar efectos visuales de deslizamiento y gestiona corrutinas de retroceso cuando el personaje recibe impactos y fuerzas de empuje directas por parte del Dron Centinela.

---

### C. Carpeta `Mecanica\`

#### 1. `ArmaAturdimiento.cs` (Opción A)

* **Propósito:** Gestionar el sistema de disparo táctico por rayo láser para dañar y congelar al Dron Centinela a distancia.
* **Descripción Textual:** Este componente detecta la pulsación de la tecla asignada (`R`) a través del sistema de entradas y controla la cadencia de fuego mediante un temporizador basado en `Time.time`. Al cumplirse el tiempo de espera, ejecuta el método de disparo que obtiene el rayo central desde el script de la cámara y determina el origen físico del proyectil desde un punto de fuego opcional (`firePoint`). Utiliza la función `Physics.Raycast` filtrando exclusivamente por la capa de enemigos (`capaEnemigos`) dentro de un rango determinado. Si el rayo impacta un objeto válido, busca el componente del dron en el objeto padre; verifica que este no se encuentre previamente congelado y, de estar activo, invoca su método público `AturdirDron()` pasándole la duración del efecto y la cantidad de daño del disparo.

#### 2. `GameManager.cs`

* **Propósito:** Actuar como el núcleo global de control del flujo del juego y de la sincronización de los objetivos de la misión bajo el patrón *Singleton*.
* **Descripción Textual:** Implementa una instancia estática y persistente para administrar la máquina de estados general del juego (`Jugando`, `Pausa`, `Victoria`, `Derrota`). Al cambiar de estado, modifica de forma dinámica la escala de tiempo del motor (`Time.timeScale`) y la visibilidad del cursor del mouse. Controla la salud global máxima y actual del jugador, aplicando las condiciones de derrota si la vida llega a cero. Además, sincroniza los objetivos de la misión evaluando concurrentemente dos variables clave: el estado de destrucción del Dron Centinela y el conteo de coleccionables recolectados. Dependiendo de si faltan elementos o enemigos por destruir, actualiza el estado de la misión (`EnCurso`, `FaltaDestruirDron`, `FaltaColeccionables`, `Completada`) y activa la victoria global cuando ambos requisitos se cumplen de forma simultánea.

#### 3. `GestorColeccionablesOrdenado.cs`

* **Propósito:** Administrar el inventario, calcular de forma automática el total de objetivos del mapa y reportar el progreso de recolección.
* **Descripción Textual:** Construido también bajo el patrón *Singleton*, este gestor utiliza una lista desplegable expuesta en el Inspector (`itemsListaDesplegable`) donde se arrastran los coleccionables distribuidos por el escenario. Durante el inicio del juego (`Start`), calcula de manera automática el total de objetivos requeridos utilizando la cantidad de elementos en la lista y sincroniza esta meta directamente con el `GameManager`. Cuando un ítem individual es recolectado, el método `RegistrarRecoleccion()` incrementa el contador interno de objetos recogidos, añade el nombre del ítem a una lista de inventario dinámico en juego y avisa al administrador global para que evalúe el progreso general de la misión, emitiendo mensajes informativos en la consola sobre el estado actual de los coleccionables.
---

## 6. Capturas y Evidencias del Prototipo en Ejecución
### Captura general del juego
   ![](/Capturas/1.png)

### Reconocer los OBJETOS

### Reconocer los OBJETOS
### Elementos
   ![](/Capturas/2.png)
- **Elementos en escena:** Jugador (Naranja) | Enemigo (Rojo) | Objeto Recolectable (Verde).

---
- **Elementos en escena:** Jugador (Naranja) | Puerta (Naranja) | Zona de barro (Marrón) | Zona tóxica (Bloque transparente verde) | Objeto Recolectable (Verde).
  - **Requisitos Opcionales (Opción D):** El bloque de color marrón simula una superficie de barro que afecta el desplazamiento.
  - **Requisitos Opcionales (Opción F):** El bloque verde transparente simula un área de riesgo por zona tóxica.
  - - **Requisitos Opcionales (Opción A):** Se visualiza el momento exacto en que se activa el arma, aplicando el efecto de congelamiento sobre el enemigo.
    ![](/Capturas/3.png)
    ![](/Capturas/4.png)

### Mecánicas Implementadas (Información de la consola de Unity)

#### Mecánica: Reconocimiento de entrada por teclado (Tecla E)
- Modifica el estado actual del objeto al interactuar mediante la tecla "E" (Activado/Desactivado).
    ![](/Capturas/5.png)

#### Mecánica: Recolección de Objetos
   ![](/Capturas/6.png)
- El sistema informa en tiempo real la cantidad de objetos restantes por recolectar en el escenario.
- Si se omite la recolección obligatoria e se intenta interactuar directamente con el enemigo:
- Se muestra un aviso en consola indicando debes recolectar los elementos faltantes dispersos en el mapa para completar la misión y habilitar el estado de Victoria.
    - ![Aviso por objetivos incompletos](/Capturas/13.png)
    - ![Aviso por objetivos completados correctamente](/Capturas/8.png)

### Mecánica: Derrotar Enemigo
- Condición cumplida: recolección previa de todos los objetos requeridos.
 - ![Estado del juego - Victoria](/capturas/12.png)
    ![](/capturas/10.png)

### Mecánica: Zona Tóxica
![Se observa el descuento progresivo de la vida del jugador al ingresar a la zona de riesgo, deteniéndose al abandonarla.](/Capturas/7.png)

### Mecánica: Zona de Barro
![Se observa la reducción en la velocidad de desplazamiento del jugador al adentrarse en la zona de barro, acumulando el efecto de daño si coincide con la zona tóxica.](/Capturas/14.png)

### Mecánica: Estado de Derrota
![Se visualiza al enemigo atacando de forma continua al jugador hasta reducir sus puntos de vida a cero.](/Capturas/11.png)