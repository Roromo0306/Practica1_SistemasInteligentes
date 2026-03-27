using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mTSP_Solver
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<int> SolucionRecocidoSimulado(int maxFes, float TInicial, float alpha, int vecinos, int semilla)
    {
        Debug.Log("Funcion recocido leida");

        int Fes = 0;
        float T = TInicial;
        System.Random rand = new System.Random(semilla);
        List<int> rutaInicial = generarRutaInicialM(rand,5);
        float mejorLongitud = calcularLongitudTotalM(rutaInicial);
        List<int> mejorRuta = new List<int>(rutaInicial);
        List<int> mejorCandidata = null;

        while (Fes < maxFes && T > 1e-6)
        {
            mejorCandidata = null;

            for (int n = 0; n < vecinos; n++)
            {

                List<int> candidata = generarVecino2OPT(rutaInicial, rand);
                float longitud = calcularLongitudTotalM(candidata);
                Fes++;

                if (longitud < mejorLongitud)
                {
                    mejorCandidata = candidata;
                    mejorLongitud = longitud;
                }
            }
           
            T = T * alpha;

        }
        return aplicar2OptFinal(mejorRuta);
    }

    private List<int> generarVecino2OPT(List<int> recorrido, System.Random rand)
    {

        List<int> nuevoRecorrido = new List<int>(recorrido);
        int i = rand.Next(1, recorrido.Count - 2);
        int j = rand.Next(i, recorrido.Count - 1);
        nuevoRecorrido.Reverse(i, j - i + 1);
        return nuevoRecorrido;
    }



    private List<int> generarRutaInicialM(System.Random rand, int numAgentes = 1)
    {
        int numCiudades = mTSPManager.Instance.NumCiudades;
        List<int> ruta = new List<int>();

        // Tomar coordenadas sin el almacén (0)
        List<int> ciudades = new List<int>();
        for (int i = 1; i < numCiudades; i++) ciudades.Add(i);

        // Ordenarlas por ángulo respecto al almacén
        Vector3 almacen = mTSPManager.Instance.Coordenadas[0];
        ciudades.Sort((a, b) => {
            Vector3 dirA = mTSPManager.Instance.Coordenadas[a] - almacen;
            Vector3 dirB = mTSPManager.Instance.Coordenadas[b] - almacen;
            float angleA = Mathf.Atan2(dirA.y, dirA.x);
            float angleB = Mathf.Atan2(dirB.y, dirB.x);
            return angleA.CompareTo(angleB);
        });

        // Dividir en numAgentes grupos
        int perAgent = Mathf.CeilToInt(ciudades.Count / (float)numAgentes);
        for (int ag = 0; ag < numAgentes; ag++)
        {
            int start = ag * perAgent;
            int end = Mathf.Min(start + perAgent, ciudades.Count);
            ruta.AddRange(ciudades.GetRange(start, end - start));

            if (ag < numAgentes - 1)
                ruta.Add(-1); // separador
        }

        return ruta;
    }
    private List<int> aplicar2OptFinal(List<int> ruta)
    {
        List<List<int>> subRutas = new List<List<int>>();
        List<int> actual = new List<int>();

        // 🔹 Separar por agentes
        foreach (int punto in ruta)
        {
            if (punto == -1)
            {
                if (actual.Count > 0)
                    subRutas.Add(new List<int>(actual));

                actual.Clear();
            }
            else
            {
                actual.Add(punto);
            }
        }

        if (actual.Count > 0)
            subRutas.Add(actual);

        // 🔹 Aplicar 2-OPT a cada agente
        for (int r = 0; r < subRutas.Count; r++)
        {
            bool mejora = true;

            while (mejora)
            {
                mejora = false;

                for (int i = 0; i < subRutas[r].Count - 2; i++)
                {
                    for (int j = i + 1; j < subRutas[r].Count - 1; j++)
                    {
                        List<int> nueva = new List<int>(subRutas[r]);
                        nueva.Reverse(i, j - i + 1);

                        if (calcularLongitudSubRuta(nueva) < calcularLongitudSubRuta(subRutas[r]))
                        {
                            subRutas[r] = nueva;
                            mejora = true;
                        }
                    }
                }
            }
        }

        // 🔹 Reconstruir ruta con -1
        List<int> resultado = new List<int>();

        for (int i = 0; i < subRutas.Count; i++)
        {
            resultado.AddRange(subRutas[i]);
            if (i < subRutas.Count - 1)
                resultado.Add(-1);
        }

        return resultado;
    }

    private float calcularLongitudSubRuta(List<int> ruta)
    {
        float distancia = 0f;

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            Vector3 a = mTSPManager.Instance.Coordenadas[ruta[i]];
            Vector3 b = mTSPManager.Instance.Coordenadas[ruta[i + 1]];
            distancia += Vector3.Distance(a, b);
        }

        return distancia;
    }
    

    private float calcularLongitudTotalM(List<int> ruta, float pesoAgente = 0.5f)
    {
        float distanciaTotal = 0f;
        float distanciaMaxAgente = 0f;
        float distanciaAgenteActual = 0f;
        int almacen = 0;

        for (int i = 1; i < ruta.Count - 1; i++)
        {
            
            if (ruta[i] == -1 || ruta[i - 1] == -1)
                continue;

            Vector3 a = mTSPManager.Instance.Coordenadas[ruta[i - 1]];
            Vector3 b = mTSPManager.Instance.Coordenadas[ruta[i]];

            float distanciaSumar = Vector3.Distance(a, b);
            distanciaTotal += distanciaSumar;
            distanciaAgenteActual += distanciaSumar;
        }

        return distanciaTotal + pesoAgente * distanciaMaxAgente;
    }

    public List<int> SolucionBusquedaTabu(int maxIteraciones, int tamanoListaTabu, int semilla)
    {
        System.Random rand = new System.Random(semilla);

        List<int> solucionActual = generarRutaInicialM(rand, 5);
        float costeActual = calcularLongitudTotalM(solucionActual);

        List<int> mejorSolucion = new List<int>(solucionActual);
        float mejorCoste = costeActual;

        Queue<string> listaTabu = new Queue<string>();

        for (int iter = 0; iter < maxIteraciones; iter++)
        {
            List<int> mejorVecino = null;
            float mejorCosteVecino = float.MaxValue;
            string mejorMovimiento = "";

            for (int k = 0; k < 50; k++) // vecinos explorados
            {
                List<int> vecino = GenerarVecinoPorAgente(solucionActual, rand);

                string movimiento = string.Join(",", vecino);

                if (listaTabu.Contains(movimiento))
                    continue;

                float costeVecino = calcularLongitudTotalM(vecino);

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

        return aplicar2OptFinal(mejorSolucion);
    }

    private List<int> GenerarVecinoPorAgente(List<int> ruta, System.Random rand)
    {
        List<List<int>> subRutas = new List<List<int>>();
        List<int> actual = new List<int>();

        // 🔹 Separar por agentes usando -1
        foreach (int punto in ruta)
        {
            if (punto == -1)
            {
                if (actual.Count > 0)
                    subRutas.Add(new List<int>(actual));

                actual.Clear();
            }
            else
            {
                actual.Add(punto);
            }
        }

        if (actual.Count > 0)
            subRutas.Add(actual);

        // 🔹 Elegir un agente aleatorio
        int agente = rand.Next(subRutas.Count);
        List<int> subRuta = new List<int>(subRutas[agente]);

        // 🔹 Aplicar 2-OPT dentro del agente
        if (subRuta.Count > 2)
        {
            int i = rand.Next(0, subRuta.Count - 2);
            int j = rand.Next(i + 1, subRuta.Count - 1);

            subRuta.Reverse(i, j - i + 1);
            subRutas[agente] = subRuta;
        }

        // 🔹 Reconstruir la ruta global con -1
        List<int> resultado = new List<int>();

        for (int k = 0; k < subRutas.Count; k++)
        {
            resultado.AddRange(subRutas[k]);

            if (k < subRutas.Count - 1)
                resultado.Add(-1);
        }

        return resultado;
    }
}

