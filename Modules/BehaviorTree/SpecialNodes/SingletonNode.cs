using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GUID = UnityEditor.GUID;

public abstract class SingletonNode : BTNode
{
    [HideInInspector] public List<NodePosition> nodePositions = new();
    
    public void Add(Vector2 pos)
    {
        nodePositions.Add(new NodePosition {guid = GUID.Generate().ToString(), position = pos});
    }
    
    public void Remove(string parentGuid)
    {
        nodePositions.RemoveAll(np => np.parentGuid == parentGuid);
    }

    public override bool IsParentTypeAllowed(BTNode parentNode)
    {
        if (parentNode.GetChildren().Any(n => n.GetType() == GetType()))
        {
            Debug.LogError($"{parentNode.GetType().Name} cannot have more than one child of type {GetType().Name}");
            return false;
        }
        return base.IsParentTypeAllowed(parentNode);
    }
    
    public override List<BTNode> GetChildren() => new();
    public override bool AddChild(BTNode newChild) => false;
    public override bool RemoveChild(BTNode childToRemove) => false;
    public override void ClearChildren() { }
    public override void CascadeExit() => Exit();
    
    public override OutputPortDefinition[] OutputPortDefinitions => Array.Empty<OutputPortDefinition>();
    
    [Serializable]
    public class NodePosition
    {
        public string guid;
        public Vector2 position;
        public string parentGuid;
    }
}