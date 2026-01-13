using UnityEngine;

public class TeleportNode : AstarAINode
{
    [Tooltip("If true, teleport around self, else teleport around 'Target' in blackboard")]
    [SerializeField] private bool isTargetMe;

    [SerializeField] private Vector3[] relativePositions;

    private Transform _target;
    
    protected override void OnEnter()
    {
        base.OnEnter();
        _target = isTargetMe? tree.Me : tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET)?[0];
        if (_target)
        {
            enemyMove?.Move();
        }
    }

    protected override State OnUpdate()
    {
        if(_target == null) return State.Failure;
        
        return Teleport(_target.position)? State.Success : State.Running;
    }

    private bool Teleport(Vector3 target) {
        if(!isActive) return false;
        Vector3 teleportPos = GetTeleportPosition(target);
        tree.AI.Teleport(teleportPos);
        return true;
    }

    private Vector3 GetTeleportPosition(Vector3 target) {
        return target + relativePositions[Random.Range(0, relativePositions.Length)];
    }

    public override BTNode Clone() {
        TeleportNode node = Instantiate(this);
        return node;
    }

    TeleportNode()
    {
        description =  "Teleport around target\n\n" +
                       "<b>Requires</b>: 'Target' in blackboard if 'isTargetMe' is false";
    }
}