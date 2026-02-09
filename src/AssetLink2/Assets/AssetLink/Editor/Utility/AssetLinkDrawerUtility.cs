/// <license>
/// Addressables copyright © 2020 Unity Technologies ApS
/// 
/// Licensed under the Unity Companion License for Unity-dependent projects--see [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License).
/// 
/// Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.
/// </license>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.U2D;

using xpTURN.AssetLink;

namespace UnityEditor.AddressableAssets.Settings
{
    using Object = UnityEngine.Object;

    public class AssetLinkDrawerUtilities
    {
        static internal bool ValidateAsset(AssetRef assetRefObject, List<AssetReferenceUIRestrictionSurrogate> restrictions, string path)
        {
            if (assetRefObject != null && assetRefObject.ValidateAsset(path))
            {
                foreach (var restriction in restrictions)
                {
                    if (!restriction.ValidateAsset(path))
                        return false;
                }

                return true;
            }

            return false;
        }

        static internal bool ValidateAsset(AssetLink assetLinkObject, List<AssetReferenceUIRestrictionSurrogate> restrictions, string path)
        {
            if (assetLinkObject != null && assetLinkObject.ValidateAsset(path))
            {
                foreach (var restriction in restrictions)
                {
                    if (!restriction.ValidateAsset(path))
                        return false;
                }

                return true;
            }

            return false;
        }

        static internal List<AssetReferenceUIRestrictionSurrogate> GatherFilters(SerializedProperty property)
        {
            List<AssetReferenceUIRestrictionSurrogate> restrictions = new List<AssetReferenceUIRestrictionSurrogate>();
            var o = property.serializedObject.targetObject;
            if (o != null)
            {
                var t = o.GetType();
                FieldInfo info = null;

                // We need to look into sub types, if any.
                string[] pathParts = property.propertyPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < pathParts.Length; i++)
                {
                    FieldInfo f = t.GetField(pathParts[i],
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null)
                    {
                        t = f.FieldType;
                        info = f;
                    }
                }

                if (info != null)
                {
                    var a = info.GetCustomAttributes(false);
                    foreach (var attr in a)
                    {
                        var uiRestriction = attr as AssetReferenceUIRestriction;
                        if (uiRestriction != null)
                        {
                            var surrogate = AssetReferenceUtility.GetSurrogate(uiRestriction.GetType());

                            if (surrogate != null)
                            {
                                var surrogateInstance =
                                    Activator.CreateInstance(surrogate) as AssetReferenceUIRestrictionSurrogate;
                                if (surrogateInstance != null)
                                {
                                    surrogateInstance.Init(uiRestriction);
                                    restrictions.Add(surrogateInstance);
                                }
                            }
                            else
                            {
                                AssetReferenceUIRestrictionSurrogate restriction =
                                    new AssetReferenceUIRestrictionSurrogate();
                                restriction.Init(uiRestriction);
                                restrictions.Add(restriction);
                            }
                        }
                    }
                }
            }

            return restrictions;
        }

        /// <summary>
        /// Gets the allowed labels from <see cref="AssetReferenceUILabelRestriction"/> on the given field, or null if not present.
        /// </summary>
        public static IReadOnlyList<string> GetAllowedLabels(FieldInfo field)
        {
            if (field == null) return null;
            var attr = field.GetCustomAttribute<AssetReferenceUILabelRestriction>(false);
            return attr?.m_AllowedLabels;
        }

        /// <summary>
        /// Gets the allowed labels from <see cref="AssetReferenceUILabelRestriction"/> on the property's field, or null if not present.
        /// </summary>
        public static IReadOnlyList<string> GetAllowedLabels(SerializedProperty property)
        {
            if (property == null) return null;
            var field = GetFieldInfoFromProperty(property);
            return GetAllowedLabels(field);
        }

        public static string GetAllowedLabelsToString(SerializedProperty property)
        {
            if (property == null) return null;
            var field = GetFieldInfoFromProperty(property);

            var labels = GetAllowedLabels(field);
            if (labels == null || labels.Count == 0) return string.Empty;

            return string.Join(", ", labels);
        }        

        /// <summary>
        /// Resolves the leaf <see cref="FieldInfo"/> for the given serialized property path.
        /// </summary>
        static internal FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            var o = property?.serializedObject?.targetObject;
            if (o == null) return null;

            var t = o.GetType();
            FieldInfo info = null;
            string[] pathParts = property.propertyPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < pathParts.Length; i++)
            {
                var f = t.GetField(pathParts[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f == null) break;
                t = f.FieldType;
                info = f;
            }

            return info;
        }
    }
}