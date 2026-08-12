using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenFadeType { FadeIn, FadeOut }

//Heads-Up Display：抬头显示界面
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair = null;
    [SerializeField] private Text _healthText = null;
    [SerializeField] private Text _staminaText = null;
    [SerializeField] private Text _interactionText = null;//显示交互提示
    [SerializeField] private Image _screenFade = null;
    [SerializeField] private Text _missionText = null;//显示当前任务
    [SerializeField] private float _missionTextDisplayTime = 3f;

    private float _currentFadeLevel = 1f;
    private IEnumerator _fadeCoroutine;

    private void Start()
    {
        if (_screenFade != null)
        {
            _screenFade.gameObject.SetActive(true);
            // 开局让黑色 Fade 图片完全不透明
            Color color = _screenFade.color;
            color.a = _currentFadeLevel;
            _screenFade.color = color;
        }

        if (_missionText != null)
            Invoke(nameof(HideMissionText), _missionTextDisplayTime);
    }

    public void ShowMissionText(string text)
    {
        if (_missionText == null) return;

        _missionText.text = text;
        _missionText.gameObject.SetActive(true);
    }

    public void HideMissionText()
    {
        if (_missionText != null)
            _missionText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 将内部文本控件更新为指定 CharacterManager 的 health 和 stamina 的整数显示；characterManager 为 null 时不执行任何操作。
    /// </summary>
    public void UpdateHUD(CharacterManager characterManager)
    {
        if (characterManager == null) return;

        if (_healthText != null)
            _healthText.text = $"{(int)characterManager.health}";

        if (_staminaText != null)
            _staminaText.text = $"{(int)characterManager.stamina}";
    }

    /// <summary>
    /// 设置交互提示文本
    /// </summary>
    public void SetInteractionText(string text)
    {
        if (_interactionText == null) return;

        bool hasText = !string.IsNullOrEmpty(text);
        _interactionText.text = hasText ? text : string.Empty;
        _interactionText.gameObject.SetActive(hasText);
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
}