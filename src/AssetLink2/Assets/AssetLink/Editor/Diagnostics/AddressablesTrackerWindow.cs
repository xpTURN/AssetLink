using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Editor
{
    /// <summary>
    /// Editor window for viewing AddressablesTracker diagnostics (tracked handles, unreferenced state, request time, stack trace).
    /// </summary>
    public class AddressablesTrackerWindow : EditorWindow
    {
        private IReadOnlyDictionary<string, List<AddressablesTracker.TrackedHandleDTO>> _snapshot;
        private Vector2 _scrollPosition;
        private Vector2 _detailScrollPosition;
        private string _selectedKey;
        private int _selectedHandleIndex = -1;
        private int _currentPage;

        private bool _isCapturing;
        private bool _showUnreferencedOnly;
        private string _keyFilter = "";
        private string _keyFilterApplied = "";

        private const int PageSize = 20;
        private const int MaxPageButtons = 10;

        [MenuItem("Window/AssetLink/Addressables Tracker")]
        public static void Open()
        {
            var window = GetWindow<AddressablesTrackerWindow>("Addressables Tracker");
            window.minSize = new Vector2(400, 300);
        }

        private void OnFocus()
        {
            _keyFilter = _keyFilterApplied ?? "";
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Snapshot", GUILayout.Width(120)))
                    CaptureSnapshot();

                _showUnreferencedOnly = GUILayout.Toggle(_showUnreferencedOnly, "Unreferenced", GUILayout.Width(94));

                GUILayout.Label("Filter", GUILayout.Width(36));
                _keyFilter = EditorGUILayout.TextField(_keyFilter ?? "", GUILayout.Width(100));
                if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    _keyFilterApplied = _keyFilter ?? "";

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Release Unreferenced", GUILayout.Width(160)))
                {
                    AddressablesTracker.ReleaseUnreferencedHandles(true);
                    CaptureSnapshot();
                }
            }

            EditorGUILayout.Space(4);

            if (_isCapturing)
            {
                EditorGUILayout.HelpBox("Capturing snapshot…", MessageType.Info);
                return;
            }

            if (_snapshot == null || _snapshot.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _snapshot == null ? "Click Capture Snapshot to load data." : "No tracked handles. (Only available in Play mode.)",
                    MessageType.Info);
                return;
            }

            var totalHandles = _snapshot.Sum(kv => kv.Value.Count);
            var unreferencedCount = _snapshot.Sum(kv => kv.Value.Count(h => h.Unreferenced));

            var visibleOrdered = _snapshot
                .OrderBy(x => x.Key)
                .Where(kv => (!_showUnreferencedOnly || kv.Value.Any(h => h.Unreferenced))
                    && (string.IsNullOrEmpty(_keyFilterApplied) || kv.Key.IndexOf(_keyFilterApplied, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            var hasFilter = _showUnreferencedOnly || !string.IsNullOrEmpty(_keyFilterApplied);
            var filteredKeys = visibleOrdered.Count;
            var filteredHandles = visibleOrdered.Sum(kv => kv.Value.Count);
            var filteredUnreferenced = visibleOrdered.Sum(kv => kv.Value.Count(h => h.Unreferenced));

            var countLabel = hasFilter
                ? $"Keys: {_snapshot.Count} (filtered: {filteredKeys})  |  Handles: {totalHandles} (filtered: {filteredHandles})  |  Unreferenced: {unreferencedCount} (filtered: {filteredUnreferenced})"
                : $"Keys: {_snapshot.Count}  |  Handles: {totalHandles}  |  Unreferenced: {unreferencedCount}";
            EditorGUILayout.LabelField(countLabel);
            EditorGUILayout.Space(2);
            var totalPages = visibleOrdered.Count == 0 ? 1 : (visibleOrdered.Count + PageSize - 1) / PageSize;
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
            var pageItems = visibleOrdered.Skip(_currentPage * PageSize).Take(PageSize).ToList();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandHeight(true)))
            {
                _scrollPosition = scroll.scrollPosition;

                foreach (var kv in pageItems)
                {
                    var key = kv.Key;
                    var handles = kv.Value;
                    var unreferenced = handles.Count(h => h.Unreferenced);

                    var bgColor = unreferenced > 0 ? new Color(1f, 0.9f, 0.9f) : Color.white;
                    using (new EditorGUIUtilityScope(bgColor))
                    {
                        var expanded = _selectedKey == key;
                        var newExpanded = EditorGUILayout.Foldout(expanded, $"{key}  ({handles.Count})  [Unreferenced: {unreferenced}]", true);
                        if (newExpanded != expanded)
                        {
                            _selectedKey = newExpanded ? key : null;
                            _selectedHandleIndex = -1;
                        }

                        if (newExpanded && handles.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            for (var i = 0; i < handles.Count; i++)
                            {
                                var hi = handles[i];
                                if (_showUnreferencedOnly && !hi.Unreferenced)
                                    continue;
                                var label = $"[{hi.AssetOwnerId}] {hi.RequestTime:HH:mm:ss}  Unreferenced={hi.Unreferenced}  Status={hi.Status}";
                                var isSelected = _selectedKey == key && _selectedHandleIndex == i;
                                if (GUILayout.Toggle(isSelected, label, EditorStyles.miniButton) != isSelected)
                                {
                                    _selectedKey = key;
                                    _selectedHandleIndex = i;
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(_currentPage == 0);
                if (GUILayout.Button("<", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                    _currentPage = Mathf.Max(0, _currentPage - 1);
                EditorGUI.EndDisabledGroup();

                GUILayout.Label("Page", GUILayout.Width(32));
                var visiblePageStart = (_currentPage / MaxPageButtons) * MaxPageButtons;
                var visiblePageEnd = Mathf.Min(visiblePageStart + MaxPageButtons - 1, totalPages - 1);
                for (var p = visiblePageStart; p <= visiblePageEnd; p++)
                {
                    var pageNum = p + 1;
                    if (p == _currentPage)
                        GUILayout.Label($"[{pageNum}]", EditorStyles.boldLabel, GUILayout.Width(36));
                    else if (GUILayout.Button($"[{pageNum}]", EditorStyles.miniButtonMid, GUILayout.Width(36)))
                        _currentPage = p;
                }

                EditorGUI.BeginDisabledGroup(_currentPage >= totalPages - 1);
                if (GUILayout.Button(">", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                    _currentPage = Mathf.Min(totalPages - 1, _currentPage + 1);
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(2);

            if (!string.IsNullOrEmpty(_selectedKey) && _snapshot.TryGetValue(_selectedKey, out var list) &&
                _selectedHandleIndex >= 0 && _selectedHandleIndex < list.Count)
            {
                DrawHandleDetail(list[_selectedHandleIndex], _selectedKey);
            }
        }

        private void DrawStackTraceWithLinks(StackTrace trace)
        {
            if (trace == null) return;

            var linkStyle = new GUIStyle(EditorStyles.label)
            {
                richText = false,
                wordWrap = false,
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f) },
                hover = { textColor = new Color(0.2f, 0.5f, 1f) }
            };
            var labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = false };

            var entries = trace.GetFrameEntries().ToList();
            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("(No frames or paths available)");
                return;
            }
            foreach (var (relativePath, lineNumber, methodDisplay) in entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{methodDisplay} at ", labelStyle, GUILayout.ExpandWidth(false));
                    var linkText = $"{relativePath}:{lineNumber}";
                    if (GUILayout.Button(linkText, linkStyle, GUILayout.ExpandWidth(false)))
                        OpenAssetAtLine(relativePath, lineNumber);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private static void OpenAssetAtLine(string assetPath, int lineNumber)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null)
                AssetDatabase.OpenAsset(obj, lineNumber);
        }

        private void DrawHandleDetail(AddressablesTracker.TrackedHandleDTO handleInfo, string key)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Key", key);
                if (handleInfo.Unreferenced) GUI.contentColor = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.LabelField("Unreferenced", handleInfo.Unreferenced.ToString());
                if (handleInfo.Unreferenced) GUI.contentColor = Color.white;
                EditorGUILayout.LabelField("AssetOwnerId", handleInfo.AssetOwnerId.ToString());
                EditorGUILayout.LabelField("SpawnCount", handleInfo.SpawnCount.ToString());
                EditorGUILayout.LabelField("Handle Valid", handleInfo.IsHandleValid.ToString());
                EditorGUILayout.LabelField("Status", handleInfo.Status ?? "Invalid");
                EditorGUILayout.LabelField("Request Time", handleInfo.RequestTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            if (handleInfo.RequestTrace != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Stack Trace", EditorStyles.boldLabel);
                using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScrollPosition, GUILayout.MaxHeight(120)))
                {
                    _detailScrollPosition = scroll.scrollPosition;
                    DrawStackTraceWithLinks(handleInfo.RequestTrace);
                }
            }
        }

        private IEnumerator CaptureSnapshotCoroutine()
        {
            _isCapturing = true;
            try
            {
                // Clear Hierarchy selection (Editor UI update)
                Selection.objects = new UnityEngine.Object[] { };
                Selection.activeObject = null;
                Selection.activeGameObject = null;
                Selection.activeObject = FindAnyObjectByType<GameObject>();
                Selection.activeGameObject = FindAnyObjectByType<GameObject>();

                // Run GC
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Run GC
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Yield until UnloadUnusedAssets completes (avoids blocking the editor instead of busy-wait)
                yield return Resources.UnloadUnusedAssets();

                // Run GC
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                yield return new WaitForSeconds(0.1f);

                // Capture snapshot
                AddressablesTracker.ReleaseTrackedHandleDTOsToPool(_snapshot);
                _snapshot = AddressablesTracker.GetTrackedHandlesSnapshotForEditor();
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private void CaptureSnapshot()
        {
            _selectedKey = null;
            _selectedHandleIndex = -1;
            _detailScrollPosition = Vector2.zero;
            _scrollPosition = Vector2.zero;
            _currentPage = 0;

            if (!Application.isPlaying)
            {
                return;
            }

            EditorCoroutineUtility.StartCoroutine(CaptureSnapshotCoroutine(), this);
        }

        private struct EditorGUIUtilityScope : IDisposable
        {
            private readonly Color _prev;

            public EditorGUIUtilityScope(Color backgroundColor)
            {
                _prev = GUI.backgroundColor;
                GUI.backgroundColor = backgroundColor;
            }

            public void Dispose()
            {
                GUI.backgroundColor = _prev;
            }
        }
    }
}
