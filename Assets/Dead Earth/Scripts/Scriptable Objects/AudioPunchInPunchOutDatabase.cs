using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioPunchInPunchOutInfo
{
    public AudioClip Clip = null;

    [Min(0.0f)] public float StartTime = 0.0f;

    [Min(0.0f)] public float EndTime = 0.0f;
}

[CreateAssetMenu(fileName = "New Audio Punch-In Punch-Out Database", menuName = "Dead Earth/Audio Punch-In Punch-Out Database")]
public class AudioPunchInPunchOutDatabase : ScriptableObject
{
    //List 是存档格式，Dictionary 是运行时索引，unity 不支持 Dictionary 序列化，所以我们需要两个数据结构来存储数据，
    //而且时间复杂度上，Dictionary 的查找是 O(1)，而 List 的查找是 O(n)，所以我们在运行时使用 Dictionary 来提高查找效率。
    //它只是一个运行时缓存/索引。
    //可以理解成：
    //.asset 真正保存：

    //_dataList
    //   ↓
    //硬盘数据

    //运行游戏时：

    //_dataList
    //   ↓
    //OnEnable()
    //   ↓
    //生成 _dataDictionary
    //   ↓
    //快速查询

    //_dataList 是“给 Unity 编辑和保存看的”；_dataDictionary 是“给程序运行时查数据看的”
    [SerializeField] protected List<AudioPunchInPunchOutInfo> _dataList = new List<AudioPunchInPunchOutInfo>();

    protected Dictionary<AudioClip, AudioPunchInPunchOutInfo> _dataDictionary = new Dictionary<AudioClip, AudioPunchInPunchOutInfo>();

    protected void OnEnable()
    {
        _dataDictionary.Clear();

        foreach (AudioPunchInPunchOutInfo info in _dataList)
        {
            if (info != null && info.Clip != null)
                _dataDictionary[info.Clip] = info;
        }
    }

    public AudioPunchInPunchOutInfo GetClipInfo(AudioClip clip)
    {
        if (clip == null)
            return null;

        _dataDictionary.TryGetValue(clip, out AudioPunchInPunchOutInfo info);
        return info;
    }
}