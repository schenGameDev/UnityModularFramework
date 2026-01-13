using UnityModularFramework;

public abstract class AstarAINode : ActionNode,IReady
{
    public string moveName;
    protected bool isActive;
    protected EnemyMove enemyMove;
    public bool Ready => enemyMove is null || enemyMove.Ready;

    protected override void OnEnter()
    {
        base.OnEnter();
        isActive = true;
        if (!string.IsNullOrEmpty(moveName) && enemyMove == null)
        {
            enemyMove = GetComponentInMe<EnemyMove>(moveName);
        }
        enemyMove?.Move();
    }

    protected override void OnExit()
    {
        tree.AI.Stop();
        enemyMove?.Stop();
        isActive = false;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(moveName) ? "" : $" ({moveName})");
    }
}