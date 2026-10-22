using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    public static class InitProjectStructure
    {
        private const string ROOT_PATH = "Assets/_Project";
        private const string OLD_SCENES_PATH = "Assets/Scenes";
        private const string NEW_SCENES_PATH = ROOT_PATH + "/Scenes";

        private const string SAMPLE_SCENE_NAME = "SampleScene.unity";
        private const string BOOTSTRAP_SCENE_NAME = "BootstrapScene.unity";
        private const string GAME_SCENE_NAME = "GameScene.unity";

        private static readonly string[] DefaultFolders =
        {
            "Scripts/Base",
            "Scripts/Constant",
            "Scripts/UI/LoadingUI",
        };

        private static readonly string[] DefaultScenes =
        {
            BOOTSTRAP_SCENE_NAME,
            GAME_SCENE_NAME,
        };

        // [MenuItem("Tools/Init Project Structure")]
        public static void Init()
        {
            // Nếu được gọi từ trong OnGUI (ví dụ bấm Button trên EditorWindow),
            // hoãn sang delayCall để tránh EditorSceneManager làm hỏng stack LayoutGroup của IMGUI.
            if (Event.current != null)
            {
                EditorApplication.delayCall += ExecuteInit;
                return;
            }

            ExecuteInit();
        }

        private static void ExecuteInit()
        {
            foreach (var folder in DefaultFolders)
            {
                CreateFolderRecursive($"{ROOT_PATH}/{folder}");
            }

            // ===== SCENES LOGIC =====
            HandleScenesFolder();
            CreateDefaultScenes();
            RemoveSampleScene();
            CleanupOldScenesFolder();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ Init Project Structure DONE");
        }

        // ==============================
        private static void HandleScenesFolder()
        {
            // Không có Assets/Scenes → chỉ cần đảm bảo _Project/Scenes tồn tại
            if (!AssetDatabase.IsValidFolder(OLD_SCENES_PATH))
            {
                CreateFolderRecursive(NEW_SCENES_PATH);
                Debug.Log("ℹ No Assets/Scenes found → ensured _Project/Scenes exists");
                return;
            }

            // Có Assets/Scenes nhưng chưa có _Project/Scenes → move nguyên folder
            if (!AssetDatabase.IsValidFolder(NEW_SCENES_PATH))
            {
                CreateFolderRecursive(ROOT_PATH);
                var result = AssetDatabase.MoveAsset(OLD_SCENES_PATH, NEW_SCENES_PATH);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogError($"❌ Move Scenes folder failed: {result}");
                }
                else
                {
                    Debug.Log("➡ Moved Assets/Scenes → _Project/Scenes");
                }
                return;
            }

            // ĐÃ có cả Assets/Scenes và _Project/Scenes → move từng scene (bỏ qua SampleScene)
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { OLD_SCENES_PATH });
            var movedCount = 0;

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileName(scenePath);

                if (string.Equals(fileName, SAMPLE_SCENE_NAME, StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetPath = $"{NEW_SCENES_PATH}/{fileName}";
                var error = AssetDatabase.MoveAsset(scenePath, targetPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"⚠ Could not move scene '{scenePath}' → '{targetPath}': {error}");
                }
                else
                {
                    movedCount++;
                }
            }

            Debug.Log($"➡ Moved {movedCount} scene file(s) into _Project/Scenes");
        }

        private static void CreateDefaultScenes()
        {
            CreateFolderRecursive(NEW_SCENES_PATH);

            foreach (var sceneFileName in DefaultScenes)
            {
                var scenePath = $"{NEW_SCENES_PATH}/{sceneFileName}";
                if (File.Exists(Path.GetFullPath(scenePath)))
                {
                    Debug.Log($"ℹ Scene already exists: {scenePath}");
                    continue;
                }

                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var saved = EditorSceneManager.SaveScene(newScene, scenePath);
                if (saved)
                {
                    Debug.Log($"🎬 Created scene: {scenePath}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to save scene: {scenePath}");
                }
            }

            // Mở BootstrapScene làm scene mặc định trên Editor
            var bootstrapPath = $"{NEW_SCENES_PATH}/{BOOTSTRAP_SCENE_NAME}";
            if (File.Exists(Path.GetFullPath(bootstrapPath)))
            {
                EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Single);
            }
        }

        private static void RemoveSampleScene()
        {
            var candidatePaths = new[]
            {
                $"{OLD_SCENES_PATH}/{SAMPLE_SCENE_NAME}",
                $"{NEW_SCENES_PATH}/{SAMPLE_SCENE_NAME}",
            };

            foreach (var path in candidatePaths)
            {
                if (!File.Exists(Path.GetFullPath(path)))
                    continue;

                if (AssetDatabase.DeleteAsset(path))
                {
                    Debug.Log($"🗑 Deleted default SampleScene: {path}");
                }
            }
        }

        private static void CleanupOldScenesFolder()
        {
            if (AssetDatabase.IsValidFolder(OLD_SCENES_PATH) && IsFolderEmpty(OLD_SCENES_PATH))
            {
                AssetDatabase.DeleteAsset(OLD_SCENES_PATH);
                Debug.Log($"🗑 Deleted empty folder: {OLD_SCENES_PATH}");
            }
        }

        private static void UpdateBuildSettings()
        {
            var buildScenes = new List<EditorBuildSettingsScene>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Đảm bảo BootstrapScene (index 0) và GameScene (index 1) đứng đầu Build Settings
            foreach (var sceneFileName in DefaultScenes)
            {
                var scenePath = $"{NEW_SCENES_PATH}/{sceneFileName}";
                if (File.Exists(Path.GetFullPath(scenePath)))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    addedPaths.Add(scenePath);
                }
            }

            // 2. Giữ lại các scene hợp lệ khác đã có trong Build Settings (loại bỏ scene đã xóa như SampleScene)
            foreach (var existingScene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(existingScene.path))
                    continue;

                if (addedPaths.Contains(existingScene.path))
                    continue;

                if (!File.Exists(Path.GetFullPath(existingScene.path)))
                    continue;

                buildScenes.Add(existingScene);
                addedPaths.Add(existingScene.path);
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("⚙ Updated Build Settings: BootstrapScene (0), GameScene (1)");
        }

        public static void CreateFolderRecursive(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var parts = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var currentPath = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }

        private static bool IsFolderEmpty(string assetFolderPath)
        {
            var fullPath = Path.GetFullPath(assetFolderPath);
            if (!Directory.Exists(fullPath)) return false;

            return Directory.GetFiles(fullPath).Length == 0 &&
                   Directory.GetDirectories(fullPath).Length == 0;
        }
    }
}
