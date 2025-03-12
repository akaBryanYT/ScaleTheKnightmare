using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 2.5f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private Transform target;
    
    private void LateUpdate()  // Better than Update for camera following
    {
        if (target == null) return;
        
        Vector3 newPos = new Vector3(target.position.x, target.position.y + yOffset, -10f);
        transform.position = Vector3.Slerp(transform.position, newPos, followSpeed * Time.deltaTime);
    }
}