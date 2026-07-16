using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class DestroyTargetNode : TriggerTargetMethodNode<Transform>
    {
        protected override void TriggerMethodOnTargets(Transform t)
        {
            Bounds bounds = new Bounds(t.position, new Vector3(10, 10, 10));
            tree.Log($"Destroy {t.name}.");
            Destroy(t.gameObject);
            AstarPath.active.UpdateGraphs(bounds);
        }

        DestroyTargetNode()
        {
            description = "Destroy target gameObject\n\n" +
                          "<b>Requires</b>: 'Target' in blackboard";
        }

    }
}