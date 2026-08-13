using System.Collections;
using UnityEngine;

public class InteractiveSound : InteractiveItem
{
    [TextArea(3, 10)]
    [SerializeField] private string _infoText = null;//尚未触发时显示的提示

    [TextArea(3, 10)]
    [SerializeField] private string _activatedText = null;//触发声音后显示的文字

    [SerializeField] private float _activatedTextDuration = 3f;//触发文字保留多久
    [SerializeField] private AudioCollection _audioCollection = null;
    [SerializeField] private int _bank = 0;

    private IEnumerator _coroutine = null;
    private float _hideActivatedTextTime = 0f;//记录何时恢复普通提示

    /// <summary>
    /// 尚未触发时，返回 _infoText，例如“按 E 播放录音”。
    /// 声音协程正在执行时，返回 _activatedText。
    /// 即使声音已经播完，只要尚未到达 _hideActivatedTextTime，仍显示触发后的文字。
    /// </summary>
    /// <returns></returns>
    public override string GetText()
    {
        if (_coroutine != null || Time.time < _hideActivatedTextTime)
            return _activatedText;

        return _infoText;
    }

    public override void Activate(CharacterManager characterManager)
    {
        // 如果协程正在执行，说明声音正在播放中，直接返回
        if (_coroutine != null) return;

        //记录何时恢复普通提示
        _hideActivatedTextTime = Time.time + _activatedTextDuration;

        _coroutine = DoActivation();
        StartCoroutine(_coroutine);
    }

    private IEnumerator DoActivation()
    {
        // 缺少声音集合或音频管理器时，终止本次交互
        if (_audioCollection == null || AudioManager.Instance == null)
        {
            _coroutine = null;
            yield break;
        }

        AudioClip clip = _audioCollection[_bank];

        // 没有取得有效音频时，终止本次交互
        if (clip == null)
        {
            _coroutine = null;
            yield break;
        }

        // 在交互物品所在的位置播放一次声音
        AudioManager.Instance.PlayOneShotSound(_audioCollection.audioGroup, clip, transform.position, 
                                               _audioCollection.volume, _audioCollection.spatialBlend, _audioCollection.priority);

        // 等待声音播放结束
        yield return new WaitForSeconds(clip.length);

        // 允许玩家再次触发
        _coroutine = null;
    }
}