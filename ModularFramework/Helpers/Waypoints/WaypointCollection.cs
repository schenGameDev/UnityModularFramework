using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointCollection : MonoBehaviour
{
    [Header("Config")]
    public WaypointPath[] Paths;
    public Vector3 SpawnPos {get;set;}

    Dictionary<string,Vector3[]> _dict = null;

    public Vector3[] GetPath(string name) {
        if(_dict == null) {
            _dict = Paths.ToDictionary(p=>p.Name,p=>p.Points);
        }
        return _dict[name];
    }
    private void OnDrawGizmos() {
        if(!Application.isPlaying && transform.hasChanged) {
            SpawnPos = transform.position;
            Gizmos.DrawRay(SpawnPos, Vector3.forward);

            Vector3 right = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0,180+20,0) * new Vector3(0,0,1);
            Vector3 left = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0,180-20,0) * new Vector3(0,0,1);
            Gizmos.DrawRay(SpawnPos + Vector3.forward, right * 0.25f);
            Gizmos.DrawRay(SpawnPos + Vector3.forward, left * 0.25f);
        }
    }
}
