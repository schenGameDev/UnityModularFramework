using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Waypoint))]
public class WaypointEditor : Editor {
    private Waypoint _waypointTarget => target as Waypoint;
    private void OnSceneGUI() {
        if(_waypointTarget.Points==null || _waypointTarget.Points.Length ==0) {
            return;
        }
        Handles.color = _waypointTarget.GizmosColor;
        for (int i = 0; i < _waypointTarget.Points.Length; i++) {
            EditorGUI.BeginChangeCheck();
            Vector3 currentPoint = _waypointTarget.SpawnPos + _waypointTarget.Points[i];
            Vector3 newPosition = Handles.FreeMoveHandle(currentPoint, 0.2f, Vector3.one*0.5f, Handles.SphereHandleCap);

            GUIStyle text = new GUIStyle();
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 16;
            text.normal.textColor = Color.black;
            Vector3 textPos = new Vector3(0.1f,-0.1f);
            Handles.Label(_waypointTarget.SpawnPos + _waypointTarget.Points[i] + textPos, $"{i+1}",text);


            if(EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(target,"Free Move");
                _waypointTarget.Points[i] = newPosition - _waypointTarget.SpawnPos;
            }
        }
    }
}