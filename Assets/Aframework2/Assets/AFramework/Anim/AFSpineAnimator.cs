#if USE_SPINE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace AFramework.Anim
{
    public class AFSpineAnimator : AFBaseAnimator
    {
        protected SkeletonAnimation mAnimator;
        public SkeletonAnimation SkeletionAni { get { return mAnimator; } }

        protected Dictionary<int, string> mHashToName = new Dictionary<int, string>();

        protected virtual void Awake()
        {
            mAnimator = GetComponent<SkeletonAnimation>();

            var animationList = mAnimator.Skeleton.Data.Animations.Items;
            for (int i = 0; i < animationList.Length; ++i)
            {
                mHashToName[Animator.StringToHash(animationList[i].Name)] = animationList[i].Name;
            }
        }

        protected virtual void Start()
        {
            mAnimator.state.Event += (entry, e) =>
            {
                if (onAnimTriggerEvent != null)
                {
                    onAnimTriggerEvent(e.Data.Name, new object[] { e.Data.Int, e.Data.Float, e.Data.String });
                }
            };

            mAnimator.state.Complete += (entry) =>
            {
                if (onAnimCompleteCallback != null)
                {
                    onAnimCompleteCallback(entry.Animation.Name);
                }
            };
        }

        public override float normalizedTime
        {
            get
            {
                var currentTrack = mAnimator.state.GetCurrent(0);
                var duration = currentTrack.AnimationEnd - currentTrack.AnimationStart;
                if (duration == 0) return 0;
                if (currentTrack.Loop)
                {
                    return (currentTrack.TrackTime % duration - currentTrack.AnimationStart) / duration;
                }
                return (currentTrack.TrackTime - currentTrack.AnimationStart) / duration;
            }
        }

        float _playSpeed;
        public override float playSpeed
        {
            get
            {
                return _playSpeed;
            }

            set
            {
                _playSpeed = value;
                mAnimator.timeScale = isDisable ? 0 : _playSpeed;
            }
        }

        bool isDisable = false;
        public override bool status
        {
            get
            {
                return !isDisable;
            }

            set
            {
                isDisable = !value;
                mAnimator.timeScale = isDisable ? 0 : _playSpeed;
            }
        }

        public override void Play(string animationName, bool loop = false, bool force = false)
        {
            if (mAnimator.AnimationName == animationName && mAnimator.state.GetCurrent(0).Loop == loop)
            {
                return;
            }
            if(mAnimator.Skeleton.Data.FindAnimation(animationName) == null)
            {
                mAnimator.ClearState();
                return;
            }
            mAnimator.state.SetAnimation(0, animationName, loop);
        }

        public override void Play(int animationId, bool loop = false, bool force = false)
        {
            if(!mHashToName.ContainsKey(animationId))
            {
                mAnimator.ClearState();
                return;
            }
            Play(mHashToName[animationId], loop, force);
        }

        public static void SetupAttachments(GameObject obj, string[] configs)
        {
            var spineAnim = obj.GetComponent<SkeletonAnimation>();
            var skeleton = spineAnim.Skeleton;
            if (skeleton == null) return;

            for (int i = 0; i < skeleton.Slots.Items.Length; ++i)
            {
                var slotData = skeleton.Slots.Items[i];
                slotData.Attachment = null;
            }

            for (int i = 0; i < configs.Length; ++i)
            {
                var data = configs[i].Split('/');
                if (data.Length < 3) continue;
                skeleton.SetAttachment(data[1], data[2]);
            }
        }

        public static void SetupAttachmentsUI(Spine.Skeleton skeleton, string[] configs)
        {
            if (skeleton == null) return;

            for (int i = 0; i < skeleton.Slots.Items.Length; ++i)
            {
                var slotData = skeleton.Slots.Items[i];
                slotData.Attachment = null;
            }

            for (int i = 0; i < configs.Length; ++i)
            {
                var data = configs[i].Split('/');
                if (data.Length < 3) continue;
                skeleton.SetAttachment(data[1], data[2]);
            }
        }

        public override void SetTimeScale(float timeScale)
        {
            base.SetTimeScale(timeScale);

            mAnimator.timeScale = timeScale;
        }

    }
}
#endif