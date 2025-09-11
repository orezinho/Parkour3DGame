using UnityEngine;

public class FirstCamera : MonoBehaviour
{
    public Transform headBone;
    public Transform cam;        
    public float followStrength = 0.3f; 
    public float smooth = 10f;

    public PlayerMovement player;

    private Vector3 offset;

    void Start()
    {
        offset = cam.localPosition;
    }

    void LateUpdate()
    {
        Vector3 targetPos = headBone.position;
        Quaternion targetRot = headBone.rotation;

        cam.position = Vector3.Lerp(cam.position, targetPos, Time.deltaTime * smooth);

        cam.rotation = Quaternion.Slerp(cam.rotation, targetRot, followStrength);
    }
}
