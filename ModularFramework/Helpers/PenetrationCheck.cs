using System;
using ModularFramework;
using UnityEngine;

public class PenetrationCheck : MonoBehaviour
{
    public LayerMask obstacleLayers;
    public bool autoResolve = true;
    public bool smoothResolve = true;

    public event Action<Vector3> OnPenetrationStart;
    public event Action<Vector3> OnPenetrationStay;
    public event Action OnPenetrationEnd;

    private Collider collider;
    private bool resolvingCollision;
    
    private void Start()
    {
        collider = GetComponent<Collider>();
        // OnPenetrationStart += correction =>
        // {
        //     float penetrationDepth = correction.magnitude;
        // };
    }

    private void Update()
    {
        bool isColliding = collider.GetPenetrationInLayer(obstacleLayers, out Vector3 correction);

        if (isColliding)
        {
            correction += correction.normalized * 0.001f;
            if(!resolvingCollision) OnPenetrationStart?.Invoke(correction);
            else OnPenetrationStay?.Invoke(correction);
            
            resolvingCollision = true;

            if (autoResolve)
            {
                ResolveCollision(correction);
            }
        }
        else if(resolvingCollision) 
        {
            OnPenetrationEnd?.Invoke();
            resolvingCollision = false;
        }
    }

    private void ResolveCollision(Vector3 correction)
    {
        Vector3 delta = smoothResolve? Vector3.Lerp(Vector3.zero, correction, 0.6f) : correction;
        transform.position += delta;
    }
}