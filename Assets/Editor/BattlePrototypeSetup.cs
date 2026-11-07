#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattlePrototypeSetup
{
    private static readonly string[] ScenePaths = { WorldMap.ScenePath, BattlePrototype.ScenePath };

    public static void PrepareProject()
    {
        EnsureSettings();
        CreateScene();
        EditorBuildSettings.scenes = System.Array.ConvertAll(ScenePaths, path => new EditorBuildSettingsScene(path, true));
        PlayerSettings.productName = "Ashen Oath Tactical Prototype";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Battle/Open Battle Settings")]
    public static void OpenSettings() { Selection.activeObject = EnsureSettings(); }

    private static BattleSettings EnsureSettings()
    {
        const string path = "Assets/Resources/BattleSettings.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var settings = AssetDatabase.LoadAssetAtPath<BattleSettings>(path);
        if (settings == null) { settings = ScriptableObject.CreateInstance<BattleSettings>(); AssetDatabase.CreateAsset(settings, path); }
        return settings;
    }

    public static void BuildWindows()
    {
        PrepareProject();
        string output = System.IO.Path.GetFullPath("Builds/Windows/AshenOath.exe");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = ScenePaths, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + report.summary.result);
        Debug.Log("BATTLE_BUILD_OK: " + output);
    }

    [MenuItem("Battle/Create Test Scene")]
    public static void CreateScene()
    {
        const string path = BattlePrototype.ScenePath;
        EnsureSettings();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (System.IO.File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            return;
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.transform.position = new Vector3(3.5f, 2.5f, -10);
        camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<CameraController>();
        new GameObject("BattlePrototype").AddComponent<BattlePrototype>();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        Selection.activeGameObject = GameObject.Find("BattlePrototype");
        Debug.Log("TestBattle scene created and saved. Press Play to start.");
    }
}
#endif
