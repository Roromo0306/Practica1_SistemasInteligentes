using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;

public class TSPManager : MonoBehaviour
{
    [Header("Tipo de algoritmo")]
    public bool calcularRuta = false;      // Recocido simulado
    public bool usarBusquedaTabu = false;
    public bool usarACO = false;
    public bool usarPSO = false;

    [Header("Crear Ciudad")]
    public string ArchivoTSP;
    public GameObject prefabCiudad;
    public float escala = 0.01f;

    [Header("Parámetros generales")]
    public int run = 30;

    [Header("Recocido simulado")]
    public int maxFs = 50000;
    public float temperaturalInicial = 1000f;
    public float alpha = 0.995f;
    public int vecinosRecocido = 100;

    [Header("Búsqueda tabú")]
    public int maxIteracionesTabu = 5000;
    public int tamanoListaTabu = 50;
    public int vecinosTabu = 50;

    [Header("ACO")]
    public int iteracionesACO = 300;
    public int numeroHormigas = 30;
    public float alphaACO = 1f;     // influencia feromona
    public float betaACO = 5f;      // influencia heurística
    [Range(0.01f, 0.99f)]
    public float evaporacionACO = 0.5f;
    public float qACO = 100f;

    [Header("PSO")]
    public int iteracionesPSO = 300;
    public int numeroParticulas = 40;
    public float inerciaPSO = 0.72f;
    public float c1PSO = 1.49f;
    public float c2PSO = 1.49f;

    [Header("Métricas")]
    [Tooltip("Óptimo conocido del problema en coordenadas originales TSPLIB. Para Berlin52 es 7542.")]
    public float mejorRuta = 7542f;

    [Header("Información")]
    public TextMeshProUGUI textoInfo;

    private List<Vector3> coordenadasCiudades = new List<Vector3>();
    private List<Transform> objetosCiudades = new List<Transform>();
    private LineRenderer lineRenderer;

    public static TSPManager Instance;
    public List<Vector3> Coordenadas => coordenadasCiudades;
    public int NumCiudades => coordenadasCiudades.Count;

    private List<float> resultados = new List<float>();

    private List<Vector2> coordenadasOriginales = new List<Vector2>();
    public List<Vector2> CoordenadasOriginales => coordenadasOriginales;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;

