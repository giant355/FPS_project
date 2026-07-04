using UnityEngine;

/// <summary>
/// 服务于每个玩家，分别地管理伤害
/// </summary>
public class CharacterManager : MonoBehaviour
{
    [SerializeField] private CapsuleCollider meleeTrigger = null;
    [SerializeField] private CameraBloodEffect cameraBloodEffect = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float health = 100f;

    private Collider playerCollider = null;
    private FPSController fpsController = null;
    private CharacterController _characterController = null;
    private GameSceneManager _gameSceneManager = null;
    private int _aiBodyPartLayer = -1;

    void Start()
    {
        playerCollider = GetComponent<Collider>();
        fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.Instance;

        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        if (_gameSceneManager != null)
        {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = playerCollider;
            info.meleeTrigger = meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(playerCollider.GetInstanceID(), info);
        }
    }
    public void TakeDamage(float amount)
    {
        health = Mathf.Max(health - amount, 0f);

        if (cameraBloodEffect != null)
        {
            cameraBloodEffect.MinBloodAmount = 1f - health/100f;
            cameraBloodEffect.BloodAmount = Mathf.Min(cameraBloodEffect.MinBloodAmount + 0.3f, 1f);
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
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 25, hit.rigidbody, this, 0);
            }
        }

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoDamage();
        }
    }
}

