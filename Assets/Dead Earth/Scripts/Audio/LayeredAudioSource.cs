using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Animator 状态
//    ↓ 发出 Play / Stop / Mute 命令
//AILayeredAudioSourcePlayer
//    ↓
//LayeredAudioSource
//    ├─ AudioLayer 0：Base Layer 的音效状态
//    ├─ AudioLayer 1：Lower Body Layer 的音效状态
//    └─ AudioLayer 2：更高图层的音效状态
//                ↓ 选出最高有效图层
//          Unity AudioSource
//                ↓
//              发声

//AudioManager.Update()
//    ↓ 每帧调用
//LayeredAudioSource.Update()
public class AudioLayer
{
    public AudioClip Clip = null;
    public AudioCollection Collection = null;
    public int Bank = 0;
    public bool Looping = true;
    public float Time = 0.0f;//记录该图层当前音效的播放进度。图层暂时被更高图层覆盖时，仍可保存/推进时间，超出duration的时间会取余
    public float Duration = 0.0f;//记录该图层当前音效的长度，比如一首歌就是三分钟左右
    public bool Muted = false;//图层静音状态。图层暂时被更高图层覆盖时，仍可保存/推进静音状态
}

public interface ILayeredAudioSource
{
    bool Play(AudioCollection collection, int bank, int layer, bool looping = true);
    void Stop(int layerIndex);
    void Mute(int layerIndex, bool mute);
    void Mute(bool mute);
}

public class LayeredAudioSource : ILayeredAudioSource
{
    //全局唯一的 AudioSource 组件，所有图层的音效都通过它发声。
    AudioSource _audioSource = null;
    List<AudioLayer>  _audioLayers = new List<AudioLayer>();
    //_activeLayer = -1 表示目前没有任何图层占用真实的 AudioSource。
    int _activeLayer = -1;

    public AudioSource audioSource{ get { return _audioSource; } }

    public LayeredAudioSource(AudioSource source, int layers)
    {
        //LayeredAudioSource 不是 MonoBehaviour，所以需要构造函数手动传入 Unity 的 AudioSource。
        //layers 决定创建多少个音频图层，通常对应 Animator 的图层数量。
        //新图层最初没有播放任务，因此把 Looping 初始化为 false。

        //把所有图层加到 _audioLayers 列表中，后续通过索引访问。
        if (source != null && layers > 0)
        {
            _audioSource = source;

            for (int i = 0; i < layers; i++)
            {
                AudioLayer newLayer = new AudioLayer();
                newLayer.Collection = null;
                newLayer.Duration = 0.0f;
                newLayer.Time = 0.0f;
                newLayer.Looping = false;
                newLayer.Bank = 0;
                newLayer.Muted = false;
                newLayer.Clip = null;

                _audioLayers.Add(newLayer);
            }
        }
    }

    /// <summary>
    /// 在指定的音频层播放来自给定集合和库（bank）的音频，并初始化或更新该层的播放状态，
    /// LayeredAudioSource.Play() 是“登记播放任务”，AudioSource.Play() 才是“真正开始发声”
    /// </summary>
    /// <remarks>如果目标层当前已使用相同的 collection、bank 和 looping，则不会重新初始化播放状态。该方法会重置时间、时长、静音标志并清除已关联的音频片段。</remarks>
    public bool Play(AudioCollection collection, int bank, int layer, bool looping = true)
    {
        if (layer < 0 || layer >= _audioLayers.Count) return false;

        AudioLayer audioLayer = _audioLayers[layer];

        if (audioLayer.Collection == collection && audioLayer.Bank == bank && audioLayer.Looping == looping) return true;

        audioLayer.Collection = collection;
        audioLayer.Bank = bank;
        audioLayer.Looping = looping;
        audioLayer.Time = 0.0f;
        audioLayer.Duration = 0.0f;
        audioLayer.Muted = false;
        audioLayer.Clip = null;

        return true;
    }

    public void Stop(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _audioLayers.Count) return;

        AudioLayer layer = _audioLayers[layerIndex];
        if (layer == null) return;

