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
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.Android.bundleVersionCode = 2;
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

        // Prefer a conservative mobile graphics API for this procedural prototype.
        // This avoids device-specific Vulkan issues while we stabilise the visual stack.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

        EnsureRuntimeShadersAreIncluded();
        AssetDatabase.SaveAssets();
    }

    static void EnsureRuntimeShadersAreIncluded()
    {
        // Koshareto currently creates its world and UI at runtime. Without explicit
        // references Unity can strip the shaders from an Android player build,
        // causing the classic bright-pink missing-shader screen.
        Object settingsObject = GraphicsSettings.GetGraphicsSettings();
        if (settingsObject == null)
        {
            Debug.LogWarning("Koshareto: GraphicsSettings object was not available.");
            return;
        }

        SerializedObject serialized = new SerializedObject(settingsObject);
        SerializedProperty alwaysIncluded = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncluded == null)
        {
            Debug.LogWarning("Koshareto: Always Included Shaders property was not found.");
            return;
        }

        string[] shaderNames =
        {
            "Standard",
            "UI/Default",
            "GUI/Text Shader",
            "Sprites/Default"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("Koshareto: shader not found in editor: " + shaderName);
                continue;
            }

            bool exists = false;
            for (int i = 0; i < alwaysIncluded.arraySize; i++)
            {
                if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                int index = alwaysIncluded.arraySize;
                alwaysIncluded.InsertArrayElementAtIndex(index);
                alwaysIncluded.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                Debug.Log("Koshareto: keeping runtime shader in Android build: " + shaderName);
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsObject);
    }
}
#endif
