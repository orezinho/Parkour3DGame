using System.Linq;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CheckpointSystem : MonoBehaviour
{
    [Tooltip("Coloque os checkpoints em sequência")]
    public GameObject[] CheckPoints;
    public GameObject SpawnPoint;

    private int currentCheckPoint = -1;
    private float tempoSegurando = 0f;
    private bool acionou = false;

    private PlayerMovement plrMove;
    private CharacterController character;

    private void Awake()
    {
        CarregarCheckPoints();
        character = GetComponent<CharacterController>();
        plrMove = GetComponent<PlayerMovement>();
        RestarPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject is GameObject obj && LayerMask.LayerToName(obj.layer) == "Checkpoint")
        {
            var i = System.Array.IndexOf(CheckPoints, obj);

            if (i != -1 && i == currentCheckPoint + 1)
            {
                currentCheckPoint = i;
                CarregarCheckPoints();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            tempoSegurando += Time.deltaTime;

            if (tempoSegurando >= 2f && !acionou)
            {
                acionou = true;
                StartCoroutine(RestarPositionCoroutine());
            }
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            tempoSegurando = 0f;
        }
    }

    private IEnumerator RestarPositionCoroutine()
    {
        character.enabled = false;
        plrMove.enabled = false;

        yield return new WaitForSeconds(0.2f);
        if (currentCheckPoint >= 0)
            transform.position = CheckPoints[currentCheckPoint].transform.position;
        else
            transform.position = SpawnPoint.transform.position;

        plrMove.velocity = Vector3.zero;

        yield return new WaitForSeconds(0.1f);
        character.enabled = true;
        plrMove.enabled = true;

        acionou = false;
        tempoSegurando = 0f;
    }

    private void RestarPosition()
    {
        if (currentCheckPoint >= 0)
            transform.position = CheckPoints[currentCheckPoint].transform.position;
        else
            transform.position = SpawnPoint.transform.position;

        if (plrMove != null)
            plrMove.velocity = Vector3.zero;
    }

    private void CarregarCheckPoints()
    {
        for (var i = 0; i < CheckPoints.Count(); i++)
        {
            if (i > currentCheckPoint && i >= 0)
                CheckPoints[i].gameObject.SetActive(true);
            else
                CheckPoints[i].gameObject.SetActive(false);
        }
    }
}