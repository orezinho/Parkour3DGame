using System.Linq;
using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    [Header("Configurações principais.")]
    public CharacterController Player;

    [Tooltip("Coloque os checkpoints em sequência")]
    public GameObject[] CheckPoints;

    private int currentCheckPoint = -1;

    private void Awake()
    {
        CarregarCheckPoints();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject is GameObject obj && LayerMask.LayerToName(obj.layer) == "CheckPoint")
        {
            var i = System.Array.IndexOf(CheckPoints, obj);

            if (i != -1 && i == currentCheckPoint + 1)
            {
                currentCheckPoint = i;
                CarregarCheckPoints();
            }
        }

    }

    private void CarregarCheckPoints()
    {
        for (var i = 0; i <= CheckPoints.Count(); i++)
        {
            if (i > currentCheckPoint)
                CheckPoints[i].gameObject.SetActive(true);
            else
                CheckPoints[i].gameObject.SetActive(false);
        }
    }
}
