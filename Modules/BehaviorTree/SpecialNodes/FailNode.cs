using UnityEngine;

public class FailNode : SingletonNode
{
    protected override State OnUpdate()
    {
        return State.Failure;
    }
    
    public override Color HeaderColor  => new Color32(135, 69, 11, 255);
    
    FailNode()
    {
        description = "Always fail";
    }
}
