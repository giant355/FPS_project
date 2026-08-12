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
    [SerializeField] private float _heavyLandingAttractionHoldTime = 0.1f;
    [SerializeField] private float _bloodRadiusScale = 6.0f;

    [SerializeField] private CameraRecoil _cameraRecoil = null;
    [SerializeField] private AudioCollection _damageSounds = null;//攻击接触玩家时的撞击、撕咬声
    [SerializeField] private AudioCollection _painSounds = null;//玩家发出的痛叫声
    [SerializeField] private float _painSoundOffset = 0.35f;//让痛叫稍晚于撞击声播放
    [SerializeField] private PlayerHUD _playerHUD = null;
    private float _nextPainSoundTime = 0f;//限制痛叫频率，避免连续攻击造成大量声音重叠

    private Collider _playerCollider = null;
    private FPSController _fpsController = null;
    private CharacterController _characterController = null;
    private GameSceneManager _gameSceneManager = null;  
    private int _aiBodyPartLayer = -1;
    private float _heavyLandingAttractionUntil;

    public float health => _health;
    public float stamina => _fpsController != null ? _fpsController.stamina : 0f;

    void Start()
    {
        _playerCollider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.Instance;

        if (_fpsController != null)
            _fpsController.HeavyLanded += HandleHeavyLanding;

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

        // 游戏开始时让黑幕在两秒内消失
        if (_playerHUD != null)
            _playerHUD.Fade(3f, ScreenFadeType.FadeIn);
    }
    private void OnDestroy()
    {
        if (_fpsController != null)
            _fpsController.HeavyLanded -= HandleHeavyLanding;
    }
    private void HandleHeavyLanding(float landingSpeed)
    {
        _heavyLandingAttractionUntil = Time.time + _heavyLandingAttractionHoldTime;
    }
    public void TakeDamage(float amount, bool doDamageSound = true, bool doPainSound = true)
    {
        _health = Mathf.Max(_health - amount, 0f);

        if (doDamageSound && _damageSounds != null && AudioManager.Instance != null) 
            AudioManager.Instance.PlayOneShotSound(_damageSounds.audioGroup, _damageSounds.audioClip, transform.position, 
                                                   _damageSounds.volume, _damageSounds.spatialBlend, _damageSounds.priority);

        if (doPainSound && _painSounds != null && AudioManager.Instance != null && Time.time >= _nextPainSoundTime)
        {
            AudioClip painClip = _painSounds.audioClip;

            if (painClip != null)
            {
                _nextPainSoundTime = Time.time + painClip.length;
                StartCoroutine(AudioManager.Instance.PlayOneShotSoundDelayed(_painSounds.audioGroup, painClip, transform.position, _painSounds.volume,
                                                                             _painSounds.spatialBlend, _painSoundOffset, _painSounds.priority));
            }
        }

        if (_fpsController != null)
        {
            _fpsController.dragMultiplier = 0f;
        }

        if (_cameraBloodEffect != null)
        {
            _cameraBloodEffect.MinBloodAmount = 1f - _health/100f;
            _cameraBloodEffect.BloodAmount = Mathf.Min(_cameraBloodEffect.MinBloodAmount + 0.3f, 1f);
        }

        _cameraRecoil.ApplyRecoil();
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
                float critChance = 20f;
                bool isRagDollHit = Random.Range(0f, 100f) < critChance;

                //弱武器也有概率直接击倒僵尸
                stateMachine.TakeDamage(hit.point, ray.direction * (1.0f + (isRagDollHit?1:0)), 15, hit.rigidbody, this, 0);
            }
        }

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoDamage();

            if (_cameraRecoil != null)
                _cameraRecoil.ApplyRecoil();
        }

        if (_fpsController != null && _soundEmitter != null)
        {
            float newRadius = Mathf.Max(_walkAudioRadius, (100.0f - _health) / _bloodRadiusScale);
            switch (_fpsController.movementStatus)
            {
                case PlayerMoveStatus.Running: newRadius = Mathf.Max(newRadius, _runAudioRadius); break;
            }

            if (Time.time < _heavyLandingAttractionUntil)
                newRadius = Mathf.Max(newRadius, _landingAudioRadius);

            _soundEmitter.SetRadius(newRadius);
        }

        if (_fpsController != null)
        {
            _fpsController.dragMultiplierLimit = Mathf.Max(_health / 100f, 0.4f);
        }

        if (_playerHUD != null)
            _playerHUD.UpdateHUD(this);
    }
}

