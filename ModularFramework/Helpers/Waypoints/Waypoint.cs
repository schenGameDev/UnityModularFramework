using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Config")]
    public Color GizmosColor = Color.red;
    [SerializeField] private Vector3[] _points;

    public Vector3[] Points => _points;
    public Vector3 SpawnPos {get;set;}
    private void Start() {
        SpawnPos = transform.position;
    }

    public Vector3 GetPosition(int pointIndex) {
        return SpawnPos + _points[pointIndex];
    }

    private void OnDrawGizmos() {
        if(!Application.isPlaying && transform.hasChanged) {
            SpawnPos = transform.position;
        }
    }
}
