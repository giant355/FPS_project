using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeDestruct : MonoBehaviour
{
    [SerializeField] private float _time = 10;

    private void Awake()
    {
        Invoke("DestroyNow", _time);
    }

    void DestroyNow()
    {
        Destroy(gameObject);
    }
}
