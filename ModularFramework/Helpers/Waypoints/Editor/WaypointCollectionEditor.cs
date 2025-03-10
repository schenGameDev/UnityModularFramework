using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaypointCollection))]
public class WaypointCollectionEditor : Editor {
    private WaypointCollection _waypointCollection => target as WaypointCollection;
    private void OnSceneGUI() {

        if(_waypointCollection.Paths==null || _waypointCollection.Paths.Length ==0) {
            return;
        }
        foreach(var path in _waypointCollection.Paths) {
            Handles.color = path.GizmosColor;
            for (int i = 0; i <path.Points.Length; i++) {
                EditorGUI.BeginChangeCheck();
                Vector3 currentPoint = _waypointCollection.SpawnPos + path.Points[i];
                Vector3 newPosition = Handles.FreeMoveHandle(currentPoint, 0.2f, Vector3.one*0.5f, Handles.SphereHandleCap);

                GUIStyle text = new GUIStyle();
                text.fontStyle = FontStyle.Normal;
                text.fontSize = 16;
                text.normal.textColor = Color.black;
                Vector3 textPos = new Vector3(0.1f,-0.1f);
                Handles.Label(_waypointCollection.SpawnPos + path.Points[i] + textPos, $"{i+1}",text);


                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target,"Free Move");
                    path.Points[i] = newPosition - _waypointCollection.SpawnPos;
                }
            }
        }
    }
}