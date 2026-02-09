using UnityEditor;
using UnityEngine;

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Editor
{
    [CustomEditor(typeof(DoAutoRelease))]
    internal sealed class DoAutoReleaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var comp = (DoAutoRelease)target;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Type", comp.Type.ToString());

            if (comp.Type == DoAutoRelease.TYPE.ASSET_SPAWNER)
            {
                EditorGUILayout.TextField("AssetOwner Id", comp.SpawnerId.ToString());

                EditorGUILayout.TextField("Asset Key", comp.RuntimeKey);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
