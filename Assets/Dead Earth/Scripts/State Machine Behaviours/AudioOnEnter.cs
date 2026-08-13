using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//进入某个状态就播放声音
public class AudioOnEnter : StateMachineBehaviour
{
    [SerializeField] AudioCollection _audioCollection = null;
    [SerializeField] int _bank = 0;


    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (AudioManager.Instance == null || _audioCollection == null) return;

        AudioManager.Instance.PlayOneShotSound(_audioCollection.audioGroup,
                                                _audioCollection[_bank],
                                                animator.transform.position,
                                                _audioCollection.volume,
                                                _audioCollection.spatialBlend,
                                                _audioCollection.priority);
    }
}
