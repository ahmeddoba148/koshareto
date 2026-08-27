#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public class KosharetoPrebuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -1000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        // Unity Build Automation can begin before InitializeOnLoad delayCall runs.
        // Apply the release profile synchronously here so validation never sees stale settings.
        KosharetoBuildSetup.ApplyReleaseSettings();

        string[] requiredFiles =
        {
            "Assets/Scenes/Main.unity",
            "Assets/Resources/KosharetoMobile.shader",
            "Assets/Resources/KosharetoUI.shader",
            "Assets/Scripts/KosharetoGame.cs",
            "Assets/Scripts/KosharetoUI.cs",
            "Assets/Scripts/KosharetoWorld.cs"
        };

        for (int i = 0; i < requiredFiles.Length; i++)
        {
            if (!File.Exists(requiredFiles[i]))
                throw new BuildFailedException("Koshareto preflight: missing required file: " + requiredFiles[i]);
        }

        Shader world = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/KosharetoMobile.shader");
        Shader ui = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/KosharetoUI.shader");
        if (world == null || !world.isSupported)
            throw new BuildFailedException("Koshareto preflight: mobile world shader failed to import or is unsupported.");
        if (ui == null || !ui.isSupported)
            throw new BuildFailedException("Koshareto preflight: UI shader failed to import or is unsupported.");
        if (Shader.Find("GUI/Text Shader") == null)
            throw new BuildFailedException("Koshareto preflight: built-in text shader is unavailable.");

        if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length == 0 || !EditorBuildSettings.scenes[0].enabled)
            throw new BuildFailedException("Koshareto preflight: Main scene is not enabled in Build Settings.");

        if (report.summary.platform == BuildTarget.Android)
        {
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                throw new BuildFailedException("Koshareto preflight: failed to force Android Portrait orientation.");
            if (PlayerSettings.Android.bundleVersionCode != 10)
                throw new BuildFailedException("Koshareto preflight: Android versionCode is not 10.");
            if (PlayerSettings.bundleVersion != "1.0.0")
                throw new BuildFailedException("Koshareto preflight: bundle version is not 1.0.0.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.koshareto.game")
                throw new BuildFailedException("Koshareto preflight: Android package id is incorrect.");

            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (apis == null || apis.Length != 1 || apis[0] != GraphicsDeviceType.OpenGLES3)
                throw new BuildFailedException("Koshareto preflight: Android must use OpenGLES3 only.");
        }

        Debug.Log("Koshareto v1 preflight passed after forcing release settings synchronously.");
    }
}
#endif
