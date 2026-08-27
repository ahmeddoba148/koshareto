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
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 10;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
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

        // Conservative mobile rendering path: stable on a broad range of Android GPUs.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

        Shader world = Resources.Load<Shader>("KosharetoMobile");
        Shader ui = Resources.Load<Shader>("KosharetoUI");
        if (world == null || !world.isSupported)
            Debug.LogError("Koshareto build guard: KosharetoMobile shader is missing or unsupported.");
        if (ui == null || !ui.isSupported)
            Debug.LogError("Koshareto build guard: KosharetoUI shader is missing or unsupported.");

        AssetDatabase.SaveAssets();
    }
}
#endif
