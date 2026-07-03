using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_SPINE
using Spine.Unity;

#endif

namespace AFramework.Anim
{
    [System.Serializable]
    public struct AutoAnimInfo
    {
        public string name;
        public bool loop;
        public float duration;
    }


    public class AFSpineAutoAnim : MonoBehaviour
    {
        public AutoAnimInfo[] AnimFlowData;
#if USE_SPINE
        SkeletonAnimation mAnimationPlayer;

        private bool _isInit;

        private void Awake()
        {
            mAnimationPlayer = this.GetComponent<SkeletonAnimation>();
        }

        private void Start()
        {
            _isInit = true;
            StartCoroutine(CRPlayAnimationThread());
        }

        private void OnEnable()
        {
            if (_isInit)
                StartCoroutine(CRPlayAnimationThread());
        }

        public void SetAnimationPlayer(SkeletonAnimation obj)
        {
            mAnimationPlayer = obj;
        }

        IEnumerator CRPlayAnimationThread()
        {
            for (int i = 0; i < AnimFlowData.Length; ++i)
            {
                var data = AnimFlowData[i];
                if (!string.IsNullOrEmpty(data.name))
                {
                    mAnimationPlayer.state.SetAnimation(0, data.name, data.loop);
                }
                else
                {
                    mAnimationPlayer.state.SetEmptyAnimation(0,0);
                }

                if (data.duration > 0)
                {
                    yield return new WaitForSeconds(data.duration);
                }
                else if (!data.loop)
                {
                    yield return new WaitForSeconds(mAnimationPlayer.skeleton.Data.FindAnimation(data.name).Duration);
                }
            }
        }
#endif
    }
}