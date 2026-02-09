using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

using UnityEditor;

namespace xpTURN.AssetLink.Editor
{
    /// <summary>
    /// Popup window for selecting a sprite from a SpriteAtlas.
    /// </summary>
    public class SpriteAtlasPopupForLink : EditorWindow
    {
        private AssetLink m_AssetLink;
        private Object m_TargetObject;
        private SerializedProperty m_Property;
        
        private string m_SearchString = "";
        private Vector2 m_ScrollPosition;
        private List<SpriteInfo> m_Sprites = new List<SpriteInfo>();
        private List<SpriteInfo> m_FilteredSprites = new List<SpriteInfo>();
        
        private struct SpriteInfo
        {
            public Sprite sprite;
            public string name;
            public Texture2D preview;
        }

        public static SpriteAtlasPopupForLink Show(SpriteAtlas atlas, AssetLink assetLink, Object targetObject, SerializedProperty property)
        {
            var window = CreateInstance<SpriteAtlasPopupForLink>();
            window.Initialize(atlas, assetLink, targetObject, property);
            
            // Position near mouse
            Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(mousePos.x, mousePos.y, 300, 400);
            window.ShowPopup();
            window.Focus();
            
            return window;
        }

        private void Initialize(SpriteAtlas atlas, AssetLink assetLink, Object targetObject, SerializedProperty property)
        {
            m_AssetLink = assetLink;
            m_TargetObject = targetObject;
            m_Property = property;
            
            titleContent = new GUIContent("Select Sprite");
            
            // Get all sprites from atlas
            int spriteCount = atlas.spriteCount;
            Sprite[] sprites = new Sprite[spriteCount];
            atlas.GetSprites(sprites);
            
            m_Sprites.Clear();
            foreach (var sprite in sprites)
            {
                if (sprite == null) continue;
                
                string spriteName = sprite.name;
                if (spriteName.EndsWith("(Clone)"))
                    spriteName = spriteName.Substring(0, spriteName.Length - 7);
                
                m_Sprites.Add(new SpriteInfo
                {
                    sprite = sprite,
                    name = spriteName,
                    preview = AssetPreview.GetAssetPreview(sprite)
                });
            }
            
            // Sort by name
            m_Sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            UpdateFilteredList();
        }

        private void UpdateFilteredList()
        {
            m_FilteredSprites.Clear();
            
            if (string.IsNullOrEmpty(m_SearchString))
            {
                m_FilteredSprites.AddRange(m_Sprites);
            }
            else
            {
                foreach (var info in m_Sprites)
                {
                    if (info.name.IndexOf(m_SearchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m_FilteredSprites.Add(info);
                    }
                }
            }
        }

        private void OnGUI()
        {
            // Handle escape key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                return;
            }

            // Search field
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            m_SearchString = EditorGUILayout.TextField(m_SearchString, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList();
            }
            EditorGUILayout.EndHorizontal();

            // Sprite list
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            
            if (m_FilteredSprites.Count == 0)
            {
                EditorGUILayout.LabelField("No sprites found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var info in m_FilteredSprites)
                {
                    bool isSelected = m_AssetLink.SubObjectName == info.name;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Sprite preview
                    Rect previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
                    if (info.preview != null)
                    {
                        GUI.DrawTexture(previewRect, info.preview, ScaleMode.ScaleToFit);
                    }
                    else if (info.sprite != null && info.sprite.texture != null)
                    {
                        GUI.DrawTexture(previewRect, info.sprite.texture, ScaleMode.ScaleToFit);
                    }
                    
                    // Sprite name button
                    GUIStyle buttonStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button(info.name, buttonStyle, GUILayout.Height(32)))
                    {
                        SelectSprite(info);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Separator line
                    Rect separatorRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void SelectSprite(SpriteInfo info)
        {
            Undo.RecordObject(m_TargetObject, "Select Sprite from Atlas");
            m_AssetLink.SetAssetName(m_AssetLink.AssetName, info.name);
            EditorUtility.SetDirty(m_TargetObject);
            m_Property.serializedObject.ApplyModifiedProperties();
            Close();
        }

        private void OnLostFocus()
        {
            // Close when focus is lost
            Close();
        }
    }
}
