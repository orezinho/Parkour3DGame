using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Câmeras")]
    public Transform firstCamera;
    public Transform thirdCamera;
    public float sensitivity = 100f;
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
    public bool isRunToggled = false;

    private Animator animator;
    private Transform activeCamera;

    private float speed;
    public int currentCam = 1;

    // --- Network Spawn: configurar câmeras ---
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Desativa câmeras dos jogadores remotos
            if (firstCamera != null) firstCamera.gameObject.SetActive(false);
            if (thirdCamera != null) thirdCamera.gameObject.SetActive(false);

            // Desativa AudioListener se existir
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();

        if (IsOwner)
            UpdateActiveCamera();
    }

    void Update()
    {

        Debug.Log($"[{OwnerClientId}] IsOwner={IsOwner} IsLocalPlayer={IsLocalPlayer}");
        if (!IsOwner) return;

        UpdateActiveCamera();
        ScreenMovement();
        PlayerMove();
        Jump();
        ApplyGravity();
        IsPlayerRunning();
        ToggleRun();
        ChangeCamera();
    }

    // ---------- Controle de Câmera ----------
    private void UpdateActiveCamera()
    {
        if (!IsOwner) return;

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

    private void ScreenMovement()
    {
        if (!IsOwner || isThirdPerson) return;

        Vector2 lookInput = Gamepad.current != null
            ? Gamepad.current.rightStick.ReadValue() * 4
            : new Vector2(Input.GetAxis("CameraHorizontal"), Input.GetAxis("CameraVertical"));

        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 70f);

        firstCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void ChangeCamera()
    {
        if (!IsOwner) return;

        if (Keyboard.current.cKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame))
        {
            if (currentCam <= 3)
                currentCam++;
            if (currentCam >= 4)
                currentCam = 1;

            CameraMode(currentCam);
        }
    }

    private void CameraMode(int mode)
    {
        ThirdPersonCamera thirdCam = thirdCamera.gameObject.GetComponent<ThirdPersonCamera>();

        switch (mode)
        {
            case 1:
                firstCamera.rotation = thirdCamera.transform.rotation;
                thirdCam.defaultDistance = 3f;
                thirdCam.maxDistance = 3f;
                thirdCam.currentDistance = 3f;
                isThirdPerson = true;
                break;
            case 2:
                thirdCam.defaultDistance = 6f;
                thirdCam.maxDistance = 6f;
                thirdCam.currentDistance = 6f;
                isThirdPerson = true;
                break;
            case 3:
                thirdCamera.rotation = firstCamera.rotation;
                isThirdPerson = false;
                break;
        }
    }

    // ---------- Movimento / Física ----------
    private void PlayerMove()
    {
        if (!IsOwner) return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        if (isClimbing) return;

        Vector2 moveInput = Gamepad.current != null
            ? Gamepad.current.leftStick.ReadValue()
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (!isRunToggled)
            speed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                     (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
                     ? runSpeed : walkSpeed;

        if (isThirdPerson)
        {
            Vector3 camForward = activeCamera.forward;
            Vector3 camRight = activeCamera.right;
            camForward.y = 0f; camRight.y = 0f;
            camForward.Normalize(); camRight.Normalize();

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

    private void Jump()
    {
        if (!IsOwner) return;

        if (isClimbing && (Keyboard.current.spaceKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)))
        {
            StartCoroutine(ExitLadder());
            isClimbing = false;
            return;
        }

        if ((Keyboard.current.spaceKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        animator.SetBool("isGrounded", isGrounded);
    }

    private IEnumerator ExitLadder()
    {
        if (!IsOwner) yield return null;

        float elapsed = 0f, duration = 0.5f;
        while (elapsed < duration)
        {
            controller.Move(-transform.forward * 3f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ToggleRun()
    {
        if (!IsOwner) return;

        if (Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame)
            isRunToggled = !isRunToggled;

        speed = isRunToggled ? runSpeed : walkSpeed;
    }

    private void ApplyGravity()
    {
        if (!IsOwner || isClimbing) return;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ---------- Animação ----------
    private void IsPlayerRunning()
    {
        Vector2 moveInput = Gamepad.current != null
            ? Gamepad.current.leftStick.ReadValue()
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        bool isWalking = move.magnitude > 0.1f && isGrounded;
        bool isRunning = (((Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                          (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed) ||
                          isRunToggled) && isWalking && isGrounded);

        // ⚠️ Note: mantive animação rodando em todos,
        // mas só o dono alimenta input → resultado é coerente
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalking", isWalking && !isRunning);
    }

    // ---------- Colisão ----------
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.CompareTag("Stairs")) isClimbing = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;
        if (other.CompareTag("Stairs")) isClimbing = false;
    }
}
