using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using Object = UnityEngine.Object;

using xpTURN.Link;

namespace xpTURN.Editor
{
    /// <summary>
    /// Drawer for displaying AssetLink in the editor.
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectLink<>))]
    [CustomPropertyDrawer(typeof(PrefabLink))]
    [CustomPropertyDrawer(typeof(SceneLink))]
    public class AssetLinkDrawer : PropertyDrawer
    {
        bool _warningNotAddressableAsset = false;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the targetObject and AssetLink
            var targetObject = property.serializedObject.targetObject;
            var assetLink = fieldInfo.GetValue(targetObject) as AssetLink;

            // Draw the label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Calculate rects
            var objectFieldRect = new Rect(position.x, position.y, position.width - 60, EditorGUIUtility.singleLineHeight);
            var resetButtonRect = new Rect(position.x + position.width - 55, position.y, 55, EditorGUIUtility.singleLineHeight);
            var pathFieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            // Draw the object field
            Object assetOrgin = assetLink?.AssetForInspector;
            var asset = EditorGUI.ObjectField(objectFieldRect, assetOrgin, assetLink.LinkableTypeForInspector, false);

            // Update the path property if the object field value changes
            if (asset != assetOrgin)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    if (IsAddressable(path))
                    {
                        _warningNotAddressableAsset = false;

                        // 
                        var key = AddressableKeyFinder.FindAddressableKeyFromPath(path);
                        assetLink?.AssignKey(key);
                        assetLink?.AssignAsset(asset);
                    }
                    else
                    {
                        Debug.LogWarning("[AssetLink] The selected asset is not addressable.");
                        _warningNotAddressableAsset = true;

                        // 
                        assetLink?.Reset();
                    }
                }
            }

            // Draw the reset button
            if (GUI.Button(resetButtonRect, "Reset"))
            {
                _warningNotAddressableAsset = false;

                // Get the target object and call the Reset method
                assetLink?.Reset();

                property.serializedObject.ApplyModifiedProperties();
            }

            // Check if the path is addressable and display a warning if not
            if (_warningNotAddressableAsset)
            {
                EditorGUI.HelpBox(pathFieldRect, "Asset is not addressable.", MessageType.Warning);
            }
            else
            {
                EditorGUI.HelpBox(pathFieldRect, assetLink?.Key, MessageType.None);
            }

            EditorGUI.EndProperty();
        }

        private bool IsAddressable(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            return entry != null && entry.address != null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 3;
        }
    }
}
