using UnityEngine;

/// <summary>
/// 模拟挂在胸口的 Bodycam 惯性：
/// 移动时轻微倾斜、平移；快速转身时有一点滞后。
/// 挂在 BodycamSway 物体上。
/// </summary>
public class BodycamSway : MonoBehaviour
{
    // 玩家根物体上的 FPSController，用来读取速度和朝向
    [SerializeField] private FPSController _controller;

    [Header("移动惯性")]

    // 每 1 m/s 横向速度，产生多少度左右侧倾
    [SerializeField] private float _strafeRollPerSpeed = 0.35f;

    // 每 1 m/s 前进速度，产生多少度前后俯仰
    [SerializeField] private float _movePitchPerSpeed = 0.10f;

    // 每 1 m/s 横向速度，产生多少米横向位移
    [SerializeField] private float _strafeOffsetPerSpeed = 0.006f;

    // 每 1 m/s 前进速度，产生多少米前后位移
    [SerializeField] private float _forwardOffsetPerSpeed = 0.003f;

    [Header("转身惯性")]

    // 每秒转动 1 度时，额外产生多少度的侧倾
    [SerializeField] private float _turnRollPerDegreePerSecond = 0.015f;

    // 限制快速甩鼠标时的最大侧倾角，避免过度晃动
    [SerializeField] private float _maxTurnRoll = 2f;

    [Header("平滑")]

    // 数值越大，越快追上目标位置和旋转
    [SerializeField] private float _smoothSpeed = 12f;

    // 保存上一帧玩家的 Y 轴朝向，用于计算这一帧转身有多快
    private float _previousYaw;

    private void Awake()
    {
        // Inspector 没手动赋值时，自动从父物体查找 FPSController
        if (_controller == null)
            _controller = GetComponentInParent<FPSController>();

        // 记录初始朝向，避免第一帧计算出异常大的转向速度
        if (_controller != null)
            _previousYaw = _controller.transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        // LateUpdate 在 FPSController 的 Update 之后执行，
        // 因此能读取到本帧最新的移动和鼠标朝向。
        if (_controller == null || _controller.characterController == null)
            return;

        // 获取玩家的世界速度；忽略 Y 轴，跳跃和下落不参与 Bodycam 摇摆
        Vector3 worldVelocity = _controller.characterController.velocity;
        worldVelocity.y = 0f;

        // 把世界速度转换为“相对玩家自身”的局部速度：
        // X：向左/右移动速度
        // Z：向前/后移动速度
        Vector3 localVelocity = _controller.transform.InverseTransformDirection(worldVelocity);

        // 计算这一帧玩家 Y 轴转了多少度
        float currentYaw = _controller.transform.eulerAngles.y;

        // DeltaAngle 能正确处理 359° 转到 0° 的情况。
        // 除以 deltaTime 后，得到每秒转动多少度。
        float yawSpeed = Mathf.DeltaAngle(_previousYaw, currentYaw) / Mathf.Max(Time.deltaTime, 0.0001f);

        _previousYaw = currentYaw;

        // 横向移动时，让镜头向相反方向略微侧倾。
        // 例如向右移动时，镜头稍微向左倾。
        float moveRoll = -localVelocity.x * _strafeRollPerSpeed;

        // 快速转向时，让镜头向转向的反方向产生惯性侧倾。
        // Clamp 限制最大角度，防止镜头太晃。
        float turnRoll = Mathf.Clamp(-yawSpeed * _turnRollPerDegreePerSecond,-_maxTurnRoll,_maxTurnRoll);

        // 最终目标旋转：
        // X = 前后移动造成的轻微俯仰
        // Y = 0，不抢玩家的左右鼠标旋转
        // Z = 横移侧倾 + 转身惯性侧倾
        Vector3 targetRotation = new Vector3(localVelocity.z * _movePitchPerSpeed,0f,moveRoll + turnRoll);

        // 最终目标位置：
        // 横移时有一点反方向位移；
        // 前进时有一点前后惯性位移。
        Vector3 targetPosition = new Vector3(-localVelocity.x * _strafeOffsetPerSpeed,0f,-localVelocity.z * _forwardOffsetPerSpeed);

        // 指数平滑：不同帧率下的手感更稳定。
        float smoothing = 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);

        // 平滑旋转到目标角度
        transform.localRotation = Quaternion.Slerp(transform.localRotation,Quaternion.Euler(targetRotation),smoothing);

        // 平滑移动到目标位置
        transform.localPosition = Vector3.Lerp(transform.localPosition,targetPosition,smoothing);
    }
}