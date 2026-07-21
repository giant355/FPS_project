using UnityEngine;

/// <summary>
/// 服务于每个玩家，分别地管理伤害
/// </summary>
public class CharacterManager : MonoBehaviour
{
    [SerializeField] private CapsuleCollider _meleeTrigger = null;
    [SerializeField] private CameraBloodEffect _cameraBloodEffect = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _health = 100f;
    [SerializeField] private AISoundEmitter _soundEmitter = null;

    [SerializeField] private float _walkAudioRadius = 1.2f;
    [SerializeField] private float _runAudioRadius = 7;
    [SerializeField] private float _landingAudioRadius = 12;
    [SerializeField] private float _bloodRadiusScale = 6.0f;

    private Collider _playerCollider = null;
    private FPSController _fpsController = null;
    private CharacterController _characterController = null;
    private GameSceneManager _gameSceneManager = null;  
    private int _aiBodyPartLayer = -1;

    void Start()
    {
        _playerCollider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.Instance;

        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        if (_gameSceneManager != null)
        {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = _playerCollider;
            info.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_playerCollider.GetInstanceID(), info);
        }
    }
    public void TakeDamage(float amount)
    {
        _health = Mathf.Max(_health - amount, 0f);

        if (_cameraBloodEffect != null)
        {
            _cameraBloodEffect.MinBloodAmount = 1f - _health/100f;
            _cameraBloodEffect.BloodAmount = Mathf.Min(_cameraBloodEffect.MinBloodAmount + 0.3f, 1f);
        }
    }
    public void DoDamage(int hitDirection = 0)
    {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << _aiBodyPartLayer);

        if (isSomethingHit)
        {
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.transform.root.GetInstanceID());
            if (stateMachine)
            {
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 15, hit.rigidbody, this, 0);
            }
        }

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoDamage();
        }

        if (_fpsController || _soundEmitter != null)
        {
            float newRadius = Mathf.Max(_walkAudioRadius, (100.0f - _health) / _bloodRadiusScale);
            switch (_fpsController.movementStatus)
            {
                case PlayerMoveStatus.Landing: newRadius = Mathf.Max(newRadius, _landingAudioRadius); break;
                case PlayerMoveStatus.Running: newRadius = Mathf.Max(newRadius, _runAudioRadius); break;
            }

            _soundEmitter.SetRadius(newRadius);
        }
    }
}

