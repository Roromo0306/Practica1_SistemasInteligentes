using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;

public class mTSPManager : MonoBehaviour
{
    [Header("Selección de algoritmo")]
    public bool usarBusquedaTabu = false;
    public bool calcularRuta = false; // recocido
    public bool usarACO = false;
    public bool usarPSO = false;

    [Header("Crear Ciudad")]
    public string ArchivoTSP;
    public GameObject prefabCiudad;
    public float escala = 0.01f;

    [Header("Parámetros generales")]
    public int run = 30;
    public int numeroAgentes = 5;

    [Header("Recocido simulado")]
    public int maxFs = 50000;
    public float temperaturalInicial = 1000f;
    public float alpha = 0.995f;

    [Header("Tabú")]
    public int maxIteracionesTabu = 5000;
    public int tamanoListaTabu = 50;

    [Header("ACO")]
    public int iteracionesACO = 250;
    public int numeroHormigas = 30;
    public float alphaACO = 1f;
    public float betaACO = 5f;
    [Range(0.01f, 0.99f)]
    public float evaporacionACO = 0.5f;
    public float qACO = 100f;

    [Header("PSO")]
    public int iteracionesPSO = 250;
    public int numeroParticulas = 35;
    public float inerciaPSO = 0.72f;
    public float c1PSO = 1.49f;
    public float c2PSO = 1.49f;

    [Header("Métricas")]
    [Tooltip("Pon aquí el mejor valor conocido de TU mTSP con 5 agentes y esta función objetivo. Si no lo conoces, deja -1.")]
    public float optimoConocidoMTSP = -1f;

    [Header("UI")]
    public TextMeshProUGUI textoInfo;

    private List<Vector3> coordenadasCiudades = new List<Vector3>();
    private List<Transform> objetosCiudades = new List<Transform>();
    private List<LineRenderer> lineasAgentes = new List<LineRenderer>();

    private List<float> resultadosActuales = new List<float>();

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

    void Start()
    {
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
            Debug.Log("Lanzando TABU");
            StartCoroutine(EjecutarTabu());
        }

        if (usarACO)
        {
            usarACO = false;
            Debug.Log("Lanzando ACO");
            StartCoroutine(EjecutarACO());
        }

