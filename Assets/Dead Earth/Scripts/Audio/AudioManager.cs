using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TrackInfo
{
    public string Name = string.Empty;       // 分组名，例如 "Zombies"
    public AudioMixerGroup Group = null;     // Unity 中实际对应的混音器分组
    public IEnumerator TrackFader = null;    // 当前控制该分组音量渐变的协程
}
public class AudioPoolItem
{
    public GameObject GameObject = null;
    public Transform Transform = null;
    public AudioSource AudioSource = null;
    public float Unimportance = float.MaxValue;
    public bool Playing = false;
    public IEnumerator Coroutine = null;
    public ulong ID = 0;
}
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    private List<LayeredAudioSource> _layeredAudios = new List<LayeredAudioSource>();

    [SerializeField] AudioMixer _mixer = null;
    [SerializeField] int _maxSounds = 10;

    Dictionary<string,TrackInfo> _tracks = new Dictionary<string,TrackInfo>();
    List<AudioPoolItem> _pool = new List<AudioPoolItem>();
    Dictionary<ulong, AudioPoolItem> _activePool = new Dictionary<ulong, AudioPoolItem>();
    ulong _idGiver = 0;
    Transform _listenerPos = null;

    public static AudioManager Instance => _instance;

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(_instance != this)
            Destroy(gameObject);

        if (!_mixer)
        {
            Debug.LogError("没有指定AudioMixer，AudioManager无法工作");
            return;
        }
        //FindMatchingGroups:路径名包含这个字符串
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(string.Empty);

        //对_mixer里面的每一个组的信息添加到tracks
        foreach (AudioMixerGroup group in groups)
        {
            TrackInfo trackInfo = new TrackInfo();
            trackInfo.Name = group.name;
            trackInfo.Group = group;
            trackInfo.TrackFader = null;
            _tracks[group.name] = trackInfo;
        }

        for( int i=0; i< _maxSounds; i++)
		{
			GameObject 		go 	= new GameObject("Pool Item");
			AudioSource 	audioSource = go.AddComponent<AudioSource>();
			go.transform.parent = transform;

			AudioPoolItem 	poolItem 	= new AudioPoolItem();
			poolItem.GameObject = go;
			poolItem.AudioSource= audioSource;
			poolItem.Transform	= go.transform;
			poolItem.Playing	= false;
			go.SetActive(false);
			_pool.Add( poolItem );
		
		}
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _listenerPos = FindAnyObjectByType<AudioListener>().transform;
    }

    void Update()
    {
        foreach (LayeredAudioSource layeredAudio in _layeredAudios)
        {
            if (layeredAudio != null) layeredAudio.Update();
        }
    }

    public float GetTrackVolume(string track)
    {
        TrackInfo trackInfo;
        if (_tracks.TryGetValue(track, out trackInfo))
        {
            float volume;
            _mixer.GetFloat(track, out volume);
            return volume;
        }

        return float.MinValue;
    }

    public AudioMixerGroup GetAudioGroupFromTrackName(string name)
    {
        TrackInfo trackInfo;
        if (_tracks.TryGetValue(name, out trackInfo))
        {
            return trackInfo.Group;
        }

        return null;
    }

    public void SetTrackVolume(string track, float volume, float fadeTime = 0.0f)
    {
        if (!_mixer) return;
        TrackInfo trackInfo;
        if (_tracks.TryGetValue(track, out trackInfo))
        {
            // 之前有协程的话先停止
            if (trackInfo.TrackFader != null) StopCoroutine(trackInfo.TrackFader);

            if (fadeTime == 0.0f)
                _mixer.SetFloat(track, volume);
            else
            {
                trackInfo.TrackFader = SetTrackVolumeInternal(track, volume, fadeTime);
                StartCoroutine(trackInfo.TrackFader);
            }
        }
    }

    /// <summary>
    /// 设置音量渐变效果的迭代器
    /// </summary>
    /// <param name="track"></param>
    /// <param name="volume"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    protected IEnumerator SetTrackVolumeInternal(string track, float volume, float fadeTime)
    {
        float startVolume = 0.0f;
        float timer = 0.0f;
        _mixer.GetFloat(track, out startVolume);

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            _mixer.SetFloat(track, Mathf.Lerp(startVolume, volume, timer / fadeTime));
            yield return null;
        }

        _mixer.SetFloat(track, volume);
    }

    /// <summary>
    /// 使poolIndex位的idem播放，记录此时它的unimportance，记录它的协程为StopSoundDelayed，加入activePool
    /// </summary>
    /// <param name="poolIndex"></param>
    /// <param name="track"></param>
    /// <param name="clip"></param>
    /// <param name="position"></param>
    /// <param name="volume"></param>
    /// <param name="spatialBlend"></param>
    /// <param name="unimportance"></param>
    /// <returns></returns>
    protected ulong ConfigurePoolObject(int poolIndex, string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, float unimportance)
    {
        if (poolIndex < 0 || poolIndex >= _pool.Count) return 0;

        AudioPoolItem poolItem = _pool[poolIndex];

        if(poolItem.Playing && poolItem.Coroutine != null)
        {
            StopCoroutine(poolItem.Coroutine);
            _activePool.Remove(poolItem.ID);
        }

        _idGiver++;

        poolItem.AudioSource.clip = clip;
        poolItem.AudioSource.volume = volume;
        poolItem.AudioSource.spatialBlend = spatialBlend;
        poolItem.AudioSource.outputAudioMixerGroup = _tracks[track].Group;
        poolItem.AudioSource.gameObject.transform.position = position;

        poolItem.Playing = true;
        poolItem.Unimportance = unimportance;
        poolItem.ID = _idGiver;
        poolItem.GameObject.SetActive(true);

        poolItem.AudioSource.Play();

        poolItem.Coroutine = StopSoundDelayed(_idGiver, poolItem.AudioSource.clip.length);
        StartCoroutine(poolItem.Coroutine);

        _activePool[_idGiver] = poolItem;

        return _idGiver;
    }

    protected IEnumerator StopSoundDelayed(ulong id, float duration)
    {
        yield return new WaitForSeconds(duration);
        AudioPoolItem activeSound;

        if (_activePool.TryGetValue(id, out activeSound))
        {
            activeSound.AudioSource.Stop();
            activeSound.AudioSource.clip = null;
            activeSound.GameObject.SetActive(false);
            _activePool.Remove(id);

            activeSound.Playing = false;
        }
    }

    public ulong PlayOneShotSound(string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, int priority = 128)
    {
        if (!_tracks.ContainsKey(track) || clip == null || volume < 0.01f) return 0;

        float unimportance = (_listenerPos.position - position).sqrMagnitude / Mathf.Max(1, priority);

        int leastImportantIndex = -1;
        float leastImportanceValue = float.MinValue;

        for (int i = 0; i < _pool.Count; i++)
        {
            AudioPoolItem poolItem = _pool[i];

            //如果空闲，直接配置并让其播放
            if (!poolItem.Playing)
                return ConfigurePoolObject(i, track, clip, position, volume, spatialBlend, unimportance);
            else
            //选出当前最不重要的那个物体（池满的情况下）
            if (poolItem.Unimportance > leastImportanceValue)
            {
                leastImportanceValue = poolItem.Unimportance;
                leastImportantIndex = i;
            }
        }

        //当前最不重要的物体比候选者还要不重要，直接挤占
        if (leastImportanceValue > unimportance)
            return ConfigurePoolObject(leastImportantIndex, track, clip, position, volume, spatialBlend, unimportance);

        return 0;
    }

    public IEnumerator PlayOneShotSoundDelayed(string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, float duration, int priority = 128)
    {
        yield return new WaitForSeconds(duration);
        PlayOneShotSound(track, clip, position, volume, spatialBlend, priority);
    }

    public ILayeredAudioSource RegisterLayeredAudioSource(AudioSource source, int layers)
    {
        if (source == null || layers <= 0) return null;

        //已经被注册过的AudioSource直接返回对应的LayeredAudioSource
        foreach (LayeredAudioSource layeredAudioSource in _layeredAudios)
        {
            if (layeredAudioSource != null && layeredAudioSource.audioSource == source) return layeredAudioSource;
        }

        LayeredAudioSource newLayeredAudio = new LayeredAudioSource(source, layers);
        _layeredAudios.Add(newLayeredAudio);

        return newLayeredAudio;
    }

    /// <summary>
    /// 注销只是让 AudioManager 停止管理它，不会主动销毁 NPC 的 AudioSource
    /// </summary>
    /// <param name="source"></param>
    public void UnregisterLayeredAudioSource(ILayeredAudioSource source)
    {
        LayeredAudioSource layeredAudioSource = source as LayeredAudioSource;
        if (layeredAudioSource != null) _layeredAudios.Remove(layeredAudioSource);
    }

    /// <summary>
    /// 通过AudioSource注销 LayeredAudioSource，注销只是让 AudioManager 停止管理它，不会主动销毁 NPC 的 AudioSource
    /// </summary>
    /// <param name="source"></param>
    public void UnregisterLayeredAudioSource(AudioSource source)
    {
        if (source == null) return;

        for (int i = 0; i < _layeredAudios.Count; i++)
        {
            LayeredAudioSource layeredAudioSource = _layeredAudios[i];

            if (layeredAudioSource != null && layeredAudioSource.audioSource == source)
            {
                _layeredAudios.RemoveAt(i);
                return;
            }
        }
    }
}
