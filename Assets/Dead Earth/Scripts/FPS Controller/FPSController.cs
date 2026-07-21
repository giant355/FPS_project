using UnityEngine;

using UnityEngine.Serialization;

public enum PlayerMoveStatus {NotMoving,Crouching,Walking,Running,NotGrounded,Landing };

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera _camera;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -20f;
    //重落地 的时间阈值
    [SerializeField] private float _heavyLandingSpeed = 10f;

    [Header("Mouse Look")]
    [SerializeField] private float _mouseSensitivity = 2f;
    [SerializeField] private float _minimumPitch = -90f;
    [SerializeField] private float _maximumPitch = 90f;

    [Header("Ground")]
    [SerializeField] private float _groundCheckOffset = 0.2f;

    [Header("Crouch")]
    [SerializeField] private KeyCode _crouchKey = KeyCode.C;
    [SerializeField] private float _crouchSpeed = 1f;
    [SerializeField] private float _crouchTransitionSpeed = 8f;
    [Range(0.25f, 0.9f)]
    [SerializeField] private float _crouchHeightMultiplier = 0.5f;

    [Header("Head Bob")]
    [SerializeField] private float _headBobFrequency = 1.8f;
    [SerializeField] private float _headBobHorizontalAmplitude = 0.03f;
    [SerializeField] private float _headBobVerticalAmplitude = 0.04f;
    [SerializeField] private float _headBobReturnSpeed = 10f;
    [SerializeField] private float _runHeadBobMultiplier = 1.35f;
    [SerializeField] private float _crouchHeadBobMultiplier = 0.65f;

    [Header("FootstepsAudio")]
    [SerializeField] private AudioSource _footstepAudioSource;
    [SerializeField] private AudioClip[] _footstepClips;
    [Range(0f, 1f)]
    [SerializeField] private float _footstepVolume = 0.7f;
    [SerializeField] private Vector2 _footstepPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Landing Audio")]
    [SerializeField] private AudioClip _heavyLandingClip;
    [SerializeField] private float _heavyLandingVolume = 1f;

    [Header("Flashlight")]
    [SerializeField] private GameObject _flashlight;

    private CharacterController _characterController;

    private Vector2 _inputVector;
    private Vector2 _mouseRotationInput;
    private float _verticalVelocity;
    private float _cameraPitch;

    private LayerMask _groundMask;

    private bool _isCursorLocked;

    private bool _isWalking = true;
    private bool _previouslyGrounded;
    private bool _groundStateInitialized;

    private bool _isCrouching;
    private float _standingControllerHeight;
    private Vector3 _standingControllerCenter;
    private Vector3 _standingCameraPosition;

    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    private float _headBobPhase;
    private Vector3 _headBobOffset;
    private Vector3 _currentCameraBasePosition;

    //记录上一次播放的是哪一个音效
    private int _lastFootstepIndex;

    public PlayerMoveStatus movementStatus => _movementStatus;
    public float walkSpeed => _walkSpeed;
    public float runSpeed => _runSpeed;
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (_footstepAudioSource == null)
        {
            _footstepAudioSource =
                GetComponentInChildren<AudioSource>();
        }

        _groundMask = LayerMask.GetMask("Default");

        if (_camera == null)
            _camera = GetComponentInChildren<Camera>();

        if (_camera == null)
        {
            Debug.LogError("FPSController 找不到玩家摄像机。", this);
            enabled = false;
            return;
        }

        _standingControllerHeight = _characterController.height;
        _standingControllerCenter = _characterController.center;
        _standingCameraPosition = _camera.transform.localPosition;

        _currentCameraBasePosition = _standingCameraPosition;
    }
    private void Start()
    {
        LockCursor();

        if (_flashlight != null)
            _flashlight.SetActive(false);
    }
    private void Update()
    {
        UpdateCursorState();

        ReadInput();
        UpdateCrouch();

        if (Time.timeScale > Mathf.Epsilon && _isCursorLocked)
        {
            UpdateMouseLook();
        }
        UpdateMovement();
        UpdateMovementStatus();
        UpdateHeadBob();
        UpdateFlashlight();
    }
    private void ReadInput()
    {
        _inputVector = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );

        _mouseRotationInput = new Vector2(Input.GetAxis("Mouse X") * _mouseSensitivity,
                                          Input.GetAxis("Mouse Y") * _mouseSensitivity);
    }
    private void UpdateMouseLook()
    {
        float mouseX = _mouseRotationInput.x;
        float mouseY = _mouseRotationInput.y;

        // 玩家身体左右旋转
        transform.Rotate(Vector3.up * mouseX);

        // 摄像机上下旋转
        _cameraPitch -= mouseY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, _minimumPitch, _maximumPitch);

        _camera.transform.localRotation =
            Quaternion.Euler(_cameraPitch, 0f, 0f);
    }


    private void UpdateMovement()
    {
        //characterController.isGrounded变量解释：上一次执行move时，character controller是否接触地面（下半球）
        bool isGrounded = _characterController.isGrounded;
        // 落地且正在下落时，清除累计下落速度，
        // 并保留小幅向下速度，让 Move 持续检测地面接触。
        if (isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        // 防止斜向移动速度更快，有输入时始终等于1
        Vector2 normalizedInput = Vector2.ClampMagnitude(_inputVector, 1f);

        _isWalking = !Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = _isCrouching ? _crouchSpeed : _isWalking ? _walkSpeed : _runSpeed;

        Vector3 desiredMove = transform.right * normalizedInput.x + transform.forward * normalizedInput.y;

        if (isGrounded && normalizedInput.sqrMagnitude > 0f && TryGetGroundNormal(out Vector3 groundNormal))
        {
            desiredMove = Vector3.ProjectOnPlane(desiredMove, groundNormal).normalized * normalizedInput.magnitude;
        }

        Vector3 groundVelocity = desiredMove * currentSpeed;

        // 只有在地面上且没有蹲下才能跳跃
        if (isGrounded && !_isCrouching && Input.GetButtonDown("Jump"))
        {
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        // 重力持续改变垂直速度
        _verticalVelocity += _gravity * Time.deltaTime;
        //最终速度
        Vector3 velocity = groundVelocity + Vector3.up * _verticalVelocity;

        _characterController.Move(velocity * Time.deltaTime);
    }


    private void UpdateMovementStatus()
    {
        bool isGrounded = _characterController.isGrounded;

        if (!_groundStateInitialized)
        {
            _previouslyGrounded = isGrounded;
            _groundStateInitialized = true;
        }

        if (!isGrounded)
        {
            _movementStatus = PlayerMoveStatus.NotGrounded;
        }
        //接下来一定会在地面上
        //!_previouslyGrounded->Landing
        else if (!_previouslyGrounded)
        {
            _movementStatus = PlayerMoveStatus.Landing;

            float landingSpeed = Mathf.Max(0f, -_verticalVelocity);

            if (landingSpeed >= _heavyLandingSpeed)
            {
                OnHeavyLanding(landingSpeed);
            }
        }
        else
        {
            Vector3 horizontalVelocity =
                _characterController.velocity;

            horizontalVelocity.y = 0f;

            if (horizontalVelocity.sqrMagnitude < 0.01f)
                _movementStatus = PlayerMoveStatus.NotMoving;
            else if (_isCrouching)
                _movementStatus = PlayerMoveStatus.Crouching;
            else if (_isWalking)
                _movementStatus = PlayerMoveStatus.Walking;
            else
                _movementStatus = PlayerMoveStatus.Running;
        }

        _previouslyGrounded = isGrounded;
    }

    private void UpdateHeadBob()
    {
        Vector3 horizontalVelocity = _characterController.velocity;
        horizontalVelocity.y = 0f;

        bool shouldBob =  _characterController.isGrounded && horizontalVelocity.sqrMagnitude > 0.01f;

        if (shouldBob)
        {
            float movementMultiplier = _isCrouching ? _crouchHeadBobMultiplier : _isWalking ? 1f : _runHeadBobMultiplier;
            _headBobPhase += horizontalVelocity.magnitude * _headBobFrequency * movementMultiplier * Time.deltaTime;
            //如果越界，归位
            _headBobPhase = Mathf.Repeat(_headBobPhase, 2*Mathf.PI);
            Vector3 targetOffset = new Vector3(Mathf.Sin(_headBobPhase) * _headBobHorizontalAmplitude, Mathf.Sin(_headBobPhase * 2f) * _headBobVerticalAmplitude, 0f);
            //smoothing = 1 - e^(-k × deltaTime)
            float smoothing = 1f - Mathf.Exp(-_headBobReturnSpeed * Time.deltaTime);
            _headBobOffset = Vector3.Lerp(_headBobOffset, targetOffset, smoothing);

            UpdateFootsteps();
        }
        else
        {
            //smoothing = 1 - e^(-k × deltaTime)
            float smoothing = 1f - Mathf.Exp(-_headBobReturnSpeed * Time.deltaTime);
            _headBobOffset = Vector3.Lerp(_headBobOffset, Vector3.zero, smoothing);
        }

        _camera.transform.localPosition = _currentCameraBasePosition + _headBobOffset;
    }

    private void UpdateFootsteps()
    {
        int footstepIndex = Mathf.FloorToInt(_headBobPhase / Mathf.PI);

        if (footstepIndex == _lastFootstepIndex)
            return;

        _lastFootstepIndex = footstepIndex;

        if (_isCrouching)
            return;

        if (_footstepAudioSource == null || _footstepClips == null || _footstepClips.Length == 0)
            return;

        AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
        _footstepAudioSource.pitch = Random.Range(_footstepPitchRange.x, _footstepPitchRange.y);
        _footstepAudioSource.PlayOneShot(clip, _footstepVolume);
    }

    private void UpdateCrouch()
    {
        if (Input.GetKeyDown(_crouchKey))
        {
            _isCrouching = !_isCrouching;
        }

        float crouchingHeight = Mathf.Max(_standingControllerHeight * _crouchHeightMultiplier,_characterController.radius * 2f);

        float targetHeight = _isCrouching? crouchingHeight : _standingControllerHeight;

        Vector3 targetCenter = _standingControllerCenter;

        if (_isCrouching)
        {
            targetCenter.y -= (_standingControllerHeight - crouchingHeight) * 0.5f;
        }

        Vector3 targetCameraPosition = _isCrouching ? _standingCameraPosition - Vector3.up * (_standingControllerHeight - crouchingHeight) : _standingCameraPosition;

        float step = _crouchTransitionSpeed * Time.deltaTime;

        _characterController.height = Mathf.MoveTowards(_characterController.height,targetHeight,step);

        _characterController.center = Vector3.MoveTowards(_characterController.center,targetCenter,step);

        _currentCameraBasePosition = Vector3.MoveTowards(_currentCameraBasePosition, targetCameraPosition, step);
    }
    private void UpdateFlashlight()
    {
        if (_flashlight == null)
            return;

        if (Input.GetButtonDown("Flashlight"))
            _flashlight.SetActive(!_flashlight.activeSelf);
    }
    //-------------------------------工具函数----------------------------------
    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        Vector3 center = transform.TransformPoint(_characterController.center);
        Vector3 castOrigin = center + ( _characterController.height * 0.5f - _characterController.radius )* Vector3.down;
        //防止检测墙壁
        float castRadius = _characterController.radius * 0.8f;

        if(Physics.SphereCast(castOrigin,castRadius,Vector3.down,out RaycastHit hit,
           _groundCheckOffset+0.2f*_characterController.radius,_groundMask,QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            return true;
        }
        groundNormal = Vector3.up;
        return false;
    }

    private void OnHeavyLanding(float landingSpeed)
    {
        Debug.Log($"重落地，落地速度：{landingSpeed:F2} m/s");

        if(_heavyLandingClip == null || _footstepAudioSource == null)
        {
            return;
        }

        _footstepAudioSource.PlayOneShot(_heavyLandingClip,_heavyLandingVolume);
    }

    private void UpdateCursorState()
    {
        // Esc 解锁鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        // 未暂停时，单击游戏画面重新锁定鼠标
        if (!_isCursorLocked && Time.timeScale > Mathf.Epsilon && Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _isCursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _isCursorLocked = false;
    }
    //-----------------------------------------------------------------------------
    //焦点处理，避免切出游戏后 _isCursorLocked 与 Unity 的真实状态不同
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            UnlockCursor();
        }
    }
}

