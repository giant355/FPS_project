using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISoundEmitter : MonoBehaviour
{
    /// <summary>
    /// 衰减速率,表示缩小到目标半径需要几秒
    /// </summary>
    [SerializeField] private float _decayRate = 1f;

    private SphereCollider _collider = null;
    //本次变化开始时的半径
    private float _srcRadius = 0f;
    //准备缩到的目标半径，例如缩到走路时的默认值
    private float _tgtRadius = 0f;
    //进度值，范围会从 0 逐渐到 1
    private float _interpolator = 0f;
    //进度增长速度；后面会根据 decayRate 算出来
    private float _interpolatorSpeed = 0f;
    private void Start()
    {
        _collider = GetComponent<SphereCollider>();
        if (!_collider) return;

        _srcRadius = _tgtRadius = _collider.radius;
        if (_decayRate > 0.02f)
            _interpolatorSpeed = 1.0f / _decayRate;
        else
            _interpolatorSpeed = 0.0f;
    }

    //FixedUpdate自动在两个半径之间插值，实时改变半径大小
    void FixedUpdate()
    {
        if (!_collider) return;

        _interpolator = Mathf.Clamp01(_interpolator + Time.deltaTime * _interpolatorSpeed);
        _collider.radius = Mathf.Lerp(_srcRadius, _tgtRadius, _interpolator);

        if (_collider.radius < Mathf.Epsilon) _collider.enabled = false;
        else _collider.enabled = true;
    }
    /// <summary>
    /// CharacterManager 调用：设置目标半径，并决定本次变化是否立即生效。
    /// </summary>
    /// <param name="newRadius">本次要设置的目标半径。</param>
    /// <param name="instantResize">是否强制立即调整；true 时即使缩小半径也不做平滑过渡。</param>
    public void SetRadius(float newRadius, bool instantResize = false)
    {
        if (!_collider || newRadius == _tgtRadius) return;
        //立即调用或新半径比原半径大，直接设置
        _srcRadius = (instantResize || newRadius > _collider.radius) ? newRadius : _collider.radius;
        _tgtRadius = newRadius;
        _interpolator = 0.0f;

    }
}
