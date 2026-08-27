#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
        PlayerSettings.bundleVersion = "0.3.0";
        PlayerSettings.Android.bundleVersionCode = 3;
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

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

        // Koshareto's runtime shaders live under Assets/Resources, so Unity must
        // package them into the player and cannot strip them as unused scene assets.
        if (Resources.Load<Shader>("KosharetoMobile") == null)
            Debug.LogError("Koshareto build guard: KosharetoMobile shader is missing from Resources.");
        if (Resources.Load<Shader>("KosharetoUI") == null)
            Debug.LogError("Koshareto build guard: KosharetoUI shader is missing from Resources.");

        AssetDatabase.SaveAssets();
    }
}
#endif
