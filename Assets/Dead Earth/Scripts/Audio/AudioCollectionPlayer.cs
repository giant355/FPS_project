using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCollectionPlayer : AIStateMachineLink
{
    //选择此状态机行为监听哪个 Animator 参数，默认是 ComChannel1
    [SerializeField] ComChannelName _commandChannel = ComChannelName.ComChannel1;
    [SerializeField] AudioCollection _collection = null;
    [SerializeField] CustomCurve _customCurve = null;
    [SerializeField] StringList _LayerExclusion = null;

    int _previousCommand = 0;
    AudioManager _audioManager = null;
    int _commandChannelHash = 0;

    override public void OnStateEnter(Animator animator,AnimatorStateInfo animStateInfo,int layerIndex)
    {
        _audioManager = AudioManager.Instance;
        _previousCommand = 0;

        if (_commandChannelHash == 0)
            _commandChannelHash = Animator.StringToHash(_commandChannel.ToString());
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine == null) return;
        if (layerIndex != 0 && animator.GetLayerWeight(layerIndex).Equals(0.0f)) return;

        if(_LayerExclusion != null)
        {
            for(int i=0;i<_LayerExclusion.count;i++)
            {
                if(_stateMachine.IsLayerActive(_LayerExclusion[i])) return;
            }
        }

        //如果 Inspector 分配了 _customCurve，优先读取它的曲线值。
        //没有分配自定义曲线时，读取 Animator 的 Com Channel 参数值。
        //Mathf.FloorToInt 将曲线的浮点值转成命令整数，如 0、1、2。
        //normalizedTime 会循环增长；减去整数部分后，只保留当前动画循环内的 0～1 时间。
        int customCommand = (_customCurve == null) ? 0 : Mathf.FloorToInt(_customCurve.Evaluate(animStateInfo.normalizedTime - (long)animStateInfo.normalizedTime));
        int command;
        if (customCommand != 0) command = customCommand;
        else command = Mathf.FloorToInt(animator.GetFloat(_commandChannelHash));

        //根据command播放音频
        if (_previousCommand != command && command > 0 && _audioManager != null && _collection != null && _stateMachine != null)
        {
            //选取AudioCollection的第几个bank
            int bank = Mathf.Max(0, Mathf.Min(command - 1, _collection.bankCount - 1));
            _audioManager.PlayOneShotSound(_collection.audioGroup, _collection[bank], _stateMachine.transform.position, _collection.volume, _collection.spatialBlend, _collection.priority);
        }

        _previousCommand = command;
    }
}
