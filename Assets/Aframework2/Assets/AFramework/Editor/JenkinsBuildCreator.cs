using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JenkinsBuildCreator
{
    static string[] SCENES = FindEnabledEditorScenes();
    public static int PerformAndroidBuild()
    {
        SwitchPlatformIfNeeded(BuildTarget.Android);
        return BuildGeneric(BuildTarget.Android);
    }

    static int BuildGeneric(BuildTarget target)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.locationPathName = Application.dataPath + "\\..\\";
        buildPlayerOptions.scenes = SCENES;// list of scenes
        buildPlayerOptions.target = target;// target platform

        string copyResultPath = string.Empty;
        string fileName = string.Empty;

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0, count = args.Length; i < count; i++)
        {
            UnityEngine.Debug.Log(
                "GetCommandLineArgs args: " + "i val: " + i +
                " : " +
                args[i]);  // whatever variables we pass from the command line using
                           // unity3D Jenkins plugin we would capture those values here

            if (args[i] == "-targetPath" && i + 1 < count)
            {
                //buildPlayerOptions.locationPathName = args[i + 1];
                copyResultPath = args[i + 1];
                if (copyResultPath.StartsWith("-"))
                {
                    copyResultPath = string.Empty;
                }
            }
            else if (args[i] == "-customFlags" && i + 1 < count)
            {
                var str = args[i + 1];
                if (!copyResultPath.StartsWith("-"))
                {
                    AddDefineSymbols(str.Split(';'));
                }
            }
        }

        //detect file name
        {
            var names = Application.productName.Split(' ');
            if (names.Length > 1)
            {
                fileName = string.Format("\\{0}", (names[0].Substring(0, 1) + names[1].Substring(0, 1)).ToLowerInvariant());
            }
            else
            {
                fileName = string.Format("\\{0}", names[0].Substring(0, 2).ToLowerInvariant());
            }

            var time = DateTime.Now;
            fileName += string.Format("_{0}_v{1}", time.ToString("dd-MM-yy"), Math.Round(time.TimeOfDay.TotalMinutes / 15));
            if (target == BuildTarget.Android) fileName += ".apk";
            buildPlayerOptions.locationPathName += fileName;
        }       

        BuildReport report =
            BuildPipeline.BuildPlayer(buildPlayerOptions);  // generates APK
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            if (!string.IsNullOrEmpty(copyResultPath))
            {
                System.IO.File.Copy(buildPlayerOptions.locationPathName, copyResultPath + "\\" + fileName, true);
            }
            EditorApplication.Exit(0);
            return 0;
        }

        UnityEngine.Debug.Log("Build failed");
        EditorApplication.Exit(1);
        //throw new UnityEditor.Build.BuildFailedException("Failed with error num: " + summary.totalErrors);
        return 1;
    }

    static string[] FindEnabledEditorScenes()
    {
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            EditorScenes.Add(scene.path);
        }
        return EditorScenes.ToArray();
    }
    static void SwitchPlatformIfNeeded(BuildTarget target)
    {
        BuildTarget current = EditorUserBuildSettings.activeBuildTarget;
        UnityEngine.Debug.Log("SwitchPlatformIfNeeded Current buildTarget: " +
                              current);
        if (current != target)
        {
            var targetGroup = BuildTargetGroup.Unknown;
            if (target == BuildTarget.Android) targetGroup = BuildTargetGroup.Android;
            else if (target == BuildTarget.iOS) targetGroup = BuildTargetGroup.iOS;
            else if (target == BuildTarget.WebGL) targetGroup = BuildTargetGroup.WebGL;
            else if (target == BuildTarget.StandaloneWindows
                || target == BuildTarget.StandaloneWindows64
                || target == BuildTarget.StandaloneOSX
                ) targetGroup = BuildTargetGroup.Standalone;

            EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup,
                                                            target);
        }
    }

    static void AddDefineSymbols(string[] addList)
    {
        if (addList == null || addList.Length == 0) return;
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> allDefines = definesString.Split(';').ToList();
        allDefines.AddRange(addList.Except(allDefines));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }
}
