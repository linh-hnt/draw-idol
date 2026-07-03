#if USE_SPINE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineAttachmentSetup : MonoBehaviour
{
    [SpineAttachment(false, true)]
    public string[] AttachmentConfigs;

    private void Start()
    {
        SetupDefaultAttachment();
    }

#if UNITY_EDITOR
    string[] cacheConfigs;
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlaying) return;
        if (AFramework.Utility.IsPrefab(this.gameObject)) return;

        if (!AFramework.Utility.CompareArray<string>(cacheConfigs, AttachmentConfigs))
        {
            SetupDefaultAttachment();
            cacheConfigs = (string[])AttachmentConfigs.Clone();
        }
    }
#endif

    void SetupDefaultAttachment()
    {
        var spineAnim = this.GetComponent<SkeletonAnimation>();
        var skeleton = spineAnim.Skeleton;
        if (skeleton == null) return;

        for (int i = 0; i < skeleton.Slots.Items.Length; ++i)
        {
            var slotData = skeleton.Slots.Items[i];
            slotData.Attachment = null;
        }

        for (int i = 0; i < AttachmentConfigs.Length; ++i)
        {
            var configs = AttachmentConfigs[i].Split('/');
            if (configs.Length < 3) continue;
            skeleton.SetAttachment(configs[1], configs[2]);
        }
    }

}
#endif