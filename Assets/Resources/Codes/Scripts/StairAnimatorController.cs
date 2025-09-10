using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class StairClimbing : MonoBehaviour
{
    [Header("Referências")]
    public Animator animator;

    [Header("Configuração da escada")]
    public float climbSpeed = 3f;
    public float gravity = -9.81f;

    [HideInInspector]
    public bool isClimbing = false;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 move = Vector3.zero;

        if (isClimbing)
        {
            float verticalInput = Input.GetAxis("Vertical");

            move = Vector3.up * verticalInput * climbSpeed;

            velocity.y = verticalInput * climbSpeed;

            animator.speed = Mathf.Abs(verticalInput) > 0.1f ? 1f : 0f;

            animator.SetBool("isClimbing", true);
        }
        else
        {
            animator.SetBool("isClimbing", false);
            animator.speed = 1f;
        }

        controller.Move((move + new Vector3(0, velocity.y, 0)) * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            isClimbing = true;
            velocity.y = 0f;
        }
    }

    // Detecta quando sai da escada
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            isClimbing = false;
        }
    }
}
