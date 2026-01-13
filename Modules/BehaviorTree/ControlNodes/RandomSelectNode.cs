using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class RandomSelectNode : ControlNode
{
    public bool pickAgainIfFailed = false;
    public bool onlyIncludeReadyChildren = false;
    public bool equalChance = true;
    [HideField(nameof(equalChance))] public int[] weights;
    
    private int _index;
    private HashSet<BTNode> picked = new HashSet<BTNode>();
    protected override void OnEnter()
    {
        picked.Clear();
        PickChild();
    }

    private void PickChild()
    {
        var ReadyChildren = GetAvailableChildren();
        if (ReadyChildren.Count == 0) 
        {
            // Debug.LogWarning($"No ready children in {title} Node.");
            currentRunningChild = null;
            return;
        }
        _index = 0;
        if (equalChance)
        {
            _index = Random.Range(0,ReadyChildren.Count);
        }
        else if (weights.Length < ReadyChildren.Count)
        {
            Debug.LogError($"Weights length is less than children count in {title} Node. Falling back to equal chance.");
            _index = Random.Range(0,ReadyChildren.Count);
        } 
        else
        {
            int totalWeight = 0;
            for (int i = 0; i < ReadyChildren.Count; i++)
            {
                totalWeight += weights[i];
            }

            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < ReadyChildren.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue < cumulativeWeight)
                {
                    _index = i;
                    break;
                }
            }
        }
        currentRunningChild = ReadyChildren[_index];
        picked.Add(currentRunningChild);
    }
    
    
    private List<BTNode> GetAvailableChildren()
    {
        List<BTNode> readyChildren = new List<BTNode>();
        foreach (var child in Children)
        {
            if (!picked.Contains(child) && (!onlyIncludeReadyChildren || IsReady(child)))
            {
                readyChildren.Add(child);
            }
        }
        return readyChildren;
    }


    protected override State OnUpdate()
    {
        if(currentRunningChild == null) return State.Failure;
        
        var res = currentRunningChild.Run();
         if(pickAgainIfFailed && res == State.Failure)
         {
             PickChild();
             return State.Running;
         }
         
         return res;
    }
    
    public override BTNode Clone() {
        var clone = base.Clone() as RandomSelectNode;
        clone.weights = weights;
        clone.pickAgainIfFailed = pickAgainIfFailed;
        clone.onlyIncludeReadyChildren = onlyIncludeReadyChildren;
        return clone;
    }

    public override string ToString()
    {
        return base.ToString() + (equalChance ? "" : " (Weighted)");
    }

    RandomSelectNode()
    {
        description = "Run a random child";
    }
}