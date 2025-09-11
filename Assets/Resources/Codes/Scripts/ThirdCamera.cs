using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Alvo / Pivot")]
    public Transform target;                
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Órbita / Input (opcional)")]
    public bool useMouseOrbit = true;
    public float minPitch = -30f;         
    public float maxPitch = 70f;             

    [Header("Distância / Zoom")]
    public float defaultDistance = 3.5f;    
    public float minDistance = 0.3f;        
    public float maxDistance = 6f;          
    public float zoomSpeed = 5f;             

    [Header("Colisão")]
    public float sphereRadius = 0.25f;      
    public float clipPushback = 0.05f;       
    public LayerMask obstructionMask = ~0;   

    [Header("Suavização")]
    public float positionSmoothTime = 0.05f; 
    public float rotationSmoothTime = 0.02f; 

    float yaw;      
    float pitch;    
    public float currentDistance;
    float distVel; 
    public PlayerMovement player;
    Quaternion currentRot, rotVel;

    void Start()
    {
        if (!target)
        {
            Debug.LogWarning("ThirdPersonCameraCollision: atribua o Target no Inspector.");
            enabled = false; return;
        }

        Vector3 toCam = (transform.position - (target.position + pivotOffset));
        if (toCam.sqrMagnitude > 0.0001f)
        {
            var lookRot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            var e = lookRot.eulerAngles;
            yaw = e.y;
            pitch = NormalizePitch(e.x);
        }

        currentRot = Quaternion.Euler(pitch, yaw, 0f);
        currentDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
    }

    void Update()
    {
        // Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            defaultDistance = Mathf.Clamp(defaultDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        if (useMouseOrbit)
        {
            Vector2 lookInput = Vector2.zero;

            if (Gamepad.current != null)
            {
                lookInput = Gamepad.current.rightStick.ReadValue() * 4;
            }
            else
            {
                lookInput = new Vector2(Input.GetAxis("CameraHorizontal"), Input.GetAxis("CameraVertical"));
            }

            yaw += lookInput.x * player.sensitivity * Time.deltaTime;
            pitch -= lookInput.y * player.sensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }


    void LateUpdate()
    {
        if (!target) return;

        Vector3 pivot = target.position + pivotOffset;

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        currentRot = SmoothDampQuaternion(currentRot, desiredRot, ref rotVel, rotationSmoothTime);

        Vector3 backDir = currentRot * Vector3.back; 

        float desiredDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        float hitDistance = desiredDistance;

        if (Physics.SphereCast(pivot, sphereRadius, backDir, out RaycastHit hit, desiredDistance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            hitDistance = Mathf.Clamp(hit.distance - clipPushback, minDistance, desiredDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, hitDistance, ref distVel, positionSmoothTime);

        Vector3 camPos = pivot + backDir * currentDistance;
        transform.SetPositionAndRotation(camPos, currentRot);
    }

    // ---------- utils ----------
    float NormalizePitch(float x)
    {
        if (x > 180f) x -= 360f;
        return x;
    }

    Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Quaternion deriv, float time)
    {
        if (time <= 0f) return target;

        if (Quaternion.Dot(current, target) < 0f)
        {
            target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
        }

        Vector4 c = new Vector4(current.x, current.y, current.z, current.w);
        Vector4 t = new Vector4(target.x, target.y, target.z, target.w);

        Vector4 result = new Vector4(
            Mathf.SmoothDamp(c.x, t.x, ref deriv.x, time),
            Mathf.SmoothDamp(c.y, t.y, ref deriv.y, time),
            Mathf.SmoothDamp(c.z, t.z, ref deriv.z, time),
            Mathf.SmoothDamp(c.w, t.w, ref deriv.w, time)
        ).normalized;

        return new Quaternion(result.x, result.y, result.z, result.w);
    }

}
