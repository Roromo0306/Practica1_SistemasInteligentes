using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class TSPManager : MonoBehaviour
{
    [Header("Crear Ciudad")]
    public string ArchivoTSP;
    public GameObject prefabCiudad;
    public float escala = 0.01f;

    [Header("Párametos recocido simulado")]
    public int run = 30;
    public int maxFs = 50000;
    public float temperaturalInicial = 1000f;
    public float alpha = 0.995f; //En cada paso se baja la temperatura 0.005

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
        
    }

    void CargarArchivoTSP()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ArchivoTSP);
        string[] lineas = File.ReadAllLines(ruta);

        foreach(string linea in lineas)
        {
            if (linea.Any(char.IsLetter))
            {
                continue;
            }

            string[] partes = linea.Split(" "); //Creamos una lista de strings que tiene en cada elemento numeros que van separados por espacios en el archivo TSP
            float X = float.Parse(partes[1]);
            float Y = float.Parse(partes[2]);
            coordenadasCiudades.Add(new Vector3(X * escala, Y * escala, 0));
        }
    }
}
