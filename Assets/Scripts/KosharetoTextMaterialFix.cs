using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class KosharetoTextMaterialFix : MonoBehaviour
{
    Material textMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Object.FindAnyObjectByType<KosharetoTextMaterialFix>() != null) return;
        GameObject go = new GameObject("Koshareto Text Material Fix");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<KosharetoTextMaterialFix>();
    }

    IEnumerator Start()
    {
        // Koshareto creates its UI at runtime, so wait for the UI hierarchy to exist.
        yield return null;
        yield return null;
        ApplyToAllText();
    }

    void ApplyToAllText()
    {
        Shader shader = Resources.Load<Shader>("KosharetoText");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("Koshareto: project text shader is missing or unsupported.");
            return;
        }

        Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
        Font sourceFont = null;
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font != null)
            {
                sourceFont = texts[i].font;
                break;
            }
        }

        if (sourceFont == null)
        {
            Debug.LogError("Koshareto: no runtime UI font was found.");
            return;
        }

        textMaterial = new Material(shader);
        textMaterial.name = "Koshareto Project Text Material";
        if (sourceFont.material != null && sourceFont.material.mainTexture != null)
            textMaterial.mainTexture = sourceFont.material.mainTexture;

        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null) continue;
            text.material = textMaterial;
        }

        Font.textureRebuilt += OnFontTextureRebuilt;
    }

    void OnFontTextureRebuilt(Font rebuiltFont)
    {
        if (textMaterial == null || rebuiltFont == null || rebuiltFont.material == null) return;
        if (rebuiltFont.material.mainTexture != null)
            textMaterial.mainTexture = rebuiltFont.material.mainTexture;
    }

    void OnDestroy()
    {
        Font.textureRebuilt -= OnFontTextureRebuilt;
        if (textMaterial != null) Destroy(textMaterial);
    }
}
