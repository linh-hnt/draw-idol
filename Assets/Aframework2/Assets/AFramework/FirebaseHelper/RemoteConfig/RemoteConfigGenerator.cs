using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AFramework
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    public partial class RemoteConfigGenData
    {
        [Button]
        public void CheckCommonError()
        {
            HashSet<string> availableList = new();
            for (int i = 0, count = ConfigInfos.Length; i < count; ++i)
            {
                var info = ConfigInfos[i];
                info.key = info.key.Trim();
                if (!info.IsClass()) info.className = string.Empty;
                else if (!string.IsNullOrEmpty(info.className)) info.className = info.className.Trim();
                if (info.defaultData != null)
                {
                    if (info.defaultData.android != null) info.defaultData.android.Trim();
                    if (info.defaultData.ios != null) info.defaultData.ios.Trim();
                }
                if (!availableList.Add(info.key))
                {
                    Debug.LogError("Duplicate index " + i + " - name " + info.key);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Check common error done");
        }

        [Button]
        public void GenerateCode()
        {
            string filePath = System.IO.Path.GetFullPath(UnityEditor.AssetDatabase.GetAssetPath(this));
            filePath = filePath.Substring(0, filePath.LastIndexOf('\\'));
            filePath += "/GameRemoteConfigGen.cs";
            string fileContent =
                @"using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if USE_BYTE_BREW
using ByteBrewSDK;
#endif

public partial class GameRemoteConfig
{
[constVariableContent]

[publicVariableContent]

[privateVariableContent]

    bool useFirebase = false;
	int useByteBrew = 0;

#if USE_FIREBASE_REMOTECONFIG
    void OnUpdateFirebaseConfig()
    {
        useFirebase = true;
[updateDataFirebaseContent]
        DataChanged = true;
    }
#endif

#if USE_BYTE_BREW
    void OnUpdateBytebrewConfig(bool isLocalCountry)
    {
        useByteBrew = isLocalCountry ? 2 : 1;
[updateDataByteBrewContent]
        DataChanged = true;
    }
#endif

    void SetSaveConfig()
    {
[saveSetDataContent]
        DataChanged = true;
    }

    public partial class [SaveGameClass]
    {
[saveSerializeContent]
    }

    [IngameDebugConsole.ConsoleMethod(""remote.show"", nameof(CheatShowRemoteConfig))]
    public static void CheatShowRemoteConfig()
	{
		var obj = FindObjectOfType<GameRemoteConfig>();
		if (obj == null)
		{
			Debug.LogError(""GameRemoteConfig obj null"");
			return;
		}

		var config = obj.GenConfig;
        if (config == null)
        {
            Debug.LogError(""GameRemoteConfig config null"");
            return;
        }

		for (int i = 0, count = config.ConfigInfos.Length; i < count; ++i)
		{
			var info = config.ConfigInfos[i];

#if USE_BYTE_BREW
			if (obj.useByteBrew > 0)
			{
				string remote = string.Empty;
				if (obj.useByteBrew == 2) remote = ByteBrew.GetRemoteConfigForKey(info.key + ""_local"", string.Empty);
				if (string.IsNullOrEmpty(remote)) remote = ByteBrew.GetRemoteConfigForKey(info.key, string.Empty);
                Debug.Log(string.Format(""<color=yellow><b>ByteBrew:</b></color> {0} / {1}"", info.key, !string.IsNullOrEmpty(remote) ? remote : ""default""));
            }
#endif
#if USE_FIREBASE_REMOTECONFIG
            if (obj.useFirebase)
			{
				var remote = obj.GetValue(info.key);
                Debug.Log(string.Format(""<color=yellow><b>Firebase:</b></color> {0} / {1}"", info.key, remote != null ? remote.GetStringValue() : ""default""));
            }
#endif
		}
    }
}
";

            string constVariableContent = "";
            string publicVariableContent = "";
            string privateVariableContent = "";
            string updateDataFirebaseContent = "";
            string updateDataByteBrewContent = "";
            string saveSetDataContent = "";
            string saveSerializeContent = "";

            for (int i = 0, count = ConfigInfos.Length; i < count; ++i)
            {
                RemoteConfigInfo info = ConfigInfos[i];
                string strEscape = (i < count - 1) ? "\n" : string.Empty;

                string constVariable = $"\tpublic const string {info.key.ToUpperInvariant()} = \"{info.key}\";{strEscape}";
                constVariableContent += constVariable;

                string publicVariable = $"\t[ShowInInspector] public {info.TypeString} {info.key} => {_saveVarName}.{info.SaveKey};{strEscape}";
                publicVariableContent += publicVariable;

                //string defaultVariable = $"\t[SerializeField, FoldoutGroup(\"Default value\")] {info.TypeString} {info.KeyDefault};{strEscape}";
                //privateVariableContent += defaultVariable;

                string updateFirebase = $"\t\tvar {info.ConfigKey} = GetValue(\"{info.key}\");\n";
                if (info.IsNullConvertable()) updateFirebase += $"\t\tif ({info.ConfigKey} != null && !string.IsNullOrEmpty({info.ConfigKey}.GetStringValue()))\n";
                else updateFirebase += $"\t\tif ({info.ConfigKey} != null)\n";
                updateFirebase += $"\t\t{{\n";
                if (info.customUpdate)
                {
                    updateFirebase += $"\t\t\tif({info.CustomUpdateFunc}(true, {info.ConfigKey}.GetStringValue()))\n";
                    updateFirebase += $"\t\t\t{{\n";
                    updateFirebase += $"\t\t\t\t{_saveVarName}.{info.SaveKey} = {info.ConfigKey}.GetStringValue();\n";
                    updateFirebase += $"\t\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 1;\n";
                    updateFirebase += $"\t\t\t}}\n";
                }
                else
                {
                    updateFirebase += $"\t\t\t{_saveVarName}.{info.SaveKey} = {info.FirebaseParseValue};\n";
                    updateFirebase += $"\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 1;\n";
                }
                updateFirebase += $"\t\t}}\n";
                updateFirebase += $"\t\telse\n";
                updateFirebase += $"\t\t{{\n";
                updateFirebase += $"\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 0;\n";
                if (info.customUpdate)
                    updateFirebase += $"\t\t\t{info.CustomUpdateFunc}(false, GenConfig.ConfigInfos[{i}].defaultData.current);\n";
                else
                {
                    var inputStr = $"GenConfig.ConfigInfos[{i}].defaultData.current";
                    updateFirebase += $"\t\t\t{_saveVarName}.{info.SaveKey} = {info.ParseValue(inputStr)};\n";
                }
                updateFirebase += $"\t\t}}{strEscape}";
                updateDataFirebaseContent += updateFirebase;

                string updateByteCrew = $"\t\tstring {info.ConfigKey} = string.Empty;\n";
                updateByteCrew += $"\t\tif (isLocalCountry) {info.ConfigKey} = ByteBrew.GetRemoteConfigForKey(\"{info.key}_local\", string.Empty);\n";
                updateByteCrew += $"\t\tif (string.IsNullOrEmpty({info.ConfigKey})) {info.ConfigKey} = ByteBrew.GetRemoteConfigForKey(\"{info.key}\", string.Empty);\n";
                updateByteCrew += $"\t\tif (string.IsNullOrEmpty({info.ConfigKey}))\n";
                updateByteCrew += $"\t\t{{\n";
                updateByteCrew += $"\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 0;\n";
                if (info.customUpdate)
                    updateByteCrew += $"\t\t\t{info.CustomUpdateFunc}(false, GenConfig.ConfigInfos[{i}].defaultData.current);\n";
                else
                {
                    var inputStr = $"GenConfig.ConfigInfos[{i}].defaultData.current";
                    updateByteCrew += $"\t\t\t{_saveVarName}.{info.SaveKey} = {info.ParseValue(inputStr)};\n";
                }
                updateByteCrew += $"\t\t}}\n";
                updateByteCrew += $"\t\telse\n";
                updateByteCrew += $"\t\t{{\n";
                if (info.customUpdate)
                {
                    updateByteCrew += $"\t\t\tif({info.CustomUpdateFunc}(true, {info.ConfigKey}))\n";
                    updateByteCrew += $"\t\t\t{{\n";
                    updateByteCrew += $"\t\t\t\t{_saveVarName}.{info.SaveKey} = {info.ConfigKey};\n";
                    updateByteCrew += $"\t\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 1;\n";
                    updateByteCrew += $"\t\t\t}}\n";
                }
                else
                {
                    updateByteCrew += $"\t\t\t{_saveVarName}.{info.SaveKey} = {info.ByteCrewParseValue};\n";
                    updateByteCrew += $"\t\t\t{_saveVarName}.{info.UseRemoteSaveKey} = 1;\n";
                }
                updateByteCrew += $"\t\t}}{strEscape}";
                updateDataByteBrewContent += updateByteCrew;

                string setSave = string.Empty;
                if (info.customUpdate)
                {
                    setSave += $"\t\tif ({_saveVarName}.{info.UseRemoteSaveKey} == 0) {info.CustomUpdateFunc}(false, GenConfig.ConfigInfos[{i}].defaultData.current);\n";
                    setSave += $"\t\telse {info.CustomUpdateFunc}(false, {_saveVarName}.{info.SaveKey});\n";
                }
                else
                {
                    var inputStr = $"GenConfig.ConfigInfos[{i}].defaultData.current";
                    setSave += $"\t\tif ({_saveVarName}.{info.UseRemoteSaveKey} == 0) {_saveVarName}.{info.SaveKey} = {info.ParseValue(inputStr)};{strEscape}";
                }
                saveSetDataContent += setSave;

                string saveVariable = $"\t\tpublic {info.TypeString} {info.SaveKey};\n";
                saveVariable += $"\t\tpublic byte {info.UseRemoteSaveKey} = 0;{strEscape}";
                saveSerializeContent += saveVariable;
            }

            fileContent = fileContent
            .Replace("[constVariableContent]", constVariableContent)
            .Replace("[publicVariableContent]", publicVariableContent)
            .Replace("[privateVariableContent]", privateVariableContent)
            .Replace("[updateDataFirebaseContent]", updateDataFirebaseContent)
            .Replace("[updateDataByteBrewContent]", updateDataByteBrewContent)
            .Replace("[saveSetDataContent]", saveSetDataContent)
            .Replace("[SaveGameClass]", _saveClassName)
            .Replace("[saveSerializeContent]", saveSerializeContent)
            ;
            System.IO.File.WriteAllText(filePath, fileContent);

            UnityEditor.AssetDatabase.Refresh();
        }
    }
#endif
}