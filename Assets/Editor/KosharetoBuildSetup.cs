#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KosharetoBuildSetup
{
    static KosharetoBuildSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        PlayerSettings.productName = "Koshareto";
        PlayerSettings.companyName = "Koshareto Studio";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
#pragma warning disable 0618
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.koshareto.game");
#pragma warning restore 0618
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.resizableWindow = false;
        PlayerSettings.Android.renderOutsideSafeArea = true;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        AssetDatabase.SaveAssets();
    }
}
#endif
