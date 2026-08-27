#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class KosharetoPrebuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        string scene = "Assets/Scenes/Main.unity";
        string worldShader = "Assets/Resources/KosharetoMobile.shader";
        string uiShader = "Assets/Resources/KosharetoUI.shader";
        string gameScript = "Assets/Scripts/KosharetoGame.cs";

        if (!File.Exists(scene)) throw new BuildFailedException("Koshareto preflight: Main scene is missing.");
        if (!File.Exists(worldShader)) throw new BuildFailedException("Koshareto preflight: mobile world shader is missing.");
        if (!File.Exists(uiShader)) throw new BuildFailedException("Koshareto preflight: UI shader is missing.");
        if (!File.Exists(gameScript)) throw new BuildFailedException("Koshareto preflight: gameplay script is missing.");

        Shader w = AssetDatabase.LoadAssetAtPath<Shader>(worldShader);
        Shader u = AssetDatabase.LoadAssetAtPath<Shader>(uiShader);
        if (w == null || !w.isSupported) throw new BuildFailedException("Koshareto preflight: mobile world shader failed to import or is unsupported.");
        if (u == null || !u.isSupported) throw new BuildFailedException("Koshareto preflight: UI shader failed to import or is unsupported.");

        if (report.summary.platform == BuildTarget.Android)
        {
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                throw new BuildFailedException("Koshareto preflight: Android orientation is not Portrait.");
            if (PlayerSettings.Android.bundleVersionCode < 3)
                throw new BuildFailedException("Koshareto preflight: versionCode must be at least 3.");
        }

        Debug.Log("Koshareto preflight passed: scene, runtime shaders, portrait settings and version are valid.");
    }
}
#endif