        layer.Looping = false;
        layer.Time = layer.Duration;
        //为什么不设置mute？因为 Stop() 是“停止播放任务”，而 Mute() 是“静音播放任务”，
        //两者的语义不同。Stop() 只是让图层不再循环播放，Time 会被推进到 Duration，表示该图层的播放任务已经完成。
        //之后time>duration,被update清空，因为collection==null，被跳过
    }

    /// <summary>
    /// 静音或取消静音指定图层
    /// </summary>
    public void Mute(int layerIndex, bool mute)
    {
        if (layerIndex < 0 || layerIndex >= _audioLayers.Count) return;

        AudioLayer layer = _audioLayers[layerIndex];
        if (layer != null) layer.Muted = mute;
    }

    /// <summary>
    /// 静音或取消静音所有图层
    /// </summary>
    public void Mute(bool mute)
    {
        for (int i = 0; i < _audioLayers.Count; i++) Mute(i, mute);
    }

    /// <summary>
    /// 更新各音频图层的播放时间、处理循环与完成状态，并根据最高有效图层配置或停止 AudioSource。
    /// </summary>
    /// <remarks>逐帧将每个图层的 Time 增加 deltaTime；若 Time 超过 Duration，则：当 Looping 为 true 或当前 Clip 为 null 时，从集合获取新的
    /// Clip，若 Clip 未改变则对 Time 取模以保持循环播放进度，否则重置 Time；当不循环且播放结束时，清除图层状态。选出索引最高的有效图层，在索引变化或需要刷新时设置 AudioSource 的
    /// clip、volume、spatialBlend、time、loop（设为 false）和输出混音组并调用 Play；若无有效图层则停止并清空 AudioSource。最后更新 _activeLayer。</remarks>
    public void Update()
    {
        //最高有效图层的索引
        int newActiveLayer = -1;
        //这一帧当前真正的 AudioSource 是否需要重新配置并重新播放。
        bool refreshAudioSource = false;

        for (int i = _audioLayers.Count - 1; i >= 0; i--)
        {
            //更新每个图层的播放时间
            AudioLayer layer = _audioLayers[i];
            if (layer.Collection == null) continue;
            layer.Time += UnityEngine.Time.deltaTime;

            if (layer.Time > layer.Duration)
            {
                if (layer.Looping || layer.Clip == null)
                {
                    AudioClip clip = layer.Collection[layer.Bank];

                    //如果当前图层的 Clip 与新获取的 clip 相同，则将 Time 对 Clip 的长度取余，确保播放进度在循环播放时不会超过音频片段的长度。
                    if (clip == layer.Clip) layer.Time %= layer.Clip.length;
                    else layer.Time = 0.0f;

                    layer.Duration = clip.length;
                    layer.Clip = clip;

                    //选出最高有效图层，确保 AudioSource 播放的是最上层的音效。
                    if (newActiveLayer < i)
                    {
                        newActiveLayer = i;
                        //最高活动图层播放完一轮：设置 refreshAudioSource = true，重新配置真实播放器（因为clip可能改变）
                        refreshAudioSource = true;
                    }
                }
                //如果当前图层的播放任务已经完成（Time 超过 Duration 且不循环），则清空该图层的播放状态。
                else
                {
                    layer.Clip = null;
                    layer.Collection = null;
                    layer.Duration = 0.0f;
                    layer.Bank = 0;
                    layer.Looping = false;
                    layer.Time = 0.0f;
                }
            }
            else
            {
                //如果当前图层的播放任务还未完成，则选出最高有效图层，确保 AudioSource 播放的是最上层的音效。
                if (newActiveLayer < i) newActiveLayer = i;
            }
        }
        if (newActiveLayer != _activeLayer || refreshAudioSource)
        {
            //如果不存在有效图层，停止audioSource
            if (newActiveLayer == -1)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }
            else
            {
                AudioLayer layer = _audioLayers[newActiveLayer];

                //配置audioSource，并调用play();
                _audioSource.clip = layer.Clip;
                _audioSource.volume = layer.Muted ? 0.0f : layer.Collection.volume;
                _audioSource.spatialBlend = layer.Collection.spatialBlend;
                _audioSource.time = layer.Time;
                //unity的loop始终为false，因为我们在LayeredAudioSource.Update()中手动处理循环逻辑。
                _audioSource.loop = false;
                _audioSource.outputAudioMixerGroup = AudioManager.Instance.GetAudioGroupFromTrackName(layer.Collection.audioGroup);
                _audioSource.Play();
            }
        }

        //更新当前最高有效图层索引
        _activeLayer = newActiveLayer;

        //为了在不需要更新图层或改变图层时进行mute
        if (_activeLayer != -1 && _audioSource != null)
        {
            AudioLayer activeAudioLayer = _audioLayers[_activeLayer];
            _audioSource.volume = activeAudioLayer.Muted ? 0.0f : activeAudioLayer.Collection.volume;
        }
    }

    
}
