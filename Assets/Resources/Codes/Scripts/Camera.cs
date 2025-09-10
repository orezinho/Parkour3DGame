using UnityEngine;

public class CameraFollowHead : MonoBehaviour
{
    public Transform headBone;
    public Transform cam;        
    public float followStrength = 0.3f; 
    public float smooth = 10f;

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
