using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSP_Solver_Game_ACO
{
    private List<Vector3> coordenadas;
    private int numCiudades;

    public TSP_Solver_Game_ACO(List<Vector3> coords)
    {
        coordenadas = coords;
        numCiudades = coordenadas.Count;
    }

    public List<int> CalcularTSP_ACO(
        int iteraciones = 200,
        int numeroHormigas = 20,
        float alpha = 1f,
        float beta = 5f,
        float evaporacion = 0.5f,
        float q = 100f,
        int semilla = 0)
    {
        System.Random rand = new System.Random(semilla);

        float[,] distancias = ConstruirMatrizDistancias();
        float[,] feromonas = new float[numCiudades, numCiudades];

        for (int i = 0; i < numCiudades; i++)
        {
            for (int j = 0; j < numCiudades; j++)
            {
                feromonas[i, j] = (i == j) ? 0f : 1f;
            }
        }

        List<int> mejorRutaGlobal = null;
        float mejorCosteGlobal = float.MaxValue;

        for (int iter = 0; iter < iteraciones; iter++)
        {
            List<List<int>> rutas = new List<List<int>>();
            List<float> costes = new List<float>();

            for (int h = 0; h < numeroHormigas; h++)
            {
                List<int> ruta = ConstruirRuta(rand, feromonas, distancias, alpha, beta);
                float coste = CalcularDistancia(ruta);

                rutas.Add(ruta);
                costes.Add(coste);

                if (coste < mejorCosteGlobal)
                {
                    mejorCosteGlobal = coste;
                    mejorRutaGlobal = new List<int>(ruta);
                }
            }

            // Evaporación
            for (int i = 0; i < numCiudades; i++)
            {
                for (int j = 0; j < numCiudades; j++)
                {
                    feromonas[i, j] *= (1f - evaporacion);
                    feromonas[i, j] = Mathf.Max(feromonas[i, j], 0.0001f);
                }
            }

            // Depósito
            for (int h = 0; h < rutas.Count; h++)
            {
                float delta = q / Mathf.Max(costes[h], 0.0001f);
                DepositarFeromonas(feromonas, rutas[h], delta);
            }

            // Refuerzo élite
            if (mejorRutaGlobal != null)
            {
                float deltaElite = (2f * q) / Mathf.Max(mejorCosteGlobal, 0.0001f);
                DepositarFeromonas(feromonas, mejorRutaGlobal, deltaElite);
            }
        }

        mejorRutaGlobal = Aplicar2OptFinal(mejorRutaGlobal);
        return mejorRutaGlobal;
    }

    private List<int> ConstruirRuta(System.Random rand, float[,] feromonas, float[,] distancias, float alpha, float beta)
    {
        List<int> ruta = new List<int>();
        HashSet<int> noVisitadas = new HashSet<int>();

        for (int i = 0; i < numCiudades; i++)
            noVisitadas.Add(i);

        int actual = rand.Next(numCiudades);
        ruta.Add(actual);
        noVisitadas.Remove(actual);

        while (noVisitadas.Count > 0)
        {
            int siguiente = SeleccionarSiguienteCiudad(rand, actual, noVisitadas, feromonas, distancias, alpha, beta);
            ruta.Add(siguiente);
            noVisitadas.Remove(siguiente);
            actual = siguiente;
        }

        return ruta;
    }

    private int SeleccionarSiguienteCiudad(System.Random rand, int actual, HashSet<int> noVisitadas, float[,] feromonas, float[,] distancias, float alpha, float beta)
    {
        Dictionary<int, float> probabilidades = new Dictionary<int, float>();
        float suma = 0f;

        foreach (int ciudad in noVisitadas)
        {
            float tau = Mathf.Pow(feromonas[actual, ciudad], alpha);
            float eta = Mathf.Pow(1f / Mathf.Max(distancias[actual, ciudad], 0.0001f), beta);
            float valor = tau * eta;

            probabilidades[ciudad] = valor;
            suma += valor;
        }

        if (suma <= 0f)
            return noVisitadas.ElementAt(rand.Next(noVisitadas.Count));

        double r = rand.NextDouble() * suma;
        float acumulado = 0f;

        foreach (var kv in probabilidades)
        {
            acumulado += kv.Value;
            if (r <= acumulado)
                return kv.Key;
        }

        return probabilidades.Keys.Last();
    }

    private void DepositarFeromonas(float[,] feromonas, List<int> ruta, float delta)
    {
        for (int i = 0; i < ruta.Count - 1; i++)
        {
            int a = ruta[i];
            int b = ruta[i + 1];
            feromonas[a, b] += delta;
            feromonas[b, a] += delta;
        }

        int ultima = ruta[ruta.Count - 1];
        int primera = ruta[0];
        feromonas[ultima, primera] += delta;
        feromonas[primera, ultima] += delta;
    }

    public float CalcularDistancia(List<int> ruta)
    {
        float distancia = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            distancia += Vector3.Distance(coordenadas[ruta[i]], coordenadas[ruta[i + 1]]);
        }

        distancia += Vector3.Distance(coordenadas[ruta[ruta.Count - 1]], coordenadas[ruta[0]]);
        return distancia;
    }

    private float[,] ConstruirMatrizDistancias()
    {
        float[,] dist = new float[numCiudades, numCiudades];

        for (int i = 0; i < numCiudades; i++)
        {
            for (int j = 0; j < numCiudades; j++)
            {
                if (i == j) dist[i, j] = 0f;
                else dist[i, j] = Vector3.Distance(coordenadas[i], coordenadas[j]);
            }
        }

        return dist;
    }

    private List<int> Aplicar2OptFinal(List<int> ruta)
    {
        if (ruta == null || ruta.Count < 4)
            return ruta;

        bool mejora = true;

        while (mejora)
        {
            mejora = false;
            float mejorDist = CalcularDistancia(ruta);

            for (int i = 1; i < ruta.Count - 2; i++)
            {
                for (int j = i + 1; j < ruta.Count - 1; j++)
                {
                    List<int> nueva = new List<int>(ruta);
                    nueva.Reverse(i, j - i + 1);

                    float nuevaDist = CalcularDistancia(nueva);

                    if (nuevaDist < mejorDist)
                    {
                        ruta = nueva;
                        mejorDist = nuevaDist;
                        mejora = true;
                    }
                }
            }
        }

        return ruta;
    }
}