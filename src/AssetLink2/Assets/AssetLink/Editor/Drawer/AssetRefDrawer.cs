using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;

namespace xpTURN.AssetLink.Editor
{
    /// <summary>
    /// Drawer for displaying AssetRef in the editor.
    /// </summary>
    [CustomPropertyDrawer(typeof(AssetRef), true)]
    public class AssetRefDrawer : PropertyDrawer
    {
        // Use static dictionary to persist warning state per property path
        // PropertyDrawer instances can be reused or recreated, so instance variables may reset
        private static Dictionary<string, bool> s_WarningStates = new ();
        private static Dictionary<string, string> s_WarningStrings = new ();

        private static string GetPropertyKey(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";
        }

        private static bool GetWarningState(SerializedProperty property)
        {
            string key = GetPropertyKey(property);
            return s_WarningStates.TryGetValue(key, out bool value) && value;
        }

        private static void SetWarningState(SerializedProperty property, bool value)
        {
            string key = GetPropertyKey(property);
            s_WarningStates[key] = value;
        }

        private static string GetWarningString(SerializedProperty property)
        {
            string key = GetPropertyKey(property);
            if (!s_WarningStrings.TryGetValue(key, out string value))
                return string.Empty;

            return value;
        }

        private static void SetWarningString(SerializedProperty property, string value)
        {
            string key = GetPropertyKey(property);
            s_WarningStrings[key] = value;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the targetObject and AssetRef
            var targetObject = property.serializedObject.targetObject;
            var assetRef = fieldInfo.GetValue(targetObject) as AssetRef;

            // Adjust label width to reduce gap between label and field when space is tight
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            float minLabelWidth = 50f;
            float labelRatio = 0.35f; // Label takes 35% of available width
            EditorGUIUtility.labelWidth = Mathf.Max(minLabelWidth, position.width * labelRatio);

            // Draw the label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Restore label width
            EditorGUIUtility.labelWidth = originalLabelWidth;

            // Calculate rects
            Type derivedClassType = assetRef?.DerivedClassType ?? typeof(Object);
            bool isSpriteType = derivedClassType == typeof(Sprite);
            bool hasSpriteAtlas = isSpriteType && assetRef?.MainClassType == typeof(SpriteAtlas);

            float atlasButtonWidth = hasSpriteAtlas ? 20f : 0f;
            var objectFieldRect = new Rect(position.x, position.y, position.width - 60 - atlasButtonWidth, EditorGUIUtility.singleLineHeight);
            var atlasDropdownRect = new Rect(position.x + position.width - 60 - atlasButtonWidth, position.y, atlasButtonWidth, EditorGUIUtility.singleLineHeight);
            var resetButtonRect = new Rect(position.x + position.width - 55, position.y, 55, EditorGUIUtility.singleLineHeight);
            var pathFieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            List<AssetReferenceUIRestrictionSurrogate> restrictions = AssetLinkDrawerUtilities.GatherFilters(property);

            // Draw the object field with sub-asset (sub-sprite) selection support
            Object assetOrigin = assetRef?.editorAsset;

            // Use ObjectFieldWithSubAssets for Sprite type to enable sub-sprite selection
            Object asset;
            if (hasSpriteAtlas)
            {
                asset = DrawSpriteObjectField(objectFieldRect, assetOrigin, assetRef, typeof(SpriteAtlas));
            }
            else if (isSpriteType)
            {
                asset = DrawSpriteObjectField(objectFieldRect, assetOrigin, assetRef, typeof(Sprite));
            }
            else
            {
                asset = EditorGUI.ObjectField(objectFieldRect, assetOrigin, derivedClassType, false);
            }

            // Draw sprite atlas dropdown button if SpriteAtlas is assigned
            if (hasSpriteAtlas)
            {
                DrawSpriteAtlasDropdown(atlasDropdownRect, assetRef, targetObject, property);
            }

            // Update the path property if the object field value changes
            if (asset != assetOrigin)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path) && ValidateAsset(assetRef, restrictions, path))
                {
                    SetWarningState(property, false);

                    // Record undo
                    Undo.RecordObject(targetObject, "Change AssetRef");

                    // Handle SpriteAtlas - set it as editor asset
                    if (asset is SpriteAtlas)
                    {
                        assetRef?.SetEditorAsset(asset);
                    }
                    else
                    {
                        // Handle sub-asset (sub-sprite) - pass the asset directly which may be a sub-asset
                        assetRef?.SetEditorAsset(asset);
                    }

                    // Apply changes
                    EditorUtility.SetDirty(targetObject);
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    if (!IsAddressable(path))
                    {
                        SetWarningString(property, "Asset is not addressable");
                        Debug.LogWarning("[AssetRef] The selected asset is not addressable.");
                    }
                    else
                    {
                        SetWarningString(property, $"Asset does not have label : [{AssetLinkDrawerUtilities.GetAllowedLabelsToString(property)}]");
                        Debug.LogWarning("[AssetRef] Assignable assets are restricted to those with specific labels.");
                    }

                    SetWarningState(property, true);

                    Undo.RecordObject(targetObject, "Reset AssetRef");
                    assetRef?.OnSetEditorAsset(null, null);
                    EditorUtility.SetDirty(targetObject);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            // Draw the reset button
            if (GUI.Button(resetButtonRect, "Reset"))
            {
                SetWarningState(property, false);
                SetWarningString(property, string.Empty);

                // Record undo
                Undo.RecordObject(targetObject, "Reset AssetRef");

                // Get the target object and call the Reset method
                assetRef?.OnSetEditorAsset(null, null);

                // Apply changes
                EditorUtility.SetDirty(targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }

            // Display the asset path in a selectable (but read-only) text field
            if (GetWarningState(property))
            {
                string warningText = GetWarningString(property);

                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                EditorGUI.SelectableLabel(pathFieldRect, warningText, EditorStyles.textField);
                GUI.backgroundColor = originalColor;
            }
            else
            {
                EditorGUI.SelectableLabel(pathFieldRect, assetRef?.RuntimeKeyString, EditorStyles.textField);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws an ObjectField for Sprite type that also accepts SpriteAtlas.
        /// </summary>
        private Object DrawSpriteObjectField(Rect position, Object currentObject, AssetRef assetRef, Type objType)
        {            
            // Draw the object field - accepts both Sprite and SpriteAtlas
            Object selectedObject = EditorGUI.ObjectField(position, currentObject, objType, false);
            
            // Handle drag and drop for Sprite and SpriteAtlas
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (position.Contains(evt.mousePosition))
                {
                    bool canAccept = false;
                    Object objectToAccept = null;
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Sprite || draggedObject is SpriteAtlas)
                        {
                            canAccept = true;
                            objectToAccept = draggedObject;
                            break;
                        }
                    }
                    
                    if (canAccept)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            selectedObject = objectToAccept;
                        }
                        
                        evt.Use();
                    }
                }
            }
            
            return selectedObject;
        }

        /// <summary>
        /// Draws a dropdown button to select a sprite from SpriteAtlas.
        /// </summary>
        private void DrawSpriteAtlasDropdown(Rect position, AssetRef assetRef, Object targetObject, SerializedProperty property)
        {
            if (GUI.Button(position, "▼", EditorStyles.miniButton))
            {
                var atlas = assetRef.editorAsset as SpriteAtlas;
                if (atlas != null)
                {
                    SpriteAtlasPopupForRef.Show(atlas, assetRef, targetObject, property);
                }
            }
        }

        private bool ValidateAsset(AssetRef assetRefObject, List<AssetReferenceUIRestrictionSurrogate> restrictions, string path)
        {
            if (!IsAddressable(path))
            {
                return false;
            }

            return AssetLinkDrawerUtilities.ValidateAsset(assetRefObject, restrictions, path);
        }
        
        /// <summary>
        /// Checks if the asset at the given path is registered as an Addressable asset.
        /// </summary>
        private bool IsAddressable(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            return entry != null && entry.address != null;
        }

        /// <summary>
        /// Returns the height of the property drawer.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Two lines: ObjectField + HelpBox, plus 2px spacing between them
            return EditorGUIUtility.singleLineHeight * 2 + 4;
        }
    }
}
