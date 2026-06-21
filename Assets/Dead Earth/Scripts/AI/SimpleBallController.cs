using UnityEngine;

public class SimpleBallController : MonoBehaviour
{
    [SerializeField] float _speed = 5f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (transform.forward * v + transform.right * h) * _speed * Time.deltaTime;
        transform.position += move;
    }
}
