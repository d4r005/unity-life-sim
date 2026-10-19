#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Al abrir el proyecto (o recompilar) se asegura de que la escena activa
/// tenga un GameObject con GameManager, y la guarda como Assets/Scenes/Main.unity
/// si la escena actual aún no está guardada. Así el proyecto abre listo para dar Play.
/// </summary>
[InitializeOnLoad]
public static class SceneBootstrap
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    static SceneBootstrap()
    {
        EditorApplication.delayCall += EnsureScene;
    }

    private static void EnsureScene()
    {
        // No tocar nada mientras el juego corre.
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Si ya existe un GameManager en la escena, no hacer nada.
        if (Object.FindObjectOfType(typeof(GameManager)) != null) return;

        // Si ya existe la escena guardada, abrirla.
        if (System.IO.File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindObjectOfType(typeof(GameManager)) == null)
            {
                var existing = new GameObject("GameManager");
                existing.AddComponent<GameManager>();
                EditorSceneManager.MarkSceneDirty(existing.scene);
            }
            return;
        }

        // Crear el GameObject en la escena activa.
        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();

        // Guardar la escena (si no tiene ruta, guardarla como Main.unity).
        var scene = go.scene;
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Escena creada: " + ScenePath + " (pulsa Play para jugar).");
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
