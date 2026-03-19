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
        List<int> rutaInicial = generarRutaInicialM(rand);
        float mejorLongitud = calcularLongitudTotalM(rutaInicial);
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

        }

        return mejorCandidata ?? rutaInicial;
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
        int numCiudades = TSPManager.Instance.NumCiudades;
        List<int> ruta = new List<int>();

        for (int i = 1; i < numCiudades; i++)
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

        int posCorte = 1;

        for(int i = 0; i < numAgentes -1; i++)
        {
            posCorte = rand.Next(posCorte, ruta.Count);
            ruta.Insert(posCorte, -1);
            if(posCorte == ruta.Count)
            {
                posCorte = 1; //crear lista que guarde los puntos de corte y cuando genere uno sea seguro que no esta ya en esa lista
            }
        }

        // LISTA DE PUNTOS DE CORTE
        List<int> puntosCorte = new List<int>();

        for (int i = 0; i < numAgentes - 1; i++)
        {
            do
            {
                posCorte = rand.Next(1, ruta.Count);
            }
            while (puntosCorte.Contains(posCorte));

            puntosCorte.Add(posCorte);
            ruta.Insert(posCorte, -1);

            if (posCorte == ruta.Count)
            {
                posCorte = 1;
            }
        }

        return ruta;
    }

    private float calcularLongitudTotalM(List<int> ruta, float pesoAgente = 0.5f)
    {
        float distanciaTotal = 0f;
        float distanciaMaxAgente = 0f;
        float distanciaAgenteActual= 0f;
        int almacen = 0;

        for (int i = 1; i < ruta.Count - 1; i++)
        {
            Vector3 a = TSPManager.Instance.Coordenadas[ruta[i - 1]];
            Vector3 b = new Vector3();


            if (i != -1 )
            {
                
                b = TSPManager.Instance.Coordenadas[ruta[i]];
                float distanciaSumar = Vector3.Distance(a, b);
                distanciaTotal += distanciaSumar;
                distanciaAgenteActual += distanciaSumar;
            }
            else
            {
                b = TSPManager.Instance.Coordenadas[ruta[almacen]];
                float distanciaSumar = Vector3.Distance(a, b);
                distanciaTotal += distanciaSumar;
                distanciaAgenteActual += distanciaSumar;
                distanciaMaxAgente = Mathf.Max(distanciaMaxAgente, distanciaAgenteActual);
                distanciaAgenteActual = 0;

            }
           
          
        }

        /*Vector3 ultima = TSPManager.Instance.Coordenadas[ruta[ruta.Count - 1]];
        Vector3 primera = TSPManager.Instance.Coordenadas[ruta[0]];
        distanciaTotal += Vector3.Distance(ultima, primera);*/

       

        return distanciaTotal + pesoAgente * distanciaMaxAgente;
    }

    public List<int> SolucionBusquedaTabu(int maxIteraciones, int tamanoListaTabu, int semilla)
    {
        System.Random rand = new System.Random(semilla);

        List<int> solucionActual = generarRutaInicialM(rand);
        float costeActual = calcularLongitudTotalM(solucionActual);

        List<int> mejorSolucion = new List<int>(solucionActual);
        float mejorCoste = costeActual;

        Queue<string> listaTabu = new Queue<string>();

        for (int iter = 0; iter < maxIteraciones; iter++)
        {
            List<int> mejorVecino = null;
            float mejorCosteVecino = float.MaxValue;
            string mejorMovimiento = "";

            for (int k = 0; k < 50; k++) // número de vecinos explorados
            {
                int i = rand.Next(1, solucionActual.Count - 2);
                int j = rand.Next(i, solucionActual.Count - 1);

                List<int> vecino = new List<int>(solucionActual);
                vecino.Reverse(i, j - i + 1);

                string movimiento = i + "-" + j;

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

        return mejorSolucion;
    }
}

