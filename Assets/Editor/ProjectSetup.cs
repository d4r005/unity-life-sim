#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Configura automáticamente el proyecto para Android moderno al abrirlo:
/// Target SDK automático (API 34+), min SDK 24, IL2CPP y ARM64.
/// Esto evita el aviso "Esta app no es compatible con la versión más
/// reciente de Android" al instalar la APK.
/// </summary>
[InitializeOnLoad]
public static class ProjectSetup
{
    static ProjectSetup()
    {
        try
        {
            bool dirty = false;

            // Target SDK: automático = la más alta instalada (evita el aviso de compatibilidad)
            if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto)
            {
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                dirty = true;
            }
            // Mínimo Android 7.0
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                dirty = true;
            }
            // IL2CPP + ARM64 (requerido para subir a tienda, y más rápido)
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                dirty = true;
            }
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                dirty = true;
            }
            // Portrait
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                dirty = true;
            }

            if (dirty)
            {
                Debug.Log("[ProjectSetup] Ajustes de Android aplicados: Target SDK Auto (34+), min 24, IL2CPP/ARM64, portrait.");
                AssetDatabase.SaveAssets();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ProjectSetup] No se pudo ajustar la configuración: " + e.Message);
        }
    }
}
#endif
