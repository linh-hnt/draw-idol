using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Anim
{
    public class AFUnityAnimator : AFBaseAnimator
    {
        public Animator animator { get; protected set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public override string currentAnimName
        {
            get {
                var current_info = this.animator.GetCurrentAnimatorClipInfo(0);
                return current_info.Length > 0 ? current_info[0].clip.name : string.Empty;
            }
        }

        public override float currentAnimDuration
        {
            get
            {
                var current_info = this.animator.GetCurrentAnimatorStateInfo(0);
                return current_info.length;
            }
        }

        public override float normalizedTime
        {
            get
            {
                return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }
        }

        public override float GetAnimDuration(string name)
        {
            RuntimeAnimatorController ac = animator.runtimeAnimatorController;    //Get Animator controller
            string lowerCase = name.ToLowerInvariant();
            for (int i = 0; i < ac.animationClips.Length; i++)                 //For all animations
            {
                if (ac.animationClips[i].name.ToLowerInvariant() == lowerCase)        //If it has the same name as your clip
                {
                    return ac.animationClips[i].length;
                }
            }
            return -1;
        }

        public override float playSpeed
        {
            get
            {
                return animator.speed;
            }

            set
            {
                animator.speed = value;
            }
        }

        public override bool status
        {
            get
            {
                return animator.enabled;
            }

            set
            {
                animator.enabled = value;
            }
        }

        public override void Play(string animationName, bool loop = false, bool force = false)
        {
            Play(Animator.StringToHash(animationName), loop, force);
        }

        public override void Play(int animationId, bool loop = false, bool force = false)
        {
            if (force)
            {
                animator.Play(animationId, 0, 0);
            }
            else
            {
                animator.Play(animationId);
            }
        }

        public override void CrossFade(string animationName, float duration, float currentAnimTime = 0, float transitionTime = 0)
        {
            CrossFade(Animator.StringToHash(animationName), duration, currentAnimTime, transitionTime);
        }

        public override void CrossFade(int animationId, float duration, float currentAnimTime = 0, float transitionTime = 0)
        {
            animator.CrossFade(animationId, duration, 0, currentAnimTime, transitionTime);
        }
    }
}