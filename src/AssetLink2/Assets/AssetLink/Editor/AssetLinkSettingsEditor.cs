using UnityEditor;
using UnityEngine;

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Editor
{
    [CustomEditor(typeof(AssetLinkSettings))]
    public sealed class AssetLinkSettingsEditor : UnityEditor.Editor
    {
        private string tooltipArea =
            "[/]AddressableAssets[/](.+)$\n" +
            "│     │                       │ │  └─ end of string\n" +
            "│     │                       │ └─ capture group: one or more characters\n" +
            "│     │                       └─ path separator\n" +
            "│     └─ literal \"AddressableAssets\"\n" +
            "└─ / path separator";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(tooltipArea);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Save"))
            {
                //
                AssetLinkSettings.ClearEditorCache();

                //
                var settings = (AssetLinkSettings)target;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
