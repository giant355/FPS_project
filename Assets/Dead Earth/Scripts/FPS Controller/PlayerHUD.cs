using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenFadeType { FadeIn, FadeOut }

//Heads-Up Display：抬头显示界面
public class PlayerHUD : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private GameObject _crosshair = null;
    [SerializeField] private Sprite _normalCrosshairSprite = null;
    [SerializeField] private Sprite _targetCrosshairSprite = null;

    [Header("UI Text")]
    [SerializeField] private Text _interactionText = null;//显示交互提示
    [SerializeField] private Text _transcriptText = null;
    [SerializeField] private Text _notificationText = null;

    [Header("UI Sliders")]
    [SerializeField] private Slider _healthSlider = null;
    [SerializeField] private Slider _staminaSlider = null;
    [SerializeField] private Slider _infectionSlider = null;
    [SerializeField] private Slider _flashlightSlider = null;
    [SerializeField] private Slider _nightVisionSlider = null;

    [Header("Shared Variables")]
    [SerializeField] private SharedFloat _health = null;
    [SerializeField] private SharedFloat _stamina = null;
    [SerializeField] private SharedFloat _infection = null;
    [SerializeField] private SharedFloat _flashlight = null;
    [SerializeField] private SharedFloat _nightVision = null;
    [SerializeField] private SharedString _interactionString = null;
    [SerializeField] private SharedString _transcriptString = null;
    [SerializeField] private SharedTimedStringQueue _notificationQueue = null;

    [Header("Additional")]
    [SerializeField] private Image _screenFade = null;

    private float _currentFadeLevel = 1f;
    private IEnumerator _fadeCoroutine;
    private Image _crosshairImage = null;

    private void Start()
    {
        if (_crosshair != null)
            _crosshairImage = _crosshair.GetComponent<Image>();

        if (_screenFade != null)
        {
            _screenFade.gameObject.SetActive(true);
            // 开局让黑色 Fade 图片完全不透明
            Color color = _screenFade.color;
            color.a = _currentFadeLevel;
            _screenFade.color = color;
        }

    }

    private void Update()
    {
        if (_healthSlider != null && _health != null)
            _healthSlider.value = _health.value;

        if (_staminaSlider != null && _stamina != null)
            _staminaSlider.value = _stamina.value;

        if (_infectionSlider != null && _infection != null)
            _infectionSlider.value = _infection.value;

        if (_flashlightSlider != null && _flashlight != null)
            _flashlightSlider.value = _flashlight.value;

        if (_nightVisionSlider != null && _nightVision != null)
            _nightVisionSlider.value = _nightVision.value;

        if (_interactionText != null && _interactionString != null)
        {
            string currentInteraction = _interactionString.value ?? string.Empty;

            _interactionText.text = currentInteraction;

            _interactionText.gameObject.SetActive(!string.IsNullOrEmpty(currentInteraction));
        }

        if (_transcriptText != null && _transcriptString != null)
        {
            _transcriptText.text = _transcriptString.value ?? string.Empty;
        }

        if (_notificationText != null && _notificationQueue != null)
        {
            string currentNotification = _notificationQueue.text ?? string.Empty;

            _notificationText.text = currentNotification;

            _notificationText.gameObject.SetActive(!string.IsNullOrEmpty(currentNotification));
        }
    }

    public void Fade(float seconds, ScreenFadeType direction)
    {
        // 同一时间只运行一个淡入淡出
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        float targetFade = direction == ScreenFadeType.FadeIn ? 0f : 1f;
        _fadeCoroutine = FadeInternal(seconds, targetFade);
        StartCoroutine(_fadeCoroutine);
    }

    private IEnumerator FadeInternal(float seconds, float targetFade)
    {
        if (_screenFade == null)
        {
            _fadeCoroutine = null;
            yield break;
        }

        seconds = Mathf.Max(seconds, 0.1f);
        float timer = 0f;
        float sourceFade = _currentFadeLevel;
        Color color = _screenFade.color;

        while (timer < seconds)
        {
            timer += Time.deltaTime;
            _currentFadeLevel = Mathf.Lerp(sourceFade, targetFade, Mathf.Clamp01(timer / seconds));
            color.a = _currentFadeLevel;
            _screenFade.color = color;
            yield return null;
        }

        // 确保最终透明度精确到达目标值
        _currentFadeLevel = targetFade;
        color.a = _currentFadeLevel;
        _screenFade.color = color;
        _fadeCoroutine = null;
    }

    public void SetCrosshairTarget(bool hasTarget)
    {
        if (_crosshairImage == null) return;

        _crosshairImage.sprite = hasTarget ? _targetCrosshairSprite : _normalCrosshairSprite;
    }
}
