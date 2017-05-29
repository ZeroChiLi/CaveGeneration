using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject targetObject;

    private Vector3 keepDistance;

    void Start()
    {
        keepDistance = transform.position - targetObject.transform.position;
    }

    void Update()
    {
        transform.position = keepDistance + targetObject.transform.position;
    }
}
