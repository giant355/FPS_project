using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private Animator _animator=null;
    private int _horizontalHash = 0;
    private int _verticalHash = 0;
    private int _attackHash = 0;
    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _horizontalHash = Animator.StringToHash("Horizontal");
        _verticalHash = Animator.StringToHash("Vertical");
        _attackHash = Animator.StringToHash("Attack");
    }

    // Update is called once per frame
    void Update()
    {
        float xAxis = Input.GetAxis("Horizontal")*2.48f;
        float yAxis = Input.GetAxis("Vertical")*5.66f;

        if (Input.GetKeyDown(KeyCode.Mouse0))
            _animator.SetTrigger(_attackHash);
        _animator.SetFloat(_horizontalHash,xAxis,.1f,Time.deltaTime);
        _animator.SetFloat(_verticalHash,yAxis, 1.0f, Time.deltaTime);
    }
}
