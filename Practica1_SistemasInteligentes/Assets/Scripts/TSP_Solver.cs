using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSP_Solver
{
    #region ========================= RECOCIDO SIMULADO =========================

    public List<int> SolucionRecocidoSimulado(
        int maxFes,
        float TInicial,
        float alpha,
        int vecinos,
        int semilla,
        System.Action<float, float> onUpdate)
    {
        Debug.Log("Funcion recocido leida");

        int Fes = 0;
        float T = TInicial;
        System.Random rand = new System.Random(semilla);

        List<int> rutaActual = GenerarRutaInicial(rand);
        float longitudActual = CalcularLongitudTotal(rutaActual);

        float mejorLongitud = longitudActual;
        List<int> mejorRuta = new List<int>(rutaActual);

        while (Fes < maxFes && T > 1e-6f)
        {
            for (int n = 0; n < vecinos && Fes < maxFes; n++)
            {
                List<int> candidata = GenerarVecino2OPT(rutaActual, rand);
                float longitud = CalcularLongitudTotal(candidata);
                Fes++;

                float delta = longitud - longitudActual;

                if (delta < 0 || rand.NextDouble() < Mathf.Exp(-delta / T))
                {
                    rutaActual = candidata;
                    longitudActual = longitud;

                    if (longitudActual < mejorLongitud)
                    {
                        mejorLongitud = longitudActual;
                        mejorRuta = new List<int>(rutaActual);
                    }
                }
            }

            onUpdate?.Invoke(mejorLongitud, T);
            T *= alpha;
        }

        mejorRuta = Aplicar2OptFinal(mejorRuta);
        return mejorRuta;
    }

    #endregion

    #region ========================= BÚSQUEDA TABÚ =========================

    public List<int> SolucionBusquedaTabu(
        int maxIteraciones,
        int tamanoListaTabu,
        int vecinosPorIteracion,
        int semilla,
        System.Action<float, float> onUpdate = null)
    {
        System.Random rand = new System.Random(semilla);

        List<int> solucionActual = GenerarRutaInicial(rand);
        float costeActual = CalcularLongitudTotal(solucionActual);

        List<int> mejorSolucion = new List<int>(solucionActual);
        float mejorCoste = costeActual;

        Queue<string> listaTabu = new Queue<string>();

        for (int iter = 0; iter < maxIteraciones; iter++)
        {
            List<int> mejorVecino = null;
            float mejorCosteVecino = float.MaxValue;
            string mejorMovimiento = "";

            for (int k = 0; k < vecinosPorIteracion; k++)
            {
                int i = rand.Next(1, solucionActual.Count - 2);
                int j = rand.Next(i, solucionActual.Count - 1);

                List<int> vecino = new List<int>(solucionActual);
                vecino.Reverse(i, j - i + 1);

                string movimiento = i + "-" + j;

                if (listaTabu.Contains(movimiento))
                    continue;

                float costeVecino = CalcularLongitudTotal(vecino);

                if (costeVecino < mejorCosteVecino)
                {
                    mejorVecino = vecino;
                    mejorCosteVecino = costeVecino;
                    mejorMovimiento = movimiento;
                }
            }

            if (mejorVecino == null)
                break;

            solucionActual = mejorVecino;
            costeActual = mejorCosteVecino;

            listaTabu.Enqueue(mejorMovimiento);
            if (listaTabu.Count > tamanoListaTabu)
                listaTabu.Dequeue();

            if (costeActual < mejorCoste)
            {
                mejorSolucion = new List<int>(solucionActual);
                mejorCoste = costeActual;
            }

            onUpdate?.Invoke(mejorCoste, iter + 1);
        }

        mejorSolucion = Aplicar2OptFinal(mejorSolucion);
        return mejorSolucion;
    }

    #endregion

    #region ========================= ACO =========================

    public List<int> SolucionACO(
        int iteraciones,
        int numeroHormigas,
        float alphaFeromona,
        float betaHeuristica,
        float evaporacion,
        float q,
        int semilla,
        System.Action<float, float> onUpdate = null)
    {
        System.Random rand = new System.Random(semilla);
        int n = TSPManager.Instance.NumCiudades;

        float[,] distancias = ConstruirMatrizDistancias();
        float[,] feromonas = new float[n, n];

        float tau0 = 1f;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                feromonas[i, j] = (i == j) ? 0f : tau0;
            }
        }

        List<int> mejorRutaGlobal = null;
        float mejorLongitudGlobal = float.MaxValue;

        for (int iter = 0; iter < iteraciones; iter++)
        {
            List<List<int>> rutasHormigas = new List<List<int>>();
            List<float> longitudesHormigas = new List<float>();

            for (int h = 0; h < numeroHormigas; h++)
            {
                List<int> ruta = ConstruirRutaHormiga(rand, feromonas, distancias, alphaFeromona, betaHeuristica);
                float longitud = CalcularLongitudTotal(ruta);

                rutasHormigas.Add(ruta);
                longitudesHormigas.Add(longitud);

                if (longitud < mejorLongitudGlobal)
                {
                    mejorLongitudGlobal = longitud;
                    mejorRutaGlobal = new List<int>(ruta);
                }
            }

            // Evaporación
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    feromonas[i, j] *= (1f - evaporacion);
                    feromonas[i, j] = Mathf.Max(feromonas[i, j], 0.0001f);
                }
            }

            // Depósito de feromona
            for (int h = 0; h < rutasHormigas.Count; h++)
            {
                float delta = q / longitudesHormigas[h];
                DepositarFeromonas(feromonas, rutasHormigas[h], delta);
            }

            // Refuerzo extra al mejor global
            if (mejorRutaGlobal != null)
            {
                float deltaBest = (q * 2f) / mejorLongitudGlobal;
                DepositarFeromonas(feromonas, mejorRutaGlobal, deltaBest);
            }

            onUpdate?.Invoke(mejorLongitudGlobal, iter + 1);
        }

        mejorRutaGlobal = Aplicar2OptFinal(mejorRutaGlobal);
        return mejorRutaGlobal;
    }

    private List<int> ConstruirRutaHormiga(
        System.Random rand,
        float[,] feromonas,
        float[,] distancias,
        float alpha,
        float beta)
    {
        int n = TSPManager.Instance.NumCiudades;
        List<int> ruta = new List<int>();
        HashSet<int> noVisitadas = new HashSet<int>();

        for (int i = 0; i < n; i++)
            noVisitadas.Add(i);

        int actual = rand.Next(n);
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

    private int SeleccionarSiguienteCiudad(
        System.Random rand,
        int actual,
        HashSet<int> noVisitadas,
        float[,] feromonas,
        float[,] distancias,
        float alpha,
        float beta)
    {
        float suma = 0f;
        Dictionary<int, float> probabilidades = new Dictionary<int, float>();

        foreach (int ciudad in noVisitadas)
        {
            float tau = Mathf.Pow(feromonas[actual, ciudad], alpha);
            float eta = Mathf.Pow(1f / Mathf.Max(distancias[actual, ciudad], 0.0001f), beta);
            float valor = tau * eta;

            probabilidades[ciudad] = valor;
            suma += valor;
        }

        if (suma <= 0f)
        {
            int indiceAleatorio = rand.Next(noVisitadas.Count);
            return noVisitadas.ElementAt(indiceAleatorio);
        }

        double r = rand.NextDouble() * suma;
        float acumulado = 0f;

        foreach (var kvp in probabilidades)
        {
            acumulado += kvp.Value;
            if (r <= acumulado)
                return kvp.Key;
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

    #endregion

    #region ========================= PSO (RANDOM KEYS) =========================

    private class ParticulaPSO
    {
        public float[] posicion;
        public float[] velocidad;
        public float[] mejorPosicion;
        public List<int> mejorRuta;
        public float mejorCoste;
    }

    public List<int> SolucionPSO(
        int iteraciones,
        int numeroParticulas,
        float inercia,
        float c1,
        float c2,
        int semilla,
        System.Action<float, float> onUpdate = null)
    {
        System.Random rand = new System.Random(semilla);
        int n = TSPManager.Instance.NumCiudades;

        List<ParticulaPSO> enjambre = new List<ParticulaPSO>();
        List<int> mejorRutaGlobal = null;
        float[] mejorPosicionGlobal = null;
        float mejorCosteGlobal = float.MaxValue;

        // Inicialización
        for (int p = 0; p < numeroParticulas; p++)
        {
            ParticulaPSO particula = new ParticulaPSO();
            particula.posicion = new float[n];
            particula.velocidad = new float[n];

            for (int i = 0; i < n; i++)
            {
                particula.posicion[i] = (float)rand.NextDouble();
                particula.velocidad[i] = UnityEngine.Random.Range(-0.1f, 0.1f);
            }

            List<int> rutaInicial = DecodificarRandomKeys(particula.posicion);
            float costeInicial = CalcularLongitudTotal(rutaInicial);

            particula.mejorPosicion = (float[])particula.posicion.Clone();
            particula.mejorRuta = new List<int>(rutaInicial);
            particula.mejorCoste = costeInicial;

            if (costeInicial < mejorCosteGlobal)
            {
                mejorCosteGlobal = costeInicial;
                mejorRutaGlobal = new List<int>(rutaInicial);
                mejorPosicionGlobal = (float[])particula.posicion.Clone();
            }

            enjambre.Add(particula);
        }

        // Iteraciones PSO
        for (int iter = 0; iter < iteraciones; iter++)
        {
            foreach (ParticulaPSO particula in enjambre)
            {
                for (int d = 0; d < n; d++)
                {
                    float r1 = (float)rand.NextDouble();
                    float r2 = (float)rand.NextDouble();

                    particula.velocidad[d] =
                        inercia * particula.velocidad[d] +
                        c1 * r1 * (particula.mejorPosicion[d] - particula.posicion[d]) +
                        c2 * r2 * (mejorPosicionGlobal[d] - particula.posicion[d]);

                    particula.posicion[d] += particula.velocidad[d];
                }

                List<int> rutaActual = DecodificarRandomKeys(particula.posicion);
                float costeActual = CalcularLongitudTotal(rutaActual);

                if (costeActual < particula.mejorCoste)
                {
                    particula.mejorCoste = costeActual;
                    particula.mejorRuta = new List<int>(rutaActual);
                    particula.mejorPosicion = (float[])particula.posicion.Clone();
                }

                if (costeActual < mejorCosteGlobal)
                {
                    mejorCosteGlobal = costeActual;
                    mejorRutaGlobal = new List<int>(rutaActual);
                    mejorPosicionGlobal = (float[])particula.posicion.Clone();
                }
            }

            onUpdate?.Invoke(mejorCosteGlobal, iter + 1);
        }

        mejorRutaGlobal = Aplicar2OptFinal(mejorRutaGlobal);
        return mejorRutaGlobal;
    }

    private List<int> DecodificarRandomKeys(float[] claves)
    {
        List<KeyValuePair<int, float>> pares = new List<KeyValuePair<int, float>>();

        for (int i = 0; i < claves.Length; i++)
            pares.Add(new KeyValuePair<int, float>(i, claves[i]));

        pares.Sort((a, b) => a.Value.CompareTo(b.Value));

        List<int> ruta = new List<int>();
        foreach (var par in pares)
            ruta.Add(par.Key);

        return ruta;
    }

    #endregion

    #region ========================= UTILIDADES =========================

    private List<int> Aplicar2OptFinal(List<int> ruta)
    {
        if (ruta == null || ruta.Count < 4)
            return ruta;

        bool mejora = true;

        while (mejora)
        {
            mejora = false;
            float distanciaActual = CalcularLongitudTotal(ruta);

            for (int i = 1; i < ruta.Count - 2; i++)
            {
                for (int j = i + 1; j < ruta.Count - 1; j++)
                {
                    List<int> nuevaRuta = new List<int>(ruta);
                    nuevaRuta.Reverse(i, j - i + 1);

                    float nuevaDistancia = CalcularLongitudTotal(nuevaRuta);

                    if (nuevaDistancia < distanciaActual)
                    {
                        ruta = nuevaRuta;
                        distanciaActual = nuevaDistancia;
                        mejora = true;
                    }
                }
            }
        }

        return ruta;
    }

    private List<int> GenerarVecino2OPT(List<int> recorrido, System.Random rand)
    {
        List<int> nuevoRecorrido = new List<int>(recorrido);
        int i = rand.Next(1, recorrido.Count - 2);
        int j = rand.Next(i, recorrido.Count - 1);
        nuevoRecorrido.Reverse(i, j - i + 1);
        return nuevoRecorrido;
    }

    private List<int> GenerarRutaInicial(System.Random rand)
    {
        int numCiudades = TSPManager.Instance.NumCiudades;
        List<int> ruta = new List<int>();

        for (int i = 0; i < numCiudades; i++)
            ruta.Add(i);

        for (int i = ruta.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int temp = ruta[i];
            ruta[i] = ruta[j];
            ruta[j] = temp;
        }

        return ruta;
    }

    private float CalcularLongitudTotal(List<int> ruta)
    {
        float distanciaTotal = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            distanciaTotal += DistanciaBerlin52(ruta[i], ruta[i + 1]);
        }

        distanciaTotal += DistanciaBerlin52(ruta[ruta.Count - 1], ruta[0]);

        return distanciaTotal;
    }
    private float DistanciaBerlin52(int i, int j)
    {
        Vector2 a = TSPManager.Instance.CoordenadasOriginales[i];
        Vector2 b = TSPManager.Instance.CoordenadasOriginales[j];

        float dx = a.x - b.x;
        float dy = a.y - b.y;

        // TSPLIB EUC_2D
        return Mathf.Round(Mathf.Sqrt(dx * dx + dy * dy));
    }
    private float[,] ConstruirMatrizDistancias()
    {
        int n = TSPManager.Instance.NumCiudades;
        float[,] dist = new float[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j) dist[i, j] = 0f;
                else
                {
                    Vector3 a = TSPManager.Instance.Coordenadas[i];
                    Vector3 b = TSPManager.Instance.Coordenadas[j];
                    dist[i, j] = Vector3.Distance(a, b);
                }
            }
        }

        return dist;
    }

    #endregion
}