        if (usarPSO)
        {
            usarPSO = false;
            Debug.Log("Lanzando PSO");
            StartCoroutine(EjecutarPSO());
        }
    }

    void CargarArchivoTSP()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ArchivoTSP);
        string[] lineas = File.ReadAllLines(ruta);

        coordenadasCiudades.Clear();

        foreach (string linea in lineas)
        {
            if (linea.Any(char.IsLetter) || string.IsNullOrWhiteSpace(linea))
                continue;

            string[] partes = linea.Split(" ");
            float X = float.Parse(partes[1]);
            float Y = float.Parse(partes[2]);

            coordenadasCiudades.Add(new Vector3(X * escala, Y * escala, 0));
        }

        DibujarCiudades();
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

    void DibujarRuta(List<int> ruta)
    {
        if (ruta == null || ruta.Count == 0)
        {
            Debug.LogError("Ruta vacía");
            return;
        }

        foreach (var lr in lineasAgentes)
        {
            if (lr != null)
                Destroy(lr.gameObject);
        }
        lineasAgentes.Clear();

        List<List<int>> rutasAgentes = SepararSubRutas(ruta);

        for (int a = 0; a < rutasAgentes.Count; a++)
        {
            GameObject lineaObj = new GameObject("RutaAgente_" + a);
            LineRenderer lr = lineaObj.AddComponent<LineRenderer>();

            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = colores[a % colores.Length];
            lr.endColor = colores[a % colores.Length];
            lr.useWorldSpace = true;

            List<int> subRuta = rutasAgentes[a];

            if (subRuta.Count == 0)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, objetosCiudades[0].position);
                lr.SetPosition(1, objetosCiudades[0].position);
            }
            else
            {
                lr.positionCount = subRuta.Count + 2;
                lr.SetPosition(0, objetosCiudades[0].position);

                for (int i = 0; i < subRuta.Count; i++)
                {
                    lr.SetPosition(i + 1, objetosCiudades[subRuta[i]].position);
                }

                lr.SetPosition(subRuta.Count + 1, objetosCiudades[0].position);
            }

            lineasAgentes.Add(lr);
        }

        Debug.Log("Rutas dibujadas: " + rutasAgentes.Count);
    }

    List<List<int>> SepararSubRutas(List<int> ruta)
    {
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

        rutasAgentes.Add(new List<int>(actual));

        while (rutasAgentes.Count < numeroAgentes)
            rutasAgentes.Add(new List<int>());

        return rutasAgentes;
    }

    float CalcularDistanciaObjetivo(List<int> ruta, float pesoAgente = 0.5f)
    {
        List<List<int>> subRutas = SepararSubRutas(ruta);

        float distanciaTotal = 0f;
        float distanciaMaxAgente = 0f;

        foreach (List<int> subRuta in subRutas)
        {
            float distanciaSubRuta = CalcularLongitudSubRuta(subRuta);
            distanciaTotal += distanciaSubRuta;

            if (distanciaSubRuta > distanciaMaxAgente)
                distanciaMaxAgente = distanciaSubRuta;
        }

        return distanciaTotal + pesoAgente * distanciaMaxAgente;
    }

    float CalcularLongitudSubRuta(List<int> subRuta)
    {
        if (subRuta == null || subRuta.Count == 0)
            return 0f;

        float distancia = 0f;

        distancia += Vector3.Distance(coordenadasCiudades[0], coordenadasCiudades[subRuta[0]]);

        for (int i = 0; i < subRuta.Count - 1; i++)
        {
            distancia += Vector3.Distance(coordenadasCiudades[subRuta[i]], coordenadasCiudades[subRuta[i + 1]]);
        }

        distancia += Vector3.Distance(coordenadasCiudades[subRuta[subRuta.Count - 1]], coordenadasCiudades[0]);

        return distancia;
    }

    IEnumerator EjecutarRecocido()
    {
        resultadosActuales.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        float T = temperaturalInicial;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionRecocidoSimulado(maxFs, T, alpha, 50, i);

            if (solucion != null)
            {
                float costeActual = CalcularDistanciaObjetivo(solucion);
                resultadosActuales.Add(costeActual);

                if (costeActual < mejorDistancia)
                {
                    mejorDistancia = costeActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI("RECOCIDO SIMULADO", i + 1, costeActual, mejorDistancia, T);
                Debug.Log($"[SA] Iter {i + 1} | Coste: {costeActual:F2}");
            }

            T *= alpha;
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Recocido simulado finalizado. Mejor distancia: " + mejorDistancia);
    }

    IEnumerator EjecutarTabu()
    {
        resultadosActuales.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionBusquedaTabu(maxIteracionesTabu, tamanoListaTabu, i);

            if (solucion != null)
            {
                float costeActual = CalcularDistanciaObjetivo(solucion);
                resultadosActuales.Add(costeActual);

                if (costeActual < mejorDistancia)
                {
                    mejorDistancia = costeActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI("TABU SEARCH", i + 1, costeActual, mejorDistancia);
                Debug.Log($"[TABU] Iter {i + 1} | Coste: {costeActual:F2}");
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Búsqueda Tabú finalizada. Mejor distancia: " + mejorDistancia);
    }

    IEnumerator EjecutarACO()
    {
        resultadosActuales.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionACO(
                iteracionesACO,
                numeroHormigas,
                alphaACO,
                betaACO,
                evaporacionACO,
                qACO,
                i);

            if (solucion != null)
            {
                float costeActual = CalcularDistanciaObjetivo(solucion);
                resultadosActuales.Add(costeActual);

                if (costeActual < mejorDistancia)
                {
                    mejorDistancia = costeActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI("ACO", i + 1, costeActual, mejorDistancia);
                Debug.Log($"[ACO] Iter {i + 1} | Coste: {costeActual:F2}");
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("ACO finalizado. Mejor distancia: " + mejorDistancia);
    }

    IEnumerator EjecutarPSO()
    {
        resultadosActuales.Clear();
        mTSP_Solver solver = new mTSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionPSO(
                iteracionesPSO,
                numeroParticulas,
                inerciaPSO,
                c1PSO,
                c2PSO,
                i);

            if (solucion != null)
            {
                float costeActual = CalcularDistanciaObjetivo(solucion);
                resultadosActuales.Add(costeActual);

                if (costeActual < mejorDistancia)
                {
                    mejorDistancia = costeActual;
                    mejorRutaActual = solucion;
                    DibujarRuta(mejorRutaActual);
                }

                ActualizarUI("PSO", i + 1, costeActual, mejorDistancia);
                Debug.Log($"[PSO] Iter {i + 1} | Coste: {costeActual:F2}");
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("PSO finalizado. Mejor distancia: " + mejorDistancia);
    }

    void ActualizarUI(string algoritmo, int iteracion, float costeActual, float mejorDistancia, float temperatura = -1f)
    {
        float dpp = DPP(resultadosActuales);
        string erTexto = "N/A";
        string gapTexto = "N/A";

        if (optimoConocidoMTSP > 0f)
        {
            erTexto = ER(resultadosActuales, optimoConocidoMTSP).ToString("F2") + "%";
            gapTexto = GAP(mejorDistancia, optimoConocidoMTSP).ToString("F2") + "%";
        }

        string texto =
            $"ALGORITMO: {algoritmo}\n" +
            $"Run: {iteracion}\n" +
            $"Coste actual: {costeActual:F2}\n" +
            $"Mejor coste: {mejorDistancia:F2}\n" +
            $"ER: {erTexto}\n" +
            $"GAP: {gapTexto}\n" +
            $"DPP: {dpp:F2}%";

        if (temperatura > 0f)
            texto += $"\nTemperatura: {temperatura:F2}";

        textoInfo.text = texto;
    }

    float ER(List<float> longitudesRutas, float optimo)
    {
        if (longitudesRutas == null || longitudesRutas.Count == 0 || optimo <= 0f)
            return 0f;

        float suma = 0f;
        foreach (float longitud in longitudesRutas)
        {
            suma += ((longitud - optimo) / optimo);
        }

        return (suma / longitudesRutas.Count) * 100f;
    }

    float DPP(List<float> longitudesRutas)
    {
        if (longitudesRutas == null || longitudesRutas.Count <= 1)
            return 0f;

        float media = longitudesRutas.Average();
        if (media <= 0f)
            return 0f;

        float varianza = 0f;
        foreach (float longitud in longitudesRutas)
        {
            varianza += Mathf.Pow(longitud - media, 2f);
        }

        varianza /= longitudesRutas.Count;

        float desviacionEstandar = Mathf.Sqrt(varianza);
        return (desviacionEstandar / media) * 100f;
    }

    float GAP(float mejorEncontrado, float optimo)
    {
        if (optimo <= 0f)
            return 0f;

        return ((mejorEncontrado - optimo) / optimo) * 100f;
    }
}