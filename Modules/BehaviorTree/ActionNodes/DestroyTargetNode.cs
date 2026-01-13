using UnityEngine;

public class DestroyTargetNode : TriggerTargetMethodNode<Transform>
{
    protected override void TriggerMethodOnTargets(Transform t)
    {
        Bounds bounds = new Bounds(t.position, new Vector3(10, 10, 10));
        Destroy(t.gameObject);
        AstarPath.active.UpdateGraphs(bounds);
    }

    DestroyTargetNode()
    {
        description = "Destroy target gameObject\n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
    
}