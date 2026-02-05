using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Void = EditorAttributes.Void;

public abstract class BTNode : ScriptableObject
{
    public enum State {
        Running,Success,Failure
    }
    
    [ShowField(nameof(titleCustomizable)),Rename(nameof(titleName), stringInputMode: StringInputMode.Dynamic)] 
    public string title;
    protected bool titleCustomizable = true;
    protected string titleName = "Title";
    
    [SerializeField,Title(nameof(description), 12, 20,stringInputMode: StringInputMode.Dynamic)]
    private Void descriptionHolder;
    protected string description;
    
    [ReadOnly] public State nodeState;
    [ReadOnly] public string guid;
    [HideInInspector] public Vector2 position;
    [HideInInspector] public BehaviorTreeSO tree;
    
    
    #region BT Editor
    

    public virtual bool IsNodeChildrenFull() => false;
    public abstract List<BTNode> GetChildren();
    public abstract bool AddChild(BTNode newChild);
    public abstract bool RemoveChild(BTNode childToRemove);
    public abstract void ClearChildren();

    public void Initialize(string guid)
    {
        var typeName = GetType().Name;
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"{GetType()} has empty guid");
            name = typeName;
        }
        else
        {
            name = guid;
        }
        
        title = typeName.EndsWith("Node") ? typeName[..^4] : typeName;
        this.guid = guid;
    }
    
    public virtual bool IsParentTypeAllowed(BTNode parentNode)
    {
        return AllowedParentTypes.Count <= 0 || AllowedParentTypes.Any(t => parentNode.GetType() == t || parentNode.GetType().IsSubclassOf(t));
    }
    
    public bool IsChildTypeAllowed(BTNode childNode)
    {
        return AllowedChildTypes.Count <= 0 || AllowedChildTypes.Any(t => childNode.GetType() == t || childNode.GetType().IsSubclassOf(t));
    }
    
    protected virtual List<Type> AllowedParentTypes => new();
    protected virtual List<Type> AllowedChildTypes => new();
    
    public abstract Color HeaderColor { get; }
    
    public abstract OutputPortDefinition[] OutputPortDefinitions { get; }
    public virtual bool HideInputPort() => false;
    [HideInInspector] public string parentPortName;
    public int GetOutputPortIndex(string portName)
    { 
        if(string.IsNullOrEmpty(portName)) return 0;
        var index = Array.FindIndex(OutputPortDefinitions, def => def.portName == portName);
        return index >= 0 ? index : 0;
    }
    
    protected List<BTNode> GetChildByPortName(string portName)
    {
        return GetChildren().Where(child => child.parentPortName == portName).ToList();
    }

    
    public class OutputPortDefinition
    {
        public string portName;
        public Port.Capacity portCapacity;

        public OutputPortDefinition(Port.Capacity portCapacity, string portName="")
        {
            this.portName = portName;
            this.portCapacity = portCapacity;
        }
    }
    #endregion

    #region Runtime
    [ReadOnly] public bool started = false;
    public virtual State Run() {
        if(!started) {
            OnEnter();
            started = true;
        }

        nodeState = OnUpdate();

        if(nodeState != State.Running) {
            OnExit();
            started = false;
        }

        return nodeState;
    }

    public virtual void Exit() {
        OnExit();
        started = false;
    }

    public abstract void CascadeExit();
    /// <summary>
    /// Recursively collects all descendant nodes. Optional stop condition halts recursion at matching nodes.
    /// </summary>
    public List<BTNode> RecursiveGetChildren(Func<BTNode, bool> stopCondition = null)
    {
        List<BTNode> children = new List<BTNode>();
        foreach (var child in GetChildren())
        {
            if (stopCondition == null)
            {
                children.Add(child);
                children.AddRange(child.RecursiveGetChildren());
            } else if (stopCondition(child))
            {
                children.Add(child);// stop recursion
            }
            else
            {
                children.AddRange(child.RecursiveGetChildren(stopCondition));
            }
            
        }
        return children;
    }

    /// <summary>
    /// Run once when the node is cloned at runtime
    /// </summary>
    public virtual void Prepare()
    {
        started = false;
    }
    
    protected virtual void OnEnter() {}
    protected abstract State OnUpdate();
    // public virtual void OnFixedUpdate() {}
    protected virtual void OnExit() {}

    public virtual BTNode Clone() {
        return Instantiate(this);
    }


    protected T GetComponentInMe<T>()
    {
        if (!tree.Me.TryGetComponent<T>(out var component))
        {
            Debug.LogError($"{typeof(T)} component not found on " + tree.Me.name);
        }
        return component;
    }
    
    protected T GetComponentInMe<T>(string uniqueId) where T : class
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            return GetComponentInMe<T>();
        }
        foreach (var component in tree.Me.GetComponents<T>().OfType<IUniqueIdentifiable>())
        {
            if (component.UniqueId == uniqueId)
            {
                return component as T;
            }
        }
        Debug.LogError($"{typeof(T).Name} component with UniqueId '{uniqueId}' not found on " + tree.Me.name);
        return null;
    }
    #endregion
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(title)? name : title;
    }
}
