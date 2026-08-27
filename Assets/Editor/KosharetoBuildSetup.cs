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

        // Force portrait deterministically for local, batchmode and Unity Build Automation.
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

        // Stable compatibility profile for the first Android release.
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

        KeepBuiltInShader("GUI/Text Shader");
        AssetDatabase.SaveAssets();
    }

    static void KeepBuiltInShader(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError("Koshareto build guard: required built-in shader not found: " + shaderName);
            return;
        }

        Object settingsObject = GraphicsSettings.GetGraphicsSettings();
        if (settingsObject == null) return;
        SerializedObject serialized = new SerializedObject(settingsObject);
        SerializedProperty alwaysIncluded = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncluded == null) return;

        for (int i = 0; i < alwaysIncluded.arraySize; i++)
        {
            if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
        }

        int index = alwaysIncluded.arraySize;
        alwaysIncluded.InsertArrayElementAtIndex(index);
        alwaysIncluded.GetArrayElementAtIndex(index).objectReferenceValue = shader;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsObject);
    }
}
#endif
