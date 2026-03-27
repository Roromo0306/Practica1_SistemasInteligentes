using System.Collections.Generic;
using UnityEngine;

public class TSP_Solver_Game
{
    private List<Vector3> coords;

    public TSP_Solver_Game(List<Vector3> coordenadas)
    {
        coords = coordenadas;
    }

    // MÉTODO PRINCIPAL
    public List<int> CalcularTSP()
    {
        return BusquedaTabu(500, 50);
    }

    // =========================
    // TABU SEARCH
    // =========================
    private List<int> BusquedaTabu(int maxIter, int tabuSize)
    {
        List<int> actual = GenerarRutaInicial();
        float costeActual = CalcularDistancia(actual);

        List<int> mejor = new List<int>(actual);
        float mejorCoste = costeActual;

        Queue<string> listaTabu = new Queue<string>();

        for (int iter = 0; iter < maxIter; iter++)
        {
            List<int> mejorVecino = null;
            float mejorCosteVecino = float.MaxValue;
            string mejorMovimiento = "";

            // explorar vecinos con 2-opt
            for (int k = 0; k < 50; k++)
            {
                int i = Random.Range(1, actual.Count - 2);
                int j = Random.Range(i, actual.Count - 1);

                List<int> vecino = new List<int>(actual);
                vecino.Reverse(i, j - i + 1);

                string movimiento = i + "-" + j;

                if (listaTabu.Contains(movimiento))
                    continue;

                float coste = CalcularDistancia(vecino);

                if (coste < mejorCosteVecino)
                {
                    mejorVecino = vecino;
                    mejorCosteVecino = coste;
                    mejorMovimiento = movimiento;
                }
            }

            if (mejorVecino == null)
                break;

            actual = mejorVecino;
            costeActual = mejorCosteVecino;

            listaTabu.Enqueue(mejorMovimiento);
            if (listaTabu.Count > tabuSize)
                listaTabu.Dequeue();

            if (costeActual < mejorCoste)
            {
                mejor = new List<int>(actual);
                mejorCoste = costeActual;
            }
        }

        // mejora final con 2-opt puro
        mejor = Mejorar2Opt(mejor);

        return mejor;
    }

    // =========================
    // 2-OPT PURO
    // =========================
    private List<int> Mejorar2Opt(List<int> ruta)
    {
        bool mejora = true;

        while (mejora)
        {
            mejora = false;

            for (int i = 1; i < ruta.Count - 2; i++)
            {
                for (int j = i + 1; j < ruta.Count - 1; j++)
                {
                    List<int> nueva = new List<int>(ruta);
                    nueva.Reverse(i, j - i + 1);

                    if (CalcularDistancia(nueva) < CalcularDistancia(ruta))
                    {
                        ruta = nueva;
                        mejora = true;
                    }
                }
            }
        }

        return ruta;
    }

    // =========================
    // RUTA INICIAL
    // =========================
    private List<int> GenerarRutaInicial()
    {
        List<int> ruta = new List<int>();

        for (int i = 0; i < coords.Count; i++)
            ruta.Add(i);

        for (int i = 0; i < ruta.Count; i++)
        {
            int j = Random.Range(0, ruta.Count);
            int temp = ruta[i];
            ruta[i] = ruta[j];
            ruta[j] = temp;
        }

        return ruta;
    }

    // =========================
    // DISTANCIA
    // =========================
    public float CalcularDistancia(List<int> ruta)
    {
        float total = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            total += Vector3.Distance(coords[ruta[i]], coords[ruta[i + 1]]);
        }

        total += Vector3.Distance(coords[ruta[ruta.Count - 1]], coords[ruta[0]]);

        return total;
    }
}