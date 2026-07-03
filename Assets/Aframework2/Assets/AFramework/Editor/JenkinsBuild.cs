// -------------------------------------------------------------------------------------------------
// Assets/Editor/JenkinsBuild.cs
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

// ------------------------------------------------------------------------
// https://docs.unity3d.com/Manual/CommandLineArguments.html
// ------------------------------------------------------------------------

namespace AFramework
{
    public class JenkinsBuild
    {

        static string[] EnabledScenes = FindEnabledEditorScenes();

        // ------------------------------------------------------------------------
        // called from Jenkins
        // ------------------------------------------------------------------------

        //[MenuItem("AFramework/Build Android")]
        public static void BuildAndroidDevelopment()
        {            
            string[] args = System.Environment.GetCommandLineArgs();
            var backendScript = ScriptingImplementation.Mono2x;
            string target_dir = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-keystorePass")
                {
                    PlayerSettings.keystorePass = args[i + 1];
                }
                else if (args[i] == "-keyaliasPass")
                {
                    PlayerSettings.keyaliasPass = args[i + 1];
                }
                else if (args[i] == "-androidSdkPath")
                {
                    EditorPrefs.SetString("AndroidSdkRoot", args[i + 1]);
                }
                else if (args[i] == "-androidNdkPath")
                {
                    EditorPrefs.SetString("AndroidNdkRoot", args[i + 1]);
                }
                else if (args[i] == "-scriptBackend")
                {
                    backendScript = (ScriptingImplementation)System.Enum.Parse(typeof(ScriptingImplementation), args[i + 1]);
                }
                else if (args[i] == "-version")
                {
                    PlayerSettings.bundleVersion = args[i + 1];
                    var versionArray = PlayerSettings.bundleVersion.Split('.');
                    int versionNum = 0;
                    int multiplier = 1;
                    for (int xx = versionArray.Length - 1; xx >= 0; --xx)
                    {
                        versionNum += int.Parse(versionArray[xx]) * multiplier;
                        multiplier *= 10;
                    }
                    PlayerSettings.Android.bundleVersionCode = versionNum;
                }
                else if (args[i] == "-outputPath")
                {
                    target_dir = args[i + 1];
                }
            }

            if ((PlayerSettings.keystorePass == null || PlayerSettings.keystorePass.Length <= 1) || (PlayerSettings.keyaliasPass == null || PlayerSettings.keyaliasPass.Length <= 1))
            {
                System.Console.WriteLine("Build failed: no key pass");
                return;
            }

            //FrameworkToolEditor.CheckFirebaseTool(false);

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, backendScript);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;

            if (target_dir == null) target_dir = ".\\";
            target_dir += string.Format("{0}_{1}.apk", Application.productName, Application.version);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EnabledScenes;
            buildPlayerOptions.locationPathName = target_dir;
            buildPlayerOptions.target = BuildTarget.Android;

            // use these options for the first build
            buildPlayerOptions.options = BuildOptions.Development;

            // use these options for building scripts
            // buildPlayerOptions.options = BuildOptions.BuildScriptsOnly | BuildOptions.Development;

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        private static string[] FindEnabledEditorScenes()
        {

            List<string> EditorScenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    EditorScenes.Add(scene.path);
                }
            }
            return EditorScenes.ToArray();
        }
    }
}