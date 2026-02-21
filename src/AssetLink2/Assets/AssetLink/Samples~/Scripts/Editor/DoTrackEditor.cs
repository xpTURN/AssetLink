using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DoTrack))]
public class DoTrackEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var doTrack = (DoTrack)target;

        if (GUILayout.Button("DoStart - Test"))
        {
            doTrack.SendMessage("DoStart", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("ReleaseUnreferenced - Test"))
        {
            doTrack.SendMessage("ReleaseUnreferenced", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(1);

        if (GUILayout.Button("DetectAndReportLeaks - Test"))
        {
            doTrack.SendMessage("DetectAndReportLeaks", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("DoAutoRelease - Test"))
        {
            doTrack.SendMessage("DoAutoRelease", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("DoUnloadSceneLink - Test"))
        {
            doTrack.SendMessage("DoUnloadSceneLink", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(1);

        if (GUILayout.Button("DoUnloadSceneRef - Test"))
        {
            doTrack.SendMessage("DoUnloadSceneRef", options: SendMessageOptions.DontRequireReceiver);
        }

        EditorGUILayout.Space(12);

        if (GUILayout.Button("DoLeakScenario - Test"))
        {
            doTrack.SendMessage("DoLeakScenario", options: SendMessageOptions.DontRequireReceiver);
        }

    }
}
