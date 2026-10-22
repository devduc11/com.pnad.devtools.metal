using UnityEditor;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    public class DevToolsWindow : EditorWindow
    {
        // Danh sách các tab bên cột trái
        private readonly string[] _tabNames = new string[]
        {
            "Init Project",
            "Script Templates",
            "Import Package"
        };

        private int _selectedTabIndex = 0;

        // Mở cửa sổ từ Menu Bar
        [MenuItem("Tools/PNAD DevTools")]
        public static void ShowWindow()
        {
            DevToolsWindow window = GetWindow<DevToolsWindow>("PNAD DevTools");
            window.minSize = new Vector2(750, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // ==========================================
            // CỘT BÊN TRÁI: SIDEBAR (NAVIGATION)
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.Width(200), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            // Tiêu đề Sidebar
            GUILayout.Label("  PNAD DevTools", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Danh sách các tab bấm chọn (Đã fix style)
            _selectedTabIndex = GUILayout.SelectionGrid(
                _selectedTabIndex,
                _tabNames,
                1,
                "LargeButton",
                GUILayout.Height(_tabNames.Length * 38)
            );

            EditorGUILayout.EndVertical();

            // Đường vạch kẻ đứng phân cách 2 cột
            Rect rect = GUILayoutUtility.GetLastRect();
            Handles.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            Handles.DrawLine(new Vector3(rect.xMax, 0, 0), new Vector3(rect.xMax, position.height, 0));

            // ==========================================
            // CỘT BÊN PHẢI: NỘI DUNG TƯƠNG ỨNG
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            // Tiêu đề tab hiện tại
            GUILayout.Label($"  {_tabNames[_selectedTabIndex]}", EditorStyles.largeLabel, GUILayout.Height(30));
            GUILayout.Space(10);

            // Nội dung chi tiết từng Tab
            switch (_selectedTabIndex)
            {
                case 0:
                    DrawInitProjectTab();
                    break;
                case 1:
                    DrawScriptTemplatesTab();
                    break;
                case 2:
                    DrawImportPackageTab();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------
        // TAB 1: INIT PROJECT
        // ------------------------------------------------------------------
        private void DrawInitProjectTab()
        {
            EditorGUILayout.HelpBox(
                "Tự động khởi tạo cấu trúc thư mục chuẩn Assets/_Project",
                MessageType.Info
            );

            GUILayout.Space(20);

            if (GUILayout.Button("🚀 Execute Init Project Structure", GUILayout.Height(45)))
            {
                // Gọi hàm Init từ class InitProjectStructure của bạn
                InitProjectStructure.Init();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📋 Install Script Templates → Assets/ScriptTemplates", GUILayout.Height(45)))
            {
                ScriptTemplateInstaller.Install();
            }

            // Hiển thị cảnh báo và nút Restart sau khi Install có copy file mới
            if (ScriptTemplateInstaller.NeedsRestart)
            {
                GUILayout.Space(15);

                EditorGUILayout.HelpBox(
                    "⚠️ Script Templates đã được cài đặt.\n" +
                    "Unity cần Restart để menu Create cập nhật đầy đủ.",
                    MessageType.Warning
                );

                GUILayout.Space(8);

                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button("🔄 Restart Unity", GUILayout.Height(40)))
                {
                    EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
                }
                GUI.backgroundColor = Color.white;
            }
        }

        // ------------------------------------------------------------------
        // TAB 2: Script Templates
        // ------------------------------------------------------------------
        private void DrawScriptTemplatesTab()
        {
            ScriptTemplatesTab.Draw();
        }

        // ------------------------------------------------------------------
        // TAB 3: Import Package
        // ------------------------------------------------------------------
        private void DrawImportPackageTab()
        {
            ImportPackageTab.Draw();
        }
    }
}