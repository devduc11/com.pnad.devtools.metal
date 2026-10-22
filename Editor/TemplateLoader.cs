using System.IO;
using UnityEditor;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    /// <summary>
    /// Đọc nội dung template từ file .cs.txt trên disk bằng System.IO.
    /// Tự động tìm đường dẫn package — hoạt động đúng cho cả embedded (Assets/Packages/)
    /// lẫn UPM git package (Library/PackageCache/).
    /// </summary>
    internal static class TemplateLoader
    {
        private const string TEMPLATE_FOLDER_NAME = "TemplateFiles";

        /// <summary>
        /// Tìm đường dẫn tuyệt đối tới thư mục TemplateFiles của package.
        /// Dùng AssetDatabase để locate file TemplateLoader.cs → suy ra package root.
        /// </summary>
        private static string ResolveTemplateDir()
        {
            // Tìm chính file này trong AssetDatabase (hoạt động với mọi cách cài đặt package)
            string[] guids = AssetDatabase.FindAssets($"{nameof(TemplateLoader)} t:Script");
            if (guids.Length > 0)
            {
                string scriptAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                // scriptAssetPath = ".../com.pnad.devtools/Editor/TemplateLoader.cs"
                string editorDir = Path.GetDirectoryName(scriptAssetPath); // .../Editor
                string templateDir = Path.Combine(editorDir, "ScriptTemplates", TEMPLATE_FOLDER_NAME);
                return Path.GetFullPath(templateDir);
            }

            Debug.LogError("[TemplateLoader] Không tìm thấy package path qua AssetDatabase.");
            return null;
        }

        /// <summary>
        /// Đọc nội dung file template theo tên file (ví dụ: "DataSave.cs.txt").
        /// </summary>
        /// <param name="fileName">Tên file template, bao gồm phần mở rộng .cs.txt</param>
        /// <returns>Nội dung file dưới dạng string</returns>
        public static string Load(string fileName)
        {
            string dir = ResolveTemplateDir();
            if (dir == null) return string.Empty;

            string fullPath = Path.Combine(dir, fileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[TemplateLoader] Template not found: {fullPath}");
                return string.Empty;
            }

            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
