using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class TSPManager_Game : MonoBehaviour
{
    public static TSPManager_Game Instance;

    public List<City> ciudades = new List<City>();

    private List<Vector3> coordenadas = new List<Vector3>();

    public float distanciaJugador = 0f;
    private Vector3 posicionAnterior;

    public int ciudadesVisitadas = 0;

    private float distanciaOptima;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Buscar ciudades automáticamente
        ciudades = FindObjectsOfType<City>().ToList();

        foreach (var c in ciudades)
        {
            coordenadas.Add(c.transform.position);
        }

        CalcularRutaOptima();

        posicionAnterior = GameObject.FindGameObjectWithTag("Player").transform.position;
    }

    void Update()
    {
        ActualizarDistanciaJugador();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Reiniciar();
        }
    }

    void ActualizarDistanciaJugador()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        distanciaJugador += Vector3.Distance(posicionAnterior, player.position);
        posicionAnterior = player.position;
    }

    void CalcularRutaOptima()
    {
        TSP_Solver_Game solver = new TSP_Solver_Game(coordenadas);

        List<int> ruta = solver.CalcularTSP();

        distanciaOptima = solver.CalcularDistancia(ruta);

        Debug.Log("Distancia óptima: " + distanciaOptima);
    }

    public void RegistrarVisita(City ciudad)
    {
        ciudadesVisitadas++;

        if (ciudadesVisitadas >= ciudades.Count)
        {
            FinJuego();
        }
    }

    void FinJuego()
    {
        float eficiencia = (distanciaOptima / distanciaJugador) * 100f;
        string rango = CalcularRango(eficiencia);

        Debug.Log("===== RESULTADO =====");
        Debug.Log("Distancia jugador: " + distanciaJugador);
        Debug.Log("Distancia óptima: " + distanciaOptima);
        Debug.Log("Eficiencia: " + eficiencia + "%");
        Debug.Log("Rango: " + rango);

        LeaderboardManager.Instance.AñadirPuntuacion(distanciaJugador, eficiencia, rango);
    }


    string CalcularRango(float eficiencia)
    {
        if (eficiencia >= 95) return "S";
        if (eficiencia >= 85) return "A";
        if (eficiencia >= 70) return "B";
        if (eficiencia >= 50) return "C";
        return "D";
    }


    public void Reiniciar()
    {
        SceneManager.LoadScene("GameTSP");
    }
}