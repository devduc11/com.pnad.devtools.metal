using System.IO;
using UnityEditor;
using UnityEngine;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PNAD.DevTools.Editor
{
    /// <summary>
    /// Copy các file Script Template từ package Runtime/ScriptTemplates/
    /// vào Assets/ScriptTemplates/ để Unity nhận diện và hiển thị
    /// trong menu Create của Project window.
    /// </summary>
    internal static class ScriptTemplateInstaller
    {
        // Thư mục đích Unity đọc Script Templates
        private const string DESTINATION_PATH = "Assets/ScriptTemplates";

        // ----------------------------------------------------------------
        // State
        // ----------------------------------------------------------------

        // True sau khi Install() copy ít nhất 1 file mới → cần restart Unity
        public static bool NeedsRestart { get; private set; } = false;

        // ----------------------------------------------------------------
        // Tìm đường dẫn vật lý của package (hoạt động với mọi cách cài)
        // ----------------------------------------------------------------
        private static string FindSourceAbsolutePath()
        {
            // Ưu tiên 1: UPM package (GitHub / registry / Packages/ folder)
            var packageInfo = UpmPackageInfo.FindForAssembly(typeof(ScriptTemplateInstaller).Assembly);
            if (packageInfo != null)
            {
                return Path.Combine(packageInfo.resolvedPath, "Runtime", "ScriptTemplates");
            }

            // Ưu tiên 2: Embedded package (Assets/Packages/)
            // Tìm file script này trong AssetDatabase → derive package root
            string[] guids = AssetDatabase.FindAssets($"{nameof(ScriptTemplateInstaller)} t:Script");
            if (guids.Length > 0)
            {
                string scriptAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                // scriptAssetPath = "Assets/Packages/com.pnad.devtools/Editor/ScriptTemplateInstaller.cs"
                string editorDir    = Path.GetDirectoryName(scriptAssetPath); // .../Editor
                string packageRoot  = Path.GetDirectoryName(editorDir);       // .../com.pnad.devtools
                return Path.GetFullPath(Path.Combine(packageRoot, "Runtime", "ScriptTemplates"));
            }

            Debug.LogError("[ScriptTemplateInstaller] ❌ Không tìm thấy thông tin package.");
            return null;
        }

        // ----------------------------------------------------------------
        // Entry point
        // ----------------------------------------------------------------
        public static void Install()
        {
            string sourceAbsolute = FindSourceAbsolutePath();

            if (string.IsNullOrEmpty(sourceAbsolute) || !Directory.Exists(sourceAbsolute))
            {
                Debug.LogError($"[ScriptTemplateInstaller] ❌ Không tìm thấy thư mục nguồn: {sourceAbsolute}");
                return;
            }

            // Tạo Assets/ScriptTemplates nếu chưa có
            if (!AssetDatabase.IsValidFolder(DESTINATION_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptTemplates");
                Debug.Log($"[ScriptTemplateInstaller] 📁 Đã tạo thư mục: {DESTINATION_PATH}");
            }

            string[] txtFiles = Directory.GetFiles(sourceAbsolute, "*.txt", SearchOption.TopDirectoryOnly);

            if (txtFiles.Length == 0)
            {
                Debug.LogWarning("[ScriptTemplateInstaller] ⚠️ Không tìm thấy file .txt nào trong thư mục nguồn.");
                return;
            }

            int copiedCount  = 0;
            int skippedCount = 0;

            foreach (string srcFile in txtFiles)
            {
                string fileName = Path.GetFileName(srcFile);
                string destFile = Path.Combine(Path.GetFullPath(DESTINATION_PATH), fileName);

                // Bỏ qua nếu file đích đã giống hệt (so sánh nội dung)
                if (File.Exists(destFile) && FilesAreIdentical(srcFile, destFile))
                {
                    skippedCount++;
                    continue;
                }

                File.Copy(srcFile, destFile, overwrite: true);
                copiedCount++;
                Debug.Log($"[ScriptTemplateInstaller] ✅ Đã copy: {fileName}");
            }

            AssetDatabase.Refresh();

            if (copiedCount > 0)
            {
                NeedsRestart = true;
            }

            Debug.Log($"[ScriptTemplateInstaller] 🎉 Hoàn tất! " +
                      $"Copy: {copiedCount} file | Bỏ qua (không đổi): {skippedCount} file");
        }

        // ----------------------------------------------------------------
        // So sánh nội dung 2 file (tránh copy thừa)
        // ----------------------------------------------------------------
        private static bool FilesAreIdentical(string path1, string path2)
        {
            byte[] bytes1 = File.ReadAllBytes(path1);
            byte[] bytes2 = File.ReadAllBytes(path2);

            if (bytes1.Length != bytes2.Length) return false;

            for (int i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i]) return false;
            }

            return true;
        }
    }
}
