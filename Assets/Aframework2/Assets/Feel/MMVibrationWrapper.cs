using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.NiceVibrations
{
    public enum HapticTypes { Selection, Success, Warning, Failure, LightImpact, MediumImpact, HeavyImpact, RigidImpact, SoftImpact, None }

    public static class MMVibrationManager
    {
        public static void Haptic(HapticTypes type, bool defaultToRegularVibrate = false, bool alsoRumble = false, MonoBehaviour coroutineSupport = null, int controllerID = -1)
        {
            Lofelt.NiceVibrations.HapticPatterns.PresetType preset = Lofelt.NiceVibrations.HapticPatterns.PresetType.None;
            if (type != HapticTypes.None) preset = (Lofelt.NiceVibrations.HapticPatterns.PresetType)((int)type);
            Lofelt.NiceVibrations.HapticPatterns.PlayPreset(preset);
        }

        public static void SetHapticsActive(bool status)
        {
            Lofelt.NiceVibrations.HapticController.hapticsEnabled = status;
        }

        public static void TransientHaptic(float intensity, float sharpness, bool alsoRumble = false, MonoBehaviour coroutineSupport = null, int controllerID = -1)
        {
            Lofelt.NiceVibrations.HapticPatterns.PlayEmphasis(intensity, sharpness);
        }
    }
}