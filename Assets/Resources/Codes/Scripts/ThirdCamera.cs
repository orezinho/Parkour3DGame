using UnityEngine;

public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("Alvo / Pivot")]
    public Transform target;                 // normalmente um empty nas costas/cabe�a do player
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("�rbita / Input (opcional)")]
    public bool useMouseOrbit = true;
    public float mouseXSensitivity = 180f;   // graus/segundo
    public float mouseYSensitivity = 120f;   // graus/segundo
    public float minPitch = -30f;            // olhar para baixo
    public float maxPitch = 70f;             // olhar para cima

    [Header("Dist�ncia / Zoom")]
    public float defaultDistance = 3.5f;     // dist�ncia �ideal�
    public float minDistance = 0.3f;         // qu�o perto pode chegar do piv�
    public float maxDistance = 6f;           // limite de zoom para tr�s
    public float zoomSpeed = 5f;             // com scroll do mouse

    [Header("Colis�o")]
    public float sphereRadius = 0.25f;       // �gordura� do cast (cobre cantos pr�ximos)
    public float clipPushback = 0.05f;       // folga do obst�culo
    public LayerMask obstructionMask = ~0;   // defina para ignorar Player

    [Header("Suaviza��o")]
    public float positionSmoothTime = 0.05f; // suaviza encurtar/voltar dist�ncia
    public float rotationSmoothTime = 0.02f; // suaviza rota��o

    // estado interno
    float yaw;      // rota��o em Y (horizontal)
    float pitch;    // rota��o em X (vertical)
    float currentDistance;
    float distVel;  // ref do SmoothDamp
    Quaternion currentRot, rotVel;

    void Start()
    {
        if (!target)
        {
            Debug.LogWarning("ThirdPersonCameraCollision: atribua o Target no Inspector.");
            enabled = false; return;
        }

        // inicializa rota��o olhando do alvo para a c�mera atual
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
        // Zoom por scroll (opcional)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            defaultDistance = Mathf.Clamp(defaultDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // �rbita por mouse (opcional)
        if (useMouseOrbit)
        {
            float mx = Input.GetAxis("CameraHorizontal");
            float my = -Input.GetAxis("CameraVertical");

            yaw += mx * mouseXSensitivity * Time.deltaTime;
            pitch += my * mouseYSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // alvo do piv�
        Vector3 pivot = target.position + pivotOffset;

        // rota��o alvo e suaviza��o de rota��o
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        currentRot = SmoothDampQuaternion(currentRot, desiredRot, ref rotVel, rotationSmoothTime);

        // dire��o �para tr�s� do piv�
        Vector3 backDir = currentRot * Vector3.back; // equivalente a -(forward)

        // dist�ncia alvo (pode ser encurtada por colis�o)
        float desiredDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        float hitDistance = desiredDistance;

        // SphereCast: do piv� para tr�s, at� a dist�ncia desejada
        if (Physics.SphereCast(pivot, sphereRadius, backDir, out RaycastHit hit, desiredDistance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            hitDistance = Mathf.Clamp(hit.distance - clipPushback, minDistance, desiredDistance);
        }

        // suaviza ir/voltar
        currentDistance = Mathf.SmoothDamp(currentDistance, hitDistance, ref distVel, positionSmoothTime);

        // aplica posi��o e rota��o
        Vector3 camPos = pivot + backDir * currentDistance;
        transform.SetPositionAndRotation(camPos, currentRot);
    }

    // ---------- utils ----------
    float NormalizePitch(float x)
    {
        // converte 0..360 para -180..180 e clampa depois em Update
        if (x > 180f) x -= 360f;
        return x;
    }

    // Suaviza��o de quaternions (sem alocar)
    Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Quaternion deriv, float time)
    {
        if (time <= 0f) return target;

        // garante o caminho mais curto
        if (Quaternion.Dot(current, target) < 0f)
        {
            target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
        }

        // converte para Vector4 pra poder usar SmoothDamp
        Vector4 c = new Vector4(current.x, current.y, current.z, current.w);
        Vector4 t = new Vector4(target.x, target.y, target.z, target.w);

        // suaviza cada componente
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(c.x, t.x, ref deriv.x, time),
            Mathf.SmoothDamp(c.y, t.y, ref deriv.y, time),
            Mathf.SmoothDamp(c.z, t.z, ref deriv.z, time),
            Mathf.SmoothDamp(c.w, t.w, ref deriv.w, time)
        ).normalized;

        // basta retornar o Quaternion, sem recalcular deriv
        return new Quaternion(result.x, result.y, result.z, result.w);
    }

}
