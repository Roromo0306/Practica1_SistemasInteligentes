using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;

public class mTSPManager : MonoBehaviour
{
    [Header("Seleccion de algoritmo")]
    public bool usarBusquedaTabu = false;
    public bool calcularRuta = false;

    [Header("Crear Ciudad")]
    public string ArchivoTSP;
    public GameObject prefabCiudad;
    public float escala = 0.01f;

    [Header("Párametos recocido simulado")]
    public int run = 30;
    public int maxFs = 50000;
    public float temperaturalInicial = 1000f;
    public float alpha = 0.995f; //En cada paso se baja la temperatura 0.005

    [Header("Metricas")]
    public float mejorRuta = 7542;

    [Header("UI")]
    public TextMeshProUGUI textoInfo;

    float optimoConocido = 7542f;

    private List<Vector3> coordenadasCiudades = new List<Vector3>();
    private List<Transform> objetosCiudades = new List<Transform>();
    private LineRenderer lineRenderer;
    private List<LineRenderer> lineasAgentes = new List<LineRenderer>();
    private List<float> resultadosRecocido = new List<float>();
    private List<float> resultadosTabu = new List<float>();

    Color[] colores = new Color[]
{
    Color.red,
    Color.blue,
    Color.green,
    Color.yellow,
    Color.magenta
};

    public static mTSPManager Instance;
    public List<Vector3> Coordenadas => coordenadasCiudades;
    public int NumCiudades => coordenadasCiudades.Count;

    void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {

        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // CONFIGURACIÓN NECESARIA PARA QUE SE VEA
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;

        CargarArchivoTSP();

    }

