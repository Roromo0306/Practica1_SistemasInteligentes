using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class mTSP_Solver
{
    private const int NUM_AGENTES = 5;

    #region ========================= RECOCIDO SIMULADO =========================

    public List<int> SolucionRecocidoSimulado(int maxFes, float TInicial, float alpha, int vecinos, int semilla)
    {
        Debug.Log("Funcion recocido mTSP leida");

        int fes = 0;
        float T = TInicial;
        System.Random rand = new System.Random(semilla);

        List<int> solucionActual = GenerarRutaInicialM(rand, NUM_AGENTES);
        float costeActual = CalcularLongitudTotalM(solucionActual);

        List<int> mejorSolucion = new List<int>(solucionActual);
        float mejorCoste = costeActual;

        while (fes < maxFes && T > 1e-6f)
        {
            for (int n = 0; n < vecinos && fes < maxFes; n++)
            {
                List<int> candidata = GenerarVecinoPorAgente(solucionActual, rand);
                float costeCandidata = CalcularLongitudTotalM(candidata);
                fes++;

                float delta = costeCandidata - costeActual;

                if (delta < 0f || rand.NextDouble() < Mathf.Exp(-delta / T))
                {
                    solucionActual = candidata;
                    costeActual = costeCandidata;

                    if (costeActual < mejorCoste)
                    {
                        mejorCoste = costeActual;
                        mejorSolucion = new List<int>(solucionActual);
                    }
                }
            }

            T *= alpha;
        }

        return Aplicar2OptFinal(mejorSolucion);
    }

    #endregion

    #region ========================= TABÚ =========================

    public List<int> SolucionBusquedaTabu(int maxIteraciones, int tamanoListaTabu, int semilla)
    {
        System.Random rand = new System.Random(semilla);

        List<int> solucionActual = GenerarRutaInicialM(rand, NUM_AGENTES);
        float costeActual = CalcularLongitudTotalM(solucionActual);

        List<int> mejorSolucion = new List<int>(solucionActual);
        float mejorCoste = costeActual;

        Queue<string> listaTabu = new Queue<string>();

        for (int iter = 0; iter < maxIteraciones; iter++)
        {
            List<int> mejorVecino = null;
            float mejorCosteVecino = float.MaxValue;
            string mejorMovimiento = "";

            for (int k = 0; k < 50; k++)
            {
                List<int> vecino = GenerarVecinoPorAgente(solucionActual, rand);
                string movimiento = string.Join(",", vecino);

                if (listaTabu.Contains(movimiento))
                    continue;

                float costeVecino = CalcularLongitudTotalM(vecino);

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
        }

        return Aplicar2OptFinal(mejorSolucion);
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
        int semilla)
    {
        System.Random rand = new System.Random(semilla);

        int n = mTSPManager.Instance.NumCiudades;
        int numClientes = n - 1; // sin el almacén 0

        float[,] distancias = ConstruirMatrizDistanciasClientes();
        float[,] feromonas = new float[numClientes, numClientes];

        for (int i = 0; i < numClientes; i++)
        {
            for (int j = 0; j < numClientes; j++)
            {
                feromonas[i, j] = (i == j) ? 0f : 1f;
            }
        }

        List<int> mejorRutaGlobal = null;
        float mejorCosteGlobal = float.MaxValue;

        for (int iter = 0; iter < iteraciones; iter++)
        {
            List<List<int>> solucionesHormigas = new List<List<int>>();
            List<float> costesHormigas = new List<float>();

            for (int h = 0; h < numeroHormigas; h++)
            {
                List<int> permutacion = ConstruirPermutacionACO(rand, feromonas, distancias, alphaFeromona, betaHeuristica);
                List<int> solucion = ConvertirPermutacionASolucionMSectorial(permutacion, NUM_AGENTES);
                solucion = Aplicar2OptFinal(solucion);

                float coste = CalcularLongitudTotalM(solucion);

                solucionesHormigas.Add(solucion);
                costesHormigas.Add(coste);

                if (coste < mejorCosteGlobal)
                {
                    mejorCosteGlobal = coste;
                    mejorRutaGlobal = new List<int>(solucion);
                }
            }

            // evaporación
            for (int i = 0; i < numClientes; i++)
            {
                for (int j = 0; j < numClientes; j++)
                {
                    feromonas[i, j] *= (1f - evaporacion);
                    feromonas[i, j] = Mathf.Max(feromonas[i, j], 0.0001f);
                }
            }

            // depósito según permutación de ciudades
            for (int h = 0; h < solucionesHormigas.Count; h++)
            {
                List<int> perm = ExtraerPermutacionCiudades(solucionesHormigas[h]);
                float delta = q / Mathf.Max(costesHormigas[h], 0.0001f);
                DepositarFeromonasClientes(feromonas, perm, delta);
            }

            if (mejorRutaGlobal != null)
            {
                List<int> mejorPerm = ExtraerPermutacionCiudades(mejorRutaGlobal);
                DepositarFeromonasClientes(feromonas, mejorPerm, (2f * q) / Mathf.Max(mejorCosteGlobal, 0.0001f));
            }
        }

        mejorRutaGlobal = ConvertirPermutacionASolucionMSectorial(ExtraerPermutacionCiudades(mejorRutaGlobal), NUM_AGENTES);
        mejorRutaGlobal = Aplicar2OptFinal(mejorRutaGlobal);
        return mejorRutaGlobal;
    }

    private List<int> ConstruirPermutacionACO(
        System.Random rand,
        float[,] feromonas,
        float[,] distancias,
        float alpha,
        float beta)
    {
        int numClientes = mTSPManager.Instance.NumCiudades - 1; // ciudades 1..N-1
        List<int> permutacion = new List<int>();
        HashSet<int> noVisitadas = new HashSet<int>();

        for (int i = 1; i <= numClientes; i++)
            noVisitadas.Add(i);

        int actual = rand.Next(1, numClientes + 1);
        permutacion.Add(actual);
        noVisitadas.Remove(actual);

        while (noVisitadas.Count > 0)
        {
            int siguiente = SeleccionarSiguienteCliente(rand, actual, noVisitadas, feromonas, distancias, alpha, beta);
            permutacion.Add(siguiente);
            noVisitadas.Remove(siguiente);
            actual = siguiente;
        }

        return permutacion;
    }

    private int SeleccionarSiguienteCliente(
        System.Random rand,
        int actualCiudad,
        HashSet<int> noVisitadas,
        float[,] feromonas,
        float[,] distancias,
        float alpha,
        float beta)
    {
        // índice de matriz = ciudad - 1
        int actual = actualCiudad - 1;

        Dictionary<int, float> pesos = new Dictionary<int, float>();
        float suma = 0f;

        foreach (int ciudad in noVisitadas)
        {
            int j = ciudad - 1;
            float tau = Mathf.Pow(feromonas[actual, j], alpha);
            float eta = Mathf.Pow(1f / Mathf.Max(distancias[actual, j], 0.0001f), beta);
            float valor = tau * eta;

            pesos[ciudad] = valor;
            suma += valor;
        }

        if (suma <= 0f)
            return noVisitadas.ElementAt(rand.Next(noVisitadas.Count));

        double r = rand.NextDouble() * suma;
        float acumulado = 0f;

        foreach (var kv in pesos)
        {
            acumulado += kv.Value;
            if (r <= acumulado)
                return kv.Key;
        }

        return pesos.Keys.Last();
    }

    private void DepositarFeromonasClientes(float[,] feromonas, List<int> permutacion, float delta)
    {
        for (int i = 0; i < permutacion.Count - 1; i++)
        {
            int a = permutacion[i] - 1;
            int b = permutacion[i + 1] - 1;
            feromonas[a, b] += delta;
            feromonas[b, a] += delta;
        }
    }

    #endregion

    #region ========================= PSO =========================

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
        int semilla)
    {
        System.Random rand = new System.Random(semilla);

        int numClientes = mTSPManager.Instance.NumCiudades - 1; // ciudades 1..N-1
        List<ParticulaPSO> enjambre = new List<ParticulaPSO>();

        float[] mejorPosicionGlobal = null;
        List<int> mejorRutaGlobal = null;
        float mejorCosteGlobal = float.MaxValue;

        // inicialización
        for (int p = 0; p < numeroParticulas; p++)
        {
            ParticulaPSO particula = new ParticulaPSO();
            particula.posicion = new float[numClientes];
            particula.velocidad = new float[numClientes];

            for (int i = 0; i < numClientes; i++)
            {
                particula.posicion[i] = (float)rand.NextDouble();
                particula.velocidad[i] = (float)(rand.NextDouble() * 0.2 - 0.1);
            }

            List<int> perm = DecodificarRandomKeysClientes(particula.posicion);
            List<int> ruta = ConvertirPermutacionASolucionMSectorial(perm, NUM_AGENTES);
            ruta = Aplicar2OptFinal(ruta);
            float coste = CalcularLongitudTotalM(ruta);

            particula.mejorPosicion = (float[])particula.posicion.Clone();
            particula.mejorRuta = new List<int>(ruta);
            particula.mejorCoste = coste;

            if (coste < mejorCosteGlobal)
            {
                mejorCosteGlobal = coste;
                mejorRutaGlobal = new List<int>(ruta);
                mejorPosicionGlobal = (float[])particula.mejorPosicion.Clone();
            }

            enjambre.Add(particula);
        }

        // iteraciones
        for (int iter = 0; iter < iteraciones; iter++)
        {
            foreach (ParticulaPSO particula in enjambre)
            {
                for (int d = 0; d < numClientes; d++)
                {
                    float r1 = (float)rand.NextDouble();
                    float r2 = (float)rand.NextDouble();

                    particula.velocidad[d] =
                        inercia * particula.velocidad[d] +
                        c1 * r1 * (particula.mejorPosicion[d] - particula.posicion[d]) +
                        c2 * r2 * (mejorPosicionGlobal[d] - particula.posicion[d]);

                    particula.posicion[d] += particula.velocidad[d];
                }

                List<int> perm = DecodificarRandomKeysClientes(particula.posicion);
                List<int> ruta = ConvertirPermutacionASolucionM(perm, NUM_AGENTES);
                float coste = CalcularLongitudTotalM(ruta);

                if (coste < particula.mejorCoste)
                {
                    particula.mejorCoste = coste;
                    particula.mejorRuta = new List<int>(ruta);
                    particula.mejorPosicion = (float[])particula.posicion.Clone();
                }

                if (particula.mejorCoste < mejorCosteGlobal)
                {
                    mejorCosteGlobal = particula.mejorCoste;
                    mejorRutaGlobal = new List<int>(particula.mejorRuta);
                    mejorPosicionGlobal = (float[])particula.mejorPosicion.Clone();
                }
            }
        }

        return Aplicar2OptFinal(mejorRutaGlobal);
    }

    private List<int> DecodificarRandomKeysClientes(float[] keys)
    {
        List<KeyValuePair<int, float>> pares = new List<KeyValuePair<int, float>>();

        // ciudad real = índice + 1
        for (int i = 0; i < keys.Length; i++)
            pares.Add(new KeyValuePair<int, float>(i + 1, keys[i]));

        pares.Sort((a, b) => a.Value.CompareTo(b.Value));

        List<int> permutacion = new List<int>();
        foreach (var p in pares)
            permutacion.Add(p.Key);

        return permutacion;
    }

    #endregion

    #region ========================= CONVERSIÓN Y VECINOS =========================

    public List<int> GenerarRutaInicialM(System.Random rand, int numAgentes = 1)
    {
        int numCiudades = mTSPManager.Instance.NumCiudades;
        List<int> ruta = new List<int>();

        List<int> ciudades = new List<int>();
        for (int i = 1; i < numCiudades; i++)
            ciudades.Add(i);

        Vector3 almacen = mTSPManager.Instance.Coordenadas[0];
        ciudades.Sort((a, b) =>
        {
            Vector3 dirA = mTSPManager.Instance.Coordenadas[a] - almacen;
            Vector3 dirB = mTSPManager.Instance.Coordenadas[b] - almacen;
            float angleA = Mathf.Atan2(dirA.y, dirA.x);
            float angleB = Mathf.Atan2(dirB.y, dirB.x);
            return angleA.CompareTo(angleB);
        });

        // pequeña aleatorización sin romper estructura
        for (int i = ciudades.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int tmp = ciudades[i];
            ciudades[i] = ciudades[j];
            ciudades[j] = tmp;
        }

        return ConvertirPermutacionASolucionM(ciudades, numAgentes);
    }

    private List<int> ConvertirPermutacionASolucionM(List<int> permutacion, int numAgentes)
    {
        List<List<int>> subRutas = new List<List<int>>();

        for (int i = 0; i < numAgentes; i++)
            subRutas.Add(new List<int>());

        foreach (int ciudad in permutacion)
        {
            int mejorAgente = 0;
            float mejorCosteIncremental = float.MaxValue;

            for (int ag = 0; ag < numAgentes; ag++)
            {
                float incremento = CosteInsertarAlFinal(subRutas[ag], ciudad);

                // pequeña penalización si ese agente ya va cargado
                float costeBalance = CalcularLongitudSubRuta(subRutas[ag]);

                float score = incremento + 0.2f * costeBalance;

                if (score < mejorCosteIncremental)
                {
                    mejorCosteIncremental = score;
                    mejorAgente = ag;
                }
            }

            subRutas[mejorAgente].Add(ciudad);
        }

        for (int i = 0; i < subRutas.Count; i++)
        {
            OrdenarSubRutaPorAngulo(subRutas[i]);
        }

        return ReconstruirConSeparadores(subRutas);
    }
    private List<int> ConvertirPermutacionASolucionMSectorial(List<int> permutacion, int numAgentes)
    {
        Vector3 almacen = mTSPManager.Instance.Coordenadas[0];
        List<List<int>> subRutas = new List<List<int>>();

        for (int i = 0; i < numAgentes; i++)
            subRutas.Add(new List<int>());

        foreach (int ciudad in permutacion)
        {
            Vector3 dir = mTSPManager.Instance.Coordenadas[ciudad] - almacen;
            float angulo = Mathf.Atan2(dir.y, dir.x);

            if (angulo < 0f)
                angulo += 2f * Mathf.PI;

            int sector = Mathf.FloorToInt((angulo / (2f * Mathf.PI)) * numAgentes);
            sector = Mathf.Clamp(sector, 0, numAgentes - 1);

            subRutas[sector].Add(ciudad);
        }

        for (int i = 0; i < subRutas.Count; i++)
            OrdenarSubRutaPorAngulo(subRutas[i]);

        return ReconstruirConSeparadores(subRutas);
    }
    private List<int> ExtraerPermutacionCiudades(List<int> solucion)
    {
        List<int> ciudades = new List<int>();
        foreach (int v in solucion)
        {
            if (v != -1)
                ciudades.Add(v);
        }
        return ciudades;
    }

    private float CosteInsertarAlFinal(List<int> subRuta, int ciudad)
    {
        int almacen = 0;

        if (subRuta.Count == 0)
        {
            return Distancia(almacen, ciudad) + Distancia(ciudad, almacen);
        }

        int ultima = subRuta[subRuta.Count - 1];

        // quitar regreso anterior al almacén y añadir ciudad nueva
        float costeAnterior = Distancia(ultima, almacen);
        float costeNuevo = Distancia(ultima, ciudad) + Distancia(ciudad, almacen);

        return costeNuevo - costeAnterior;
    }

    private List<int> GenerarVecinoPorAgente(List<int> ruta, System.Random rand)
    {
        List<List<int>> subRutas = SepararSubRutas(ruta);

        int movimiento = rand.Next(3);

        // 0 = 2-opt en un agente
        // 1 = swap entre dos agentes
        // 2 = mover ciudad de un agente a otro
        if (movimiento == 0)
        {
            int agente = rand.Next(subRutas.Count);
            if (subRutas[agente].Count > 2)
            {
                int i = rand.Next(0, subRutas[agente].Count - 1);
                int j = rand.Next(i, subRutas[agente].Count);
                subRutas[agente].Reverse(i, j - i + 1);
            }
        }
        else if (movimiento == 1)
        {
            int a = rand.Next(subRutas.Count);
            int b = rand.Next(subRutas.Count);

            if (a != b && subRutas[a].Count > 0 && subRutas[b].Count > 0)
            {
                int ia = rand.Next(subRutas[a].Count);
                int ib = rand.Next(subRutas[b].Count);

                int temp = subRutas[a][ia];
                subRutas[a][ia] = subRutas[b][ib];
                subRutas[b][ib] = temp;
            }
        }
        else
        {
            int from = rand.Next(subRutas.Count);
            int to = rand.Next(subRutas.Count);

            if (from != to && subRutas[from].Count > 1)
            {
                int idx = rand.Next(subRutas[from].Count);
                int ciudad = subRutas[from][idx];
                subRutas[from].RemoveAt(idx);

                int insert = subRutas[to].Count == 0 ? 0 : rand.Next(subRutas[to].Count + 1);
                subRutas[to].Insert(insert, ciudad);
            }
        }

        return ReconstruirConSeparadores(subRutas);
    }

    private void OrdenarSubRutaPorAngulo(List<int> subRuta)
    {
        Vector3 almacen = mTSPManager.Instance.Coordenadas[0];

        subRuta.Sort((a, b) =>
        {
            Vector3 dirA = mTSPManager.Instance.Coordenadas[a] - almacen;
            Vector3 dirB = mTSPManager.Instance.Coordenadas[b] - almacen;

            float angleA = Mathf.Atan2(dirA.y, dirA.x);
            float angleB = Mathf.Atan2(dirB.y, dirB.x);

            return angleA.CompareTo(angleB);
        });
    }
    private List<List<int>> SepararSubRutas(List<int> ruta)
    {
        List<List<int>> subRutas = new List<List<int>>();
        List<int> actual = new List<int>();

        foreach (int punto in ruta)
        {
            if (punto == -1)
            {
                subRutas.Add(new List<int>(actual));
                actual.Clear();
            }
            else
            {
                actual.Add(punto);
            }
        }

        subRutas.Add(new List<int>(actual));

        while (subRutas.Count < NUM_AGENTES)
            subRutas.Add(new List<int>());

        return subRutas;
    }

    private List<int> ReconstruirConSeparadores(List<List<int>> subRutas)
    {
        List<int> resultado = new List<int>();

        for (int i = 0; i < NUM_AGENTES; i++)
        {
            resultado.AddRange(subRutas[i]);
            if (i < NUM_AGENTES - 1)
                resultado.Add(-1);
        }

        return resultado;
    }

    #endregion

    #region ========================= 2-OPT =========================

    private List<int> Aplicar2OptFinal(List<int> ruta)
    {
        List<List<int>> subRutas = SepararSubRutas(ruta);

        for (int r = 0; r < subRutas.Count; r++)
        {
            bool mejora = true;

            while (mejora)
            {
                mejora = false;
                float mejorDist = CalcularLongitudSubRuta(subRutas[r]);

                for (int i = 0; i < subRutas[r].Count - 2; i++)
                {
                    for (int j = i + 1; j < subRutas[r].Count - 1; j++)
                    {
                        List<int> nueva = new List<int>(subRutas[r]);
                        nueva.Reverse(i, j - i + 1);

                        float nuevaDist = CalcularLongitudSubRuta(nueva);

                        if (nuevaDist < mejorDist)
                        {
                            subRutas[r] = nueva;
                            mejorDist = nuevaDist;
                            mejora = true;
                        }
                    }
                }
            }
        }

        return ReconstruirConSeparadores(subRutas);
    }

    private float CalcularLongitudSubRuta(List<int> ruta)
    {
        if (ruta == null || ruta.Count == 0)
            return 0f;

        float distancia = 0f;
        int almacen = 0;

        distancia += Distancia(almacen, ruta[0]);

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            distancia += Distancia(ruta[i], ruta[i + 1]);
        }

        distancia += Distancia(ruta[ruta.Count - 1], almacen);

        return distancia;
    }

    #endregion

    #region ========================= COSTE =========================

    private float CalcularLongitudTotalM(List<int> ruta, float pesoMax = 1.2f, float pesoBalance = 1.0f)
    {
        List<List<int>> subRutas = SepararSubRutas(ruta);

        float distanciaTotal = 0f;
        float distanciaMaxAgente = 0f;
        float distanciaMinAgente = float.MaxValue;

        List<float> distanciasAgentes = new List<float>();

        foreach (List<int> subRuta in subRutas)
        {
            float dist = CalcularLongitudSubRuta(subRuta);
            distanciasAgentes.Add(dist);
            distanciaTotal += dist;

            if (dist > distanciaMaxAgente)
                distanciaMaxAgente = dist;

            if (dist < distanciaMinAgente)
                distanciaMinAgente = dist;
        }

        float desequilibrio = distanciaMaxAgente - distanciaMinAgente;

        return distanciaTotal + pesoMax * distanciaMaxAgente + pesoBalance * desequilibrio;
    }

    private float Distancia(int idxA, int idxB)
    {
        Vector3 a = mTSPManager.Instance.Coordenadas[idxA];
        Vector3 b = mTSPManager.Instance.Coordenadas[idxB];
        return Vector3.Distance(a, b);
    }

    private float[,] ConstruirMatrizDistanciasClientes()
    {
        int numClientes = mTSPManager.Instance.NumCiudades - 1;
        float[,] dist = new float[numClientes, numClientes];

        for (int i = 0; i < numClientes; i++)
        {
            for (int j = 0; j < numClientes; j++)
            {
                if (i == j)
                {
                    dist[i, j] = 0f;
                }
                else
                {
                    int ciudadA = i + 1;
                    int ciudadB = j + 1;
                    dist[i, j] = Distancia(ciudadA, ciudadB);
                }
            }
        }

        return dist;
    }

    #endregion
}