using UnityEngine;

public class SuccessNode : SingletonNode
{
    protected override State OnUpdate()
    {
        return State.Success;
    }
    public override Color HeaderColor => new Color32(61, 234, 51, 255);

    SuccessNode()
    {
        description = "Always Success";
    }
}