    // Update is called once per frame
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
            Debug.Log("Lanzando TABU");
            StartCoroutine(EjecutarTabu());
        }
    }

    void CargarArchivoTSP()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ArchivoTSP);
        string[] lineas = File.ReadAllLines(ruta);

        foreach (string linea in lineas)
        {
            if (linea.Any(char.IsLetter) || string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            string[] partes = linea.Split(" "); //Creamos una lista de strings que tiene en cada elemento numeros que van separados por espacios en el archivo TSP
            float X = float.Parse(partes[1]);
            float Y = float.Parse(partes[2]);
            coordenadasCiudades.Add(new Vector3(X * escala, Y * escala, 0));

        }

        DibujarCiudades();
    }

    void DibujarRuta(List<int> ruta)
    {
        if (ruta == null || ruta.Count == 0)
        {
            Debug.LogError("Ruta vacía");
            return;
        }

        // Limpiar líneas anteriores
        foreach (var lr in lineasAgentes)
        {
            Destroy(lr.gameObject);
        }
        lineasAgentes.Clear();

        List<List<int>> rutasAgentes = new List<List<int>>();
        List<int> actual = new List<int>();

        foreach (int punto in ruta)
        {
            if (punto == -1)
            {
                rutasAgentes.Add(new List<int>(actual));
                actual.Clear();
            }
            else
            {
                actual.Add(punto);
            }
        }

        if (actual.Count > 0)
            rutasAgentes.Add(actual);

        Color[] colores = new Color[]
        {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta
        };

        for (int a = 0; a < rutasAgentes.Count; a++)
        {
            GameObject lineaObj = new GameObject("RutaAgente_" + a);
            LineRenderer lr = lineaObj.AddComponent<LineRenderer>();

            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = colores[a % colores.Length];
            lr.endColor = colores[a % colores.Length];

            List<int> subRuta = rutasAgentes[a];

            lr.positionCount = subRuta.Count + 2;

            // Inicio en almacén (0)
            lr.SetPosition(0, objetosCiudades[0].position);

            for (int i = 0; i < subRuta.Count; i++)
            {
                lr.SetPosition(i + 1, objetosCiudades[subRuta[i]].position);
            }

            // Volver al almacén
            lr.SetPosition(subRuta.Count + 1, objetosCiudades[0].position);

            lineasAgentes.Add(lr);
        }

        Debug.Log("Rutas dibujadas: " + rutasAgentes.Count);
    }

    void DibujarCiudades()
    {
        foreach (Vector3 pos in coordenadasCiudades)
        {
            GameObject ciudad = Instantiate(prefabCiudad, pos, Quaternion.identity);
            objetosCiudades.Add(ciudad.transform);
        }

        Debug.Log("Ciudades creadas: " + objetosCiudades.Count);


    }
    float CalcularDistancia(List<int> ruta)
    {
        float distanciaTotal = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            // Evitar índices inválidos (-1)
            if (ruta[i] == -1 || ruta[i + 1] == -1)
                continue;

            Vector3 a = coordenadasCiudades[ruta[i]];
            Vector3 b = coordenadasCiudades[ruta[i + 1]];
            distanciaTotal += Vector3.Distance(a, b);
        }

        return distanciaTotal;
    }
    IEnumerator EjecutarRecocido()
    {
        resultadosRecocido.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        float T = temperaturalInicial;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionRecocidoSimulado(maxFs, T, alpha, 50, i);

            if (solucion != null)
            {
                float distanciaActual = CalcularDistancia(solucion);
                resultadosRecocido.Add(distanciaActual);

                if (distanciaActual < mejorDistancia)
                {
                    mejorDistancia = distanciaActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                // 🔹 Métricas actualizadas correctamente
                float er = (distanciaActual - mejorDistancia) / mejorDistancia * 100f; // ER relativo a mejor hasta ahora
                float gap = (resultadosRecocido.Average() - mejorDistancia) / mejorDistancia * 100f; // GAP promedio respecto al mejor
                float dpp = DPP(resultadosRecocido); // dispersión real

                textoInfo.text =
                    $"ALGORITMO: RECOCIDO SIMULADO\n" +
                    $"Iteración: {i + 1}\n" +
                    $"Distancia: {distanciaActual:F2}\n" +
                    $"Temperatura: {T:F2}\n" +
                    $"ER: {er:F2}%\n" +
                    $"GAP: {gap:F2}%\n" +
                    $"DPP: {dpp:F2}%";

                Debug.Log($"[SA] Iter {i + 1} | Dist: {distanciaActual:F2} | ER: {er:F2}% | GAP: {gap:F2}% | DPP: {dpp:F2}% | T: {T:F2}");
            }

            T *= alpha;
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Recocido simulado finalizado. Mejor distancia: " + mejorDistancia);
    }



    IEnumerator EjecutarTabu()
    {
        resultadosTabu.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        float optimoConocido = 7542f; // referencia fija

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionBusquedaTabu(maxFs, 50, i);

            if (solucion != null)
            {
                float distanciaActual = CalcularDistancia(solucion);
                resultadosTabu.Add(distanciaActual);

                if (distanciaActual < mejorDistancia)
                {
                    mejorDistancia = distanciaActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                float referencia = mejorDistancia;

                float er = (distanciaActual - referencia) / referencia * 100f;
                float gap = (resultadosTabu.Average() - referencia) / referencia * 100f;
                float dpp = resultadosTabu.Count > 1 ? DPP(resultadosTabu) : 0f;

                textoInfo.text =
                    $"ALGORITMO: TABU SEARCH\n" +
                    $"Iteración: {i + 1}\n" +
                    $"Distancia: {distanciaActual:F2}\n" +
                    $"ER: {er:F2}%\n" +
                    $"GAP: {gap:F2}%\n" +
                    $"DPP: {dpp:F2}%";

                Debug.Log($"[TABU] Iter {i + 1} | Dist: {distanciaActual:F2} | ER: {er:F2}% | GAP: {gap:F2}% | DPP: {dpp:F2}%");
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Búsqueda Tabú finalizada. Mejor distancia: " + mejorDistancia);
    }

    float ER(List<float> longitudesRutas, float optimo) //error relativo (aplicarlo con tabu search y recocido)
        {
            float suma = 0;
            foreach (var longitud in longitudesRutas)
            {
                float er = (longitud - optimo) / optimo;
                suma += er;
            }

            return (suma / longitudesRutas.Count) * 100;
        }


    float DPP(List<float> longitudesRutas)
    {
        if (longitudesRutas.Count <= 1)
            return 0f;

        float media = longitudesRutas.Average();
        float varianza = 0f;

        foreach (var longitud in longitudesRutas)
        {
            varianza += Mathf.Pow((longitud - media), 2);
        }
        varianza /= longitudesRutas.Count;

        float desviacionEstandar = Mathf.Sqrt(varianza);
        return (desviacionEstandar / media) * 100f;
    }

    float GAP(List<float> longitudesRutas, float optimo) //(aplicarlo con tabu search y recocido)
        {
            float media = longitudesRutas.Average();
            return ((media - optimo) / optimo) * 100;
        }

        //añadir como baja el recocido simulado y lineas de convergencia
        //Añadir a un agente/personaje que recorra esas rutas

}

