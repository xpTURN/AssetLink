/// <license>
/// Addressables copyright © 2020 Unity Technologies ApS
/// 
/// Licensed under the Unity Companion License for Unity-dependent projects--see [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License).
/// 
/// Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.
/// </license>
using System;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace xpTURN.AssetLink
{
#if UNITY_EDITOR
    class AssetPathToTypes : AssetPostprocessor
    {
        internal static Dictionary<string, HashSet<Type>> s_PathToTypes = new Dictionary<string, HashSet<Type>>();

        public static HashSet<Type> GetTypesForAssetPath(string path)
        {
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));

            if (s_PathToTypes.TryGetValue(path, out HashSet<Type> value))
                return value;

            var objectsForAsset = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            value = AddTypesToPath(objectsForAsset, path);
            return value;
        }

        internal static HashSet<Type> AddTypesToPath(Object[] objectsForAsset, string path)
        {
            HashSet<Type> value = new HashSet<Type>();
            foreach (Object o in objectsForAsset)
            {
                if (o != null)
                    value.Add(o.GetType());
            }

            s_PathToTypes.Add(path, value);
            return value;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
                s_PathToTypes.Remove(str);

            foreach (string str in deletedAssets)
                s_PathToTypes.Remove(str);

            for (int i = 0; i < movedFromAssetPaths.Length; ++i)
            {
                if (s_PathToTypes.TryGetValue(movedFromAssetPaths[i], out var values))
                {
                    s_PathToTypes.Remove(movedFromAssetPaths[i]);
                    s_PathToTypes.Add(movedAssets[i], values);
                }
            }
        }
    }
#endif    
}