using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public class ReadyNode : DecoratorNode
{
    [SerializeField,Tooltip("type/type_uniqueId")] private string[] componentIds;
    [SerializeField] private bool matchAny = false;
    
    private readonly List<IReady> _readyComponents = new List<IReady>();
    private bool _isReady;
    
    public override void Prepare()
    {
        base.Prepare();
        Dictionary<string, List<string>> typeIds = new();
        List<string> names = new();
        foreach (var componentId in componentIds)
        {
            var parts = componentId.Split('_',2);
            var typeName = parts[0];
            var ids = typeIds.GetOrCreateDefault(typeName);
            
            if (parts.Length == 2)
            {
                ids.Add(parts[1]);
            }
        }
        foreach (var component in tree.Me.GetComponents<IReady>())
        {
            var typeName = component.GetType().Name;
            if(!typeIds.TryGetValue(typeName, out var ids)) continue;
            if(ids.IsEmpty())
            {
                _readyComponents.Add(component);
                names.Add(typeName);
            }
            else
            {
                var uniqueId = (component as IUniqueIdentifiable)?.UniqueId;
                if (uniqueId != null && ids.Contains(uniqueId))
                {
                    _readyComponents.Add(component);
                    names.Add(typeName + "_" + uniqueId);
                }
            }
        } 
        Debug.Log("Ready Node Monitors: " + string.Join(",", names));
    }

    public bool Ready => matchAny ? _readyComponents.Any(c => c.Ready) : _readyComponents.All(c => c.Ready);

    private bool _isReadyOnEnter = false;

    protected override void OnEnter()
    {
        _isReadyOnEnter = child!=null && Ready;
    }

    protected override State OnUpdate()
    {
        if (!_isReadyOnEnter) return State.Failure;
        return child.Run();
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as ReadyNode;
        clone.componentIds = componentIds;
        clone.matchAny = matchAny;
        return clone;
    }

    public ReadyNode()
    {
        description = "Block the branch until specified IReady components are ready";
        titleCustomizable = false;
    }
}