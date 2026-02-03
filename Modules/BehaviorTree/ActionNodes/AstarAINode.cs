public abstract class AstarAINode : ActionNode
{
    public string moveName;
    protected bool isActive;
    protected BTMove BtMove;

    protected override void OnEnter()
    {
        base.OnEnter();
        isActive = true;
        if (!string.IsNullOrEmpty(moveName) && BtMove == null)
        {
            BtMove = GetComponentInMe<BTMove>(moveName);
        }
        BtMove?.Move();
    }

    protected override void OnExit()
    {
        tree.AI.Stop();
        BtMove?.Stop();
        isActive = false;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(moveName) ? "" : $" ({moveName})");
    }
}