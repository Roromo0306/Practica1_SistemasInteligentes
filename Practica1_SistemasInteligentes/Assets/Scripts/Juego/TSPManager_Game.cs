using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TSPManager_Game : MonoBehaviour
{
    public static TSPManager_Game Instance;

    [Header("Referencias")]
    public TextMeshProUGUI textoResultado;
    public LineRenderer lineaOptima;
    public Transform jugador;

    [Header("ACO")]
    public int iteracionesACO = 200;
    public int numeroHormigas = 20;
    public float alphaACO = 1f;
    public float betaACO = 5f;
    public float evaporacionACO = 0.5f;
    public float qACO = 100f;
    public int semillaACO = 0;

    [Header("Datos del problema")]
    public List<City> ciudades = new List<City>();
    private List<Vector3> coordenadas = new List<Vector3>();

    [Header("Métricas del jugador")]
    public float distanciaJugador = 0f;
    private Vector3 posicionAnterior;
    public int ciudadesVisitadas = 0;

    [Header("Referencia óptima")]
    public float distanciaOptima = 0f;
    public List<int> rutaOptima = new List<int>();

    [Header("Líneas")]
    
    public LineRenderer lineaJugador;

    private bool juegoTerminado = false;
    private HashSet<City> ciudadesRegistradas = new HashSet<City>();

    private List<Vector3> puntosRutaJugador = new List<Vector3>();
    public float distanciaMinimaEntrePuntos = 0.1f;

    private City primeraCiudadVisitada = null;
    private bool todasVisitadas = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ciudades = FindObjectsOfType<City>().ToList();
        coordenadas.Clear();

        foreach (var c in ciudades)
            coordenadas.Add(c.transform.position);

        CalcularRutaOptimaACO();

        if (jugador == null)
            jugador = GameObject.FindGameObjectWithTag("Player").transform;

        posicionAnterior = jugador.position;

        if (lineaOptima != null)
        {
            lineaOptima.positionCount = 0;
        }

        if (lineaJugador != null)
        {
            lineaJugador.positionCount = 0;
            lineaJugador.useWorldSpace = true;
        }

        puntosRutaJugador.Clear();
        puntosRutaJugador.Add(jugador.position);

        if (lineaJugador != null)
        {
            lineaJugador.positionCount = 1;
            lineaJugador.SetPosition(0, jugador.position);
        }
    }

    void Update()
    {
        if (juegoTerminado) return;

        ActualizarDistanciaJugador();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Reiniciar();
        }
    }

    void ActualizarDistanciaJugador()
    {
        Vector3 actual = jugador.position;
        float delta = Vector3.Distance(posicionAnterior, actual);

        distanciaJugador += delta;

        if (delta >= distanciaMinimaEntrePuntos)
        {
            puntosRutaJugador.Add(actual);

            if (lineaJugador != null)
            {
                lineaJugador.positionCount = puntosRutaJugador.Count;
                lineaJugador.SetPositions(puntosRutaJugador.ToArray());
            }

            posicionAnterior = actual;
        }
    }

    void CalcularRutaOptimaACO()
    {
        TSP_Solver_Game_ACO solver = new TSP_Solver_Game_ACO(coordenadas);

        rutaOptima = solver.CalcularTSP_ACO(
            iteracionesACO,
            numeroHormigas,
            alphaACO,
            betaACO,
            evaporacionACO,
            qACO,
            semillaACO
        );

        distanciaOptima = solver.CalcularDistancia(rutaOptima);

        Debug.Log("Ruta óptima ACO calculada");
        Debug.Log("Distancia óptima: " + distanciaOptima);
    }

    public void RegistrarVisita(City ciudad)
    {
        // Guardar la primera ciudad visitada
        if (primeraCiudadVisitada == null)
        {
            primeraCiudadVisitada = ciudad;
        }

        // Si aún no estaba registrada, contarla
        if (!ciudadesRegistradas.Contains(ciudad))
        {
            ciudadesRegistradas.Add(ciudad);
            ciudadesVisitadas++;

            Debug.Log("Ciudad visitada: " + ciudad.name + " | Total: " + ciudadesVisitadas + "/" + ciudades.Count);

            if (ciudadesVisitadas >= ciudades.Count)
            {
                todasVisitadas = true;
                Debug.Log("Todas las ciudades visitadas. Vuelve a la primera para cerrar el circuito.");
            }

            return;
        }

        // Si ya estaban todas visitadas, solo termina al volver a la primera
        if (todasVisitadas && ciudad == primeraCiudadVisitada)
        {
            Debug.Log("Circuito cerrado volviendo a la primera ciudad.");
            FinJuego();
        }
    }

    void FinJuego()
    {
        juegoTerminado = true;

    if (lineaOptima != null)
    {
        DibujarRutaOptima();
    }

    float eficiencia = 0f;

    if (distanciaJugador > 0f)
        eficiencia = (distanciaOptima / distanciaJugador) * 100f;

    eficiencia = Mathf.Clamp(eficiencia, 0f, 100f);

    string rango = CalcularRango(eficiencia);

    Debug.Log("===== RESULTADO FINAL =====");
    Debug.Log("Distancia jugador: " + distanciaJugador);
    Debug.Log("Distancia óptima ACO: " + distanciaOptima);
    Debug.Log("Eficiencia: " + eficiencia + "%");
    Debug.Log("Rango: " + rango);

    if (textoResultado != null)
    {
        textoResultado.text =
            "Distancia jugador: " + distanciaJugador.ToString("F2") + "\n" +
            "Distancia óptima: " + distanciaOptima.ToString("F2") + "\n" +
            "Eficiencia: " + eficiencia.ToString("F2") + "%\n" +
            "Rango: " + rango;
    }

    if (LeaderboardManager.Instance != null)
    {
        LeaderboardManager.Instance.AñadirPuntuacion(distanciaJugador, eficiencia, rango);
    }
    }

    string CalcularRango(float eficiencia)
    {
        if (eficiencia >= 100f) return "S+";
        if (eficiencia >= 95f) return "S";
        if (eficiencia >= 85f) return "A";
        if (eficiencia >= 70f) return "B";
        if (eficiencia >= 50f) return "C";
        return "D";
    }

    void DibujarRutaOptima()
    {
        if (lineaOptima == null || rutaOptima == null || rutaOptima.Count == 0)
            return;

        lineaOptima.positionCount = rutaOptima.Count + 1;

        for (int i = 0; i < rutaOptima.Count; i++)
        {
            lineaOptima.SetPosition(i, coordenadas[rutaOptima[i]]);
        }

        lineaOptima.SetPosition(rutaOptima.Count, coordenadas[rutaOptima[0]]);
    }

    public void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}