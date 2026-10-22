using System.IO;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    public static class ScriptCreator
    {
        /// <summary>
        /// Tạo file script tại đường dẫn chỉ định với nội dung cho trước.
        /// Bỏ qua nếu file đã tồn tại.
        /// </summary>
        public static void CreateScriptFile(string assetPath, string content)
        {
            // Đổi sang đường dẫn hệ thống thực
            string fullPath = Path.GetFullPath(assetPath);

            if (File.Exists(fullPath))
            {
                Debug.LogWarning($"⚠ Script đã tồn tại, bỏ qua: {assetPath}");
                return;
            }

            // Đảm bảo thư mục cha tồn tại
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
            Debug.Log($"📄 Đã tạo script: {assetPath}");
        }

        /// <summary>
        /// Tạo file script, ghi đè nếu file đã tồn tại.
        /// </summary>
        public static void CreateScriptFileOverwrite(string assetPath, string content)
        {
            string fullPath = Path.GetFullPath(assetPath);

            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
            Debug.Log($"📄 Đã ghi đè script: {assetPath}");
        }
    }
}
