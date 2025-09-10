using System.Collections;
using UnityEngine;

public class Pad : MonoBehaviour
{
    [Header("Configuração Visual")]
    public float squashAmount = 0.5f;
    public float squashSpeed = 5f;   

    [Header("Impulsão do Player")]
    public float jumpForce = 10f;    

    private Vector3 originalScale;
    private bool isSquashing = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player colidiu com o pad");
            PlayerMovement player = other.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.velocity.y = jumpForce;
            }

            if (!isSquashing)
            {
                StartCoroutine(SquashAndStretch());
            }
        }
    }

    private IEnumerator SquashAndStretch()
    {
        isSquashing = true;

        Vector3 squashed = new Vector3(originalScale.x, originalScale.y * squashAmount, originalScale.z);

        while (Vector3.Distance(transform.localScale, squashed) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, squashed, Time.deltaTime * squashSpeed);
            yield return null;
        }

        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * squashSpeed);
            yield return null;
        }

        transform.localScale = originalScale;
        isSquashing = false;
    }
}
