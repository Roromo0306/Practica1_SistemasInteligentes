[System.Serializable]
public class ScoreData
{
    public float distancia;
    public float eficiencia;
    public string rango;

    public ScoreData(float d, float e, string r)
    {
        distancia = d;
        eficiencia = e;
        rango = r;
    }
}