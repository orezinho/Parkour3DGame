using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Câmeras")]
    public Transform firstCamera;
    public Transform thirdCamera;
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController controller;
    public Vector3 velocity;
    private bool isGrounded;

    [Header("Estados")]
    public bool isClimbing = false;
    public bool isThirdPerson = false;

    private Animator animator;
    private Transform activeCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();
        UpdateActiveCamera();
    }

    void Update()
    {
        UpdateActiveCamera();
        ScreenMovement();
        PlayerMove();
        Jump();
        ApplyGravity();
        IsPlayerRunning();
    }

    private void UpdateActiveCamera()
    {
        if (isThirdPerson)
        {
            activeCamera = thirdCamera;
            thirdCamera.gameObject.SetActive(true);
            firstCamera.gameObject.SetActive(false);
        }
        else
        {
            activeCamera = firstCamera;
            firstCamera.gameObject.SetActive(true);
            thirdCamera.gameObject.SetActive(false);
        }
    }

    private void Jump()
    {
        if (isClimbing && (Keyboard.current.spaceKey.wasPressedThisFrame || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)))
        {
            StartCoroutine(ExitLadder());
            isClimbing = false;
            return;
        }

        if ((Keyboard.current.spaceKey.wasPressedThisFrame || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) && isGrounded)
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

        Vector2 moveInput = Vector2.zero;
        if (Gamepad.current != null)
        {
            moveInput = Gamepad.current.leftStick.ReadValue();
        }
        else
        {
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        float speed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                      (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
                      ? runSpeed : walkSpeed;

        if (isThirdPerson)
        {
            Vector3 camForward = activeCamera.forward;
            Vector3 camRight = activeCamera.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 move = camRight * moveInput.x + camForward * moveInput.y;

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                controller.Move(move.normalized * speed * Time.deltaTime);
            }
        }
        else
        {
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            controller.Move(move * speed * Time.deltaTime);
        }
    }

    private void ScreenMovement()
    {
        if (isThirdPerson) return;

        Vector2 lookInput = Vector2.zero;
        if (Gamepad.current != null)
        {
            lookInput = Gamepad.current.rightStick.ReadValue();
        }
        else
        {
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 70f);

        firstCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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
        Vector2 moveInput = Vector2.zero;
        if (Gamepad.current != null)
        {
            moveInput = Gamepad.current.leftStick.ReadValue();
        }
        else
        {
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        bool isWalking = move.magnitude > 0.1f && isGrounded;
        bool isRunning = ((Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                         (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)) &&
                         isWalking && isGrounded;

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
