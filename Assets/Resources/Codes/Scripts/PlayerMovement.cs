using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;

    public Transform playerCamera;
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController controller;
    public Vector3 velocity;
    private bool isGrounded;

    public bool isClimbing = false;

    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        ScreenMovement();
        PlayerMove();
        Jump();
        ApplyGravity();
        IsPlayerRunning();
    }

    private void Jump()
    {
        if (isClimbing && Input.GetButtonDown("Jump"))
        {
            StartCoroutine(ExitLadder());
            isClimbing = false;
            return;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }
         
        animator.SetBool("isGrounded", isGrounded);
    }

    private IEnumerator ExitLadder()
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            controller.Move(-transform.forward * 3f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PlayerMove()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        if (isClimbing) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);
    }

    private void ScreenMovement()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 70f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void ApplyGravity()
    {
        if (isClimbing) return;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void IsPlayerRunning()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        bool isWalking = move.magnitude > 0.1f && isGrounded;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isWalking && isGrounded;

        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalking", isWalking && !isRunning);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stairs")) 
        {
            isClimbing = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            isClimbing = false;
        }
    }

}


