#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class KosharetoBuildSetup
{
    static KosharetoBuildSetup()
    {
        EditorApplication.delayCall += ApplyReleaseSettings;
    }

    public static void ApplyReleaseSettings()
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

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.koshareto.game");

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.resizeableActivity = false;
        PlayerSettings.Android.renderOutsideSafeArea = true;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

        Shader world = Resources.Load<Shader>("KosharetoMobile");
        Shader ui = Resources.Load<Shader>("KosharetoUI");
        Shader text = Resources.Load<Shader>("KosharetoText");
        if (world == null || !world.isSupported)
            Debug.LogError("Koshareto build guard: KosharetoMobile shader is missing or unsupported.");
        if (ui == null || !ui.isSupported)
            Debug.LogError("Koshareto build guard: KosharetoUI shader is missing or unsupported.");
        if (text == null || !text.isSupported)
            Debug.LogError("Koshareto build guard: KosharetoText shader is missing or unsupported.");

        // Do not add Unity built-in shaders to Always Included Shaders here.
        // Built-in assets can carry HideFlags.DontSave and break Cloud Build export.
        AssetDatabase.SaveAssets();
    }
}
#endif
