using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace PNAD.DevTools.Editor
{
    /// <summary>
    /// Quản lý toàn bộ UI và logic cho Tab "Import Package".
    /// Được gọi từ <see cref="DevToolsWindow"/>.
    /// </summary>
    internal static class ImportPackageTab
    {
        // ----------------------------------------------------------------
        // Cấu trúc mô tả một package entry
        // ----------------------------------------------------------------
        private class PackageEntry
        {
            public string Name;         // Tên hiển thị
            public string ImportUrl;    // URL dùng để import qua UPM
            public string InfoUrl;      // URL trang chủ / repo để tham khảo (tuỳ chọn)
            public string Note;         // Ghi chú thêm hiển thị dưới tên (tuỳ chọn)
            public string Token;        // Token xác thực cần copy thủ công (tuỳ chọn)
            public string TokenLabel;   // Label mô tả token (tuỳ chọn)

            // State riêng của từng package
            public AddRequest Request;
            public bool IsImporting;
        }

        // ----------------------------------------------------------------
        // Danh sách package
        // Thêm package mới vào đây để tự động sinh UI
        // ----------------------------------------------------------------
        private static readonly List<PackageEntry> Packages = new List<PackageEntry>
        {
            new PackageEntry
            {
                Name      = "EagleSDK",
                ImportUrl = "https://github.com/dat-dangba/EagleSDK.git",
                InfoUrl   = "https://github.com/dat-dangba/EagleSDK",
                Note = "SDK Token  (Log Level: Verbose)",
            },
            new PackageEntry
            {
                Name      = "MetalSDK",
                ImportUrl = "https://github.com/dat-dangba/MetalSDK.git",
                InfoUrl   = "https://github.com/dat-dangba/MetalSDK",
                Note = "SDK Token  (Log Level: Verbose)",
            },
            new PackageEntry
            {
                Name      = "UI Effect",
                ImportUrl = "https://github.com/mob-sakai/UIEffect.git?path=Packages/src#5.9.0",
                InfoUrl   = "https://github.com/mob-sakai/UIEffect",
                Note      = "v5.9.0 — mob-sakai/UIEffect",
            },
            new PackageEntry
            {
                Name      = "UI Particle",
                ImportUrl = "https://github.com/mob-sakai/ParticleEffectForUGUI.git",
                InfoUrl   = "https://github.com/mob-sakai/ParticleEffectForUGUI",
                Note      = "mob-sakai/ParticleEffectForUGUI",
            },
            // new PackageEntry
            // {
            //     Name      = "PNAD DevTools",
            //     ImportUrl = "https://github.com/devduc11/dev_tools.git?path=Assets/Packages/com.pnad.devtools#main",
            //     InfoUrl   = "https://github.com/devduc11/dev_tools",
            //     Note      = "PNAD DevTools (Latest Main Branch)",
            // },
        };

        // ----------------------------------------------------------------
        // State chung
        // ----------------------------------------------------------------
        private static Vector2 ScrollPos;

        // ----------------------------------------------------------------
        // Entry point: được gọi mỗi frame từ DevToolsWindow.OnGUI()
        // ----------------------------------------------------------------
        public static void Draw()
        {
            EditorGUILayout.HelpBox(
                "Import package cần thiết của dự án.",
                MessageType.Info
            );

            GUILayout.Space(10);

            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            GUILayout.Space(5);

            foreach (PackageEntry pkg in Packages)
            {
                DrawPackageEntry(pkg);
                GUILayout.Space(10);
                DrawSeparator();
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();

            // Poll tiến trình import tất cả package
            PollAllImports();
        }

        // ----------------------------------------------------------------
        // Vẽ UI cho một package entry
        // ----------------------------------------------------------------
        private static void DrawPackageEntry(PackageEntry pkg)
        {
            // Tên package
            EditorGUILayout.LabelField(pkg.Name, EditorStyles.boldLabel);

            // Ghi chú (nếu có)
            if (!string.IsNullOrEmpty(pkg.Note))
            {
                EditorGUILayout.LabelField(pkg.Note, EditorStyles.miniLabel);
            }

            GUILayout.Space(4);

            // Import URL
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Import URL:", GUILayout.Width(78));
                EditorGUILayout.SelectableLabel(
                    pkg.ImportUrl,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight)
                );
            }

            // Info URL (nếu có)
            if (!string.IsNullOrEmpty(pkg.InfoUrl))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Repo:", GUILayout.Width(78));
                    EditorGUILayout.SelectableLabel(
                        pkg.InfoUrl,
                        EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)
                    );
                }
            }

            // Token (nếu có)
            if (!string.IsNullOrEmpty(pkg.Token))
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField(pkg.TokenLabel ?? "Token:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(
                    pkg.Token,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight)
                );
            }

            GUILayout.Space(8);

            // Nút Import
            using (new EditorGUI.DisabledScope(pkg.IsImporting))
            {
                string btnLabel = pkg.IsImporting ? $"⏳ Importing {pkg.Name}..." : $"📦 Import {pkg.Name}";
                if (GUILayout.Button(btnLabel, GUILayout.Height(40)))
                {
                    StartImport(pkg);
                }
            }
        }

        // ----------------------------------------------------------------
        // Đường kẻ phân cách giữa các package
        // ----------------------------------------------------------------
        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        // ----------------------------------------------------------------
        // Bắt đầu import một package
        // ----------------------------------------------------------------
        private static void StartImport(PackageEntry pkg)
        {
            Debug.Log($"[ImportPackageTab] 🚀 Bắt đầu import {pkg.Name}: {pkg.ImportUrl}");
            pkg.IsImporting = true;
            pkg.Request     = Client.Add(pkg.ImportUrl);
        }

        // ----------------------------------------------------------------
        // Poll trạng thái tất cả AddRequest (gọi mỗi frame trong OnGUI)
        // ----------------------------------------------------------------
        private static void PollAllImports()
        {
            foreach (PackageEntry pkg in Packages)
            {
                if (pkg.Request == null || !pkg.IsImporting) continue;
                if (!pkg.Request.IsCompleted) continue;

                pkg.IsImporting = false;

                if (pkg.Request.Status == StatusCode.Success)
                {
                    Debug.Log($"[ImportPackageTab] ✅ Import thành công: {pkg.Request.Result.displayName} ({pkg.Request.Result.version})");
                }
                else if (pkg.Request.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"[ImportPackageTab] ❌ Import thất bại [{pkg.Name}]: {pkg.Request.Error.message}");
                }

                pkg.Request = null;
            }
        }
    }
}
