using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TSP_Solver 
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SolucionRecocidoSimulado(int maxFes,float TInicial, float alpha, int vecinos, int semilla)
    {
        int Fes = 0;
        float T = TInicial;
        System.Random rand = new System.Random(semilla);
        List<int> rutaInicial = generarRutaInicial(rand);
        float mejorLongitud = calcularLongitudTotal(rutaInicial);

        while (Fes< maxFes && T > 1e-6)
        {
            List<int> mejorCandidata = null;

            for (int n = 0; n < vecinos; n++)
            {

                List<int> candidata = generarVecino2OPT(rutaInicial, rand);
                float longitud = calcularLongitudTotal(candidata);
                if (longitud < mejorLongitud)
                {
                    mejorCandidata = candidata;
                    mejorLongitud = longitud;
                }
            }
        }
    }

    private List<int> generarVecino2OPT(List<int> recorrido, System.Random rand)
    {

        List<int> nuevoRecorrido = new List<int>(recorrido);
        int i = rand.Next(1,recorrido.Count -2);
        int j = rand.Next(i, recorrido.Count - 1);
        nuevoRecorrido.Reverse(i, j - i + 1);
        return nuevoRecorrido;
    }

    private List<int> generarRutaInicial(System.Random rand)
    {
        int numCiudades = TSPManager.Instance.NumCiudades; 
        List<int> ruta = new List<int>();

        for (int i = 0; i < numCiudades; i++)
        {
            ruta.Add(i);
        }

        for (int i = ruta.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int temp = ruta[i];
            ruta[i] = ruta[j];
            ruta[j] = temp;
        }

        return ruta;
    }

    private float calcularLongitudTotal(List<int> ruta)
    {
        float distanciaTotal = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            Vector3 a = TSPManager.Instance.Coordenadas[ruta[i]];
            Vector3 b = TSPManager.Instance.Coordenadas[ruta[i + 1]];
            distanciaTotal += Vector3.Distance(a, b);
        }

        // Volver a la ciudad inicial
        Vector3 ultima = TSPManager.Instance.Coordenadas[ruta[ruta.Count - 1]];
        Vector3 primera = TSPManager.Instance.Coordenadas[ruta[0]];
        distanciaTotal += Vector3.Distance(ultima, primera);

        return distanciaTotal;
    }
}
