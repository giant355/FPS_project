using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ClipBank
{
    public List<AudioClip> Clips = new List<AudioClip>();
}

/// <summary>
/// AudioCollection对于其他脚本来说，就是提供了一个分组随机选取特定类型clip的接口
/// </summary>
[CreateAssetMenu(fileName = "New Audio Collection")]
public class AudioCollection : ScriptableObject
{
    [SerializeField] string _audioGroup = string.Empty;
    [SerializeField, Range(0f, 1f)] float _volume = 1f;
    [SerializeField, Range(0f, 1f)] float _spatialBlend = 1f;
    [SerializeField, Range(0, 256)] int _priority = 128;

    [SerializeField] List<ClipBank> _audioClipBanks = new List<ClipBank>();

    public string audioGroup { get { return _audioGroup; } }
    public float volume { get { return _volume; } }
    public float spatialBlend { get { return _spatialBlend; } }
    public int priority { get { return _priority; } }
    public int bankCount { get { return _audioClipBanks.Count; } }

    public AudioClip this[int i]
    {
        get
        {
            if (_audioClipBanks == null || _audioClipBanks.Count <= i) return null;
            if (_audioClipBanks[i].Clips.Count == 0) return null;

            List<AudioClip> clipList = _audioClipBanks[i].Clips;
            AudioClip clip = clipList[Random.Range(0, clipList.Count)];

            return clip;
        }
    }

    public AudioClip audioClip
    {
        get
        {
            if (_audioClipBanks == null || _audioClipBanks.Count == 0) return null;
            if (_audioClipBanks[0].Clips.Count == 0) return null;

            List<AudioClip> clipList = _audioClipBanks[0].Clips;
            AudioClip clip = clipList[Random.Range(0, clipList.Count)];

            return clip;
        }
    }
}