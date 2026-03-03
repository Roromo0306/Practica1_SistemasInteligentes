using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class TSPManager : MonoBehaviour
{
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

    void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
      
        lineRenderer = GetComponent<LineRenderer>();
        CargarArchivoTSP();

    }

    // Update is called once per frame
    void Update()
    {
        if (calcularRuta)
        {
            TSP_Solver solver = new TSP_Solver();
           List<int> solucion = solver.SolucionRecocidoSimulado(maxFs,temperaturalInicial,alpha,20,2);
            DibujarRuta(solucion);
            calcularRuta = false;

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
            DibujarCiudades();
        }
    }

    void DibujarRuta(List<int> puntos)
    {
        if(lineRenderer == null)
        {
            Debug.LogError("Falta el line renderer");
        }

        if(puntos == null)
        {
            Debug.LogError("Falta la ruta a dibujar");
        }
        lineRenderer.positionCount = puntos.Count + 1;

        for(int i = 0; i < puntos.Count; i++)
        {
            lineRenderer.SetPosition(i, objetosCiudades[puntos[i]].position);
        }

        lineRenderer.SetPosition(puntos.Count, objetosCiudades[puntos[0]].position);
    }

    void DibujarCiudades()
    {
        foreach(Vector3 pos in coordenadasCiudades)
        {
            GameObject ciudad = Instantiate(prefabCiudad, pos, Quaternion.identity);
            objetosCiudades.Add(ciudad.transform);
        }
    }
}
