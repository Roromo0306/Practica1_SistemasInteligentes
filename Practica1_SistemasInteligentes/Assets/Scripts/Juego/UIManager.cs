using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI texto;

    void Update()
    {
        var gm = TSPManager_Game.Instance;

        texto.text =
            "Ciudades: " + gm.ciudadesVisitadas + "/" + gm.ciudades.Count + "\n" +
            "Distancia: " + gm.distanciaJugador.ToString("F1");
    }
}
