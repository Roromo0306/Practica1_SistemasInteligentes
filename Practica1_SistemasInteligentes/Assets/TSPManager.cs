using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class TSPManager : MonoBehaviour
{
    [Header("Tipo de algoritmo")]
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



    private List<Vector3> coordenadasCiudades = new List<Vector3>();
    private List<Transform>  objetosCiudades = new List<Transform>();
    private LineRenderer lineRenderer;

    public static TSPManager Instance;
    public List<Vector3> Coordenadas => coordenadasCiudades;
    public int NumCiudades => coordenadasCiudades.Count;

    List<float> resultados = new List<float>();

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

            if (usarBusquedaTabu)
                StartCoroutine(EjecutarTabu());
            else
                StartCoroutine(EjecutarRecocido());
        }
    }

    void CargarArchivoTSP()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ArchivoTSP);
        string[] lineas = File.ReadAllLines(ruta);

        foreach(string linea in lineas)
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

    void DibujarCiudades()
    {
        foreach(Vector3 pos in coordenadasCiudades)
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
            Vector3 a = coordenadasCiudades[ruta[i]];
            Vector3 b = coordenadasCiudades[ruta[i + 1]];
            distanciaTotal += Vector3.Distance(a, b);
        }

        Vector3 ultima = coordenadasCiudades[ruta[ruta.Count - 1]];
        Vector3 primera = coordenadasCiudades[ruta[0]];
        distanciaTotal += Vector3.Distance(ultima, primera);

        return distanciaTotal;
    }
    IEnumerator EjecutarRecocido()
    {
        Debug.Log("funcion tabu leida");
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        resultados.Clear();
        List<int> mejorRutaActual = null;

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionRecocidoSimulado(maxFs, temperaturalInicial, alpha, 100, i);

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                Debug.Log("Distancia: " + distancia);
                resultados.Add(distancia);

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;

                    DibujarRuta(mejorRutaActual);
                }
            }

            yield return null; // espera al siguiente frame
        }

        //float optimoReal = mejorRuta;
        float optimoReal = 870f;

        Debug.Log("===== RECOCIDO =====");
        Debug.Log("ER: " + ER(resultados, optimoReal));
        Debug.Log("DPP: " + DPP(resultados));
        Debug.Log("GAP: " + GAP(resultados, optimoReal));
    }

    IEnumerator EjecutarTabu()
    {
        TSP_Solver solver = new TSP_Solver();

        float mejorDistancia = float.MaxValue;
        List<int> mejorRutaActual = null;
        resultados.Clear();

        for (int i = 0; i < run; i++)
        {
            List<int> solucion = solver.SolucionBusquedaTabu(5000, 50, i);

            if (solucion != null)
            {
                float distancia = CalcularDistancia(solucion);
                Debug.Log("Distancia: " + distancia);
                resultados.Add(distancia);


                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorRutaActual = solucion;

                    DibujarRuta(mejorRutaActual);
                }
            }

            yield return null;
        }

        //float optimoReal = mejorRuta;
        float optimoReal = 870f;

        Debug.Log("===== TABU =====");
        Debug.Log("ER: " + ER(resultados, optimoReal));
        Debug.Log("DPP: " + DPP(resultados));
        Debug.Log("GAP: " + GAP(resultados, optimoReal));
    }

    float ER(List<float> longitudesRutas, float optimoReal)
    {
        float suma = 0;
        foreach (var longitud in longitudesRutas)
        {
            suma += (longitud - optimoReal) / optimoReal;
        }
        return (suma / longitudesRutas.Count) * 100f;
    }

    float DPP(List<float> longitudesRutas)
    {
        float media = longitudesRutas.Average();
        float varianza = 0;

        foreach (var longitud in longitudesRutas)
        {
            varianza += Mathf.Pow((longitud - media), 2);
        }

        varianza /= longitudesRutas.Count;
        float desviacionEstandar = Mathf.Sqrt(varianza);

        return (desviacionEstandar / media) * 100;
    }

    float GAP(List<float> longitudesRutas, float optimo)
    {
        float media = longitudesRutas.Average();
        return ((media - optimo) / optimo) * 100;
    }

    float ObtenerFactorEscala()
    {
        return escala;
    }

   
}