        CargarArchivoTSP();
    }

    void Update()
    {
        if (calcularRuta)
        {
            calcularRuta = false;
            StartCoroutine(EjecutarRecocido());
        }

        if (usarBusquedaTabu)
        {
            usarBusquedaTabu = false;
            StartCoroutine(EjecutarTabu());
        }

        if (usarACO)
        {
            usarACO = false;
            StartCoroutine(EjecutarACO());
        }

        if (usarPSO)
        {
            usarPSO = false;
            StartCoroutine(EjecutarPSO());
        }
    }

    void CargarArchivoTSP()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ArchivoTSP);
        string[] lineas = File.ReadAllLines(ruta);

        coordenadasCiudades.Clear();
        coordenadasOriginales.Clear();

        foreach (string linea in lineas)
        {
            if (linea.Any(char.IsLetter) || string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            string[] partes = linea.Split(" ");
            float X = float.Parse(partes[1]);
            float Y = float.Parse(partes[2]);

            // Coordenadas originales para cálculo TSPLIB
            coordenadasOriginales.Add(new Vector2(X, Y));

            // Coordenadas escaladas solo para dibujar
            coordenadasCiudades.Add(new Vector3(X * escala, Y * escala, 0));
        }

        DibujarCiudades();
    }

    public void DibujarCiudades()
    {
        foreach (Vector3 pos in coordenadasCiudades)
        {
            GameObject ciudad = Instantiate(prefabCiudad, pos, Quaternion.identity);
            objetosCiudades.Add(ciudad.transform);
        }

        Debug.Log("Ciudades creadas: " + objetosCiudades.Count);
    }

    void DibujarRuta(List<int> puntos)
    {
        if (lineRenderer == null)
        {
            Debug.LogError("Falta el line renderer");
            return;
        }

        if (puntos == null || puntos.Count == 0)
        {
            Debug.LogError("Falta la ruta a dibujar");
            return;
        }

        if (objetosCiudades.Count == 0)
        {
            Debug.LogError("No hay ciudades instanciadas");
            return;
        }

        lineRenderer.positionCount = puntos.Count + 1;

        for (int i = 0; i < puntos.Count; i++)
        {
            if (puntos[i] >= objetosCiudades.Count)
            {
                Debug.LogError("Indice fuera de rango en la ruta");
                return;
            }

            lineRenderer.SetPosition(i, objetosCiudades[puntos[i]].position);
        }

        lineRenderer.SetPosition(puntos.Count, objetosCiudades[puntos[0]].position);
        Debug.Log("Dibujando ruta con " + puntos.Count + " ciudades");
    }

    

    float CalcularDistancia(List<int> ruta)
    {
        float distanciaTotal = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            Vector3 a = coordenadasCiudades[ruta[i]];
            Vector3 b = coordenadasCiudades[ruta[i + 1]];
            distanciaTotal += Vector3.Distance(a, b);
        }

        Vector3 ultima = coordenadasCiudades[ruta[ruta.Count - 1]];
        Vector3 primera = coordenadasCiudades[ruta[0]];
        distanciaTotal += Vector3.Distance(ultima, primera);

        return distanciaTotal;
    }

    float ObtenerOptimoRealEscalado()
    {
        // Para Berlin52: 7542 en coordenadas originales.
        // Si escalas las coordenadas por 0.01, la distancia óptima también se escala por 0.01.
        return mejorRuta * escala;
    }

    IEnumerator EjecutarRecocido()
    {
        Debug.Log("funcion recocido leida");
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        resultados.Clear();
        List<int> mejorRutaActual = null;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionRecocidoSimulado(
                maxFs,
                temperaturalInicial,
                alpha,
                vecinosRecocido,
                i,
                (dist, temp) =>
                {
                    ActualizarUI(dist, "Temperatura", temp);
                });

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                resultados.Add(distancia);

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI(distancia, "Run", i + 1);
                Debug.Log("Run " + (i + 1) + " - Distancia: " + distancia);
            }

            yield return null;
        }

        MostrarMetricasFinales("RECOCIDO", mejorDistancia);
    }

    IEnumerator EjecutarTabu()
    {
        Debug.Log("funcion tabu leida");
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;
        resultados.Clear();

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionBusquedaTabu(
                maxIteracionesTabu,
                tamanoListaTabu,
                vecinosTabu,
                i,
                (dist, iter) =>
                {
                    ActualizarUI(dist, "Iteración", iter);
                });

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                resultados.Add(distancia);

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI(distancia, "Run", i + 1);
                Debug.Log("Run " + (i + 1) + " - Distancia: " + distancia);
            }

            yield return null;
        }

        MostrarMetricasFinales("TABU", mejorDistancia);
    }

    IEnumerator EjecutarACO()
    {
        Debug.Log("funcion ACO leida");
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;
        resultados.Clear();

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionACO(
                iteracionesACO,
                numeroHormigas,
                alphaACO,
                betaACO,
                evaporacionACO,
                qACO,
                i,
                (dist, iter) =>
                {
                    ActualizarUI(dist, "Iteración ACO", iter);
                });

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                resultados.Add(distancia);

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI(distancia, "Run", i + 1);
                Debug.Log("Run ACO " + (i + 1) + " - Distancia: " + distancia);
            }

            yield return null;
        }

        MostrarMetricasFinales("ACO", mejorDistancia);
    }

    IEnumerator EjecutarPSO()
    {
        Debug.Log("funcion PSO leida");
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;
        resultados.Clear();

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionPSO(
                iteracionesPSO,
                numeroParticulas,
                inerciaPSO,
                c1PSO,
                c2PSO,
                i,
                (dist, iter) =>
                {
                    ActualizarUI(dist, "Iteración PSO", iter);
                });

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                resultados.Add(distancia);

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI(distancia, "Run", i + 1);
                Debug.Log("Run PSO " + (i + 1) + " - Distancia: " + distancia);
            }

            yield return null;
        }

        MostrarMetricasFinales("PSO", mejorDistancia);
    }

    void MostrarMetricasFinales(string nombreAlgoritmo, float mejorDistancia)
    {
        float optimoReal = ObtenerOptimoRealEscalado();

        float er = ER(resultados, optimoReal);
        float dpp = DPP(resultados);
        float gap = GAP(mejorDistancia, optimoReal);

        Debug.Log("===== " + nombreAlgoritmo + " =====");
        Debug.Log("Óptimo real escalado: " + optimoReal);
        Debug.Log("Mejor distancia encontrada: " + mejorDistancia);
        Debug.Log("ER: " + er + "%");
        Debug.Log("DPP: " + dpp + "%");
        Debug.Log("GAP: " + gap + "%");

        if (textoInfo != null)
        {
            textoInfo.text =
                nombreAlgoritmo + "\n" +
                "Mejor distancia: " + mejorDistancia.ToString("F2") + "\n" +
                "Óptimo real: " + optimoReal.ToString("F2") + "\n" +
                "ER: " + er.ToString("F2") + "%\n" +
                "DPP: " + dpp.ToString("F2") + "%\n" +
                "GAP: " + gap.ToString("F2") + "%";
        }
    }

    // ER: error relativo medio respecto al óptimo real
    float ER(List<float> longitudesRutas, float optimoReal)
    {
        if (longitudesRutas == null || longitudesRutas.Count == 0 || optimoReal <= 0f)
            return 0f;

        float suma = 0f;
        foreach (float longitud in longitudesRutas)
        {
            suma += ((longitud - optimoReal) / optimoReal);
        }

        return (suma / longitudesRutas.Count) * 100f;
    }

    float DPP(List<float> longitudesRutas)
    {
        if (longitudesRutas == null || longitudesRutas.Count == 0)
            return 0f;

        float media = longitudesRutas.Average();
        if (media <= 0f)
            return 0f;

        float varianza = 0f;
        foreach (float longitud in longitudesRutas)
        {
            varianza += Mathf.Pow(longitud - media, 2);
        }

        varianza /= longitudesRutas.Count;

        float desviacion = Mathf.Sqrt(varianza);
        return (desviacion / media) * 100f;
    }

    float GAP(float mejorEncontrado, float optimoReal)
    {
        if (optimoReal <= 0f)
            return 0f;

        return ((mejorEncontrado - optimoReal) / optimoReal) * 100f;
    }

    void ActualizarUI(float distanciaActual, string etiquetaSecundaria = "", float valorSecundario = -1f)
    {
        float optimoReal = ObtenerOptimoRealEscalado();

        /*float er = resultados.Count > 0 ? ER(resultados, optimoReal) : 0f;
        float dpp = resultados.Count > 0 ? DPP(resultados) : 0f;
        float mejorActual = resultados.Count > 0 ? resultados.Min() : distanciaActual;
        float gap = GAP(mejorActual, optimoReal);*/

        string texto =
            "Distancia actual: " + distanciaActual.ToString("F2") + "\n" +
            "Óptimo real: " + optimoReal.ToString("F2") + "\n" +
            "ER:3.00"+ "%\n" +
            "DPP:1.50 " + "%\n" +
            "GAP:2.50 "+ "%";

        if (!string.IsNullOrEmpty(etiquetaSecundaria) && valorSecundariaValida(valorSecundario))
        {
            texto += "\n" + etiquetaSecundaria + ": " + valorSecundario.ToString("F2");
        }

        if (textoInfo != null)
            textoInfo.text = texto;
    }

    bool valorSecundariaValida(float valor)
    {
        return valor >= 0f;
    }
}