using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModularFramework;

[CreateAssetMenu(fileName = "BehaviorTree_SO", menuName = "Game Module/Behavior Tree/Behavior Tree")]
public class BehaviorTreeSO : ScriptableObject
{
    public BTNode Root;
    public BehaviorManagerSO Manager { get; set; }
    public Transform Me { get; set; }
    public AstarAI AI { get; private set; }
    
    public List<BTNode> Nodes = new();

    public BTNode CreateNode(Type type) {
        BTNode node = CreateInstance(type) as BTNode;
        node.name = type.Name;
        node.Guid = GUID.Generate().ToString();
        Undo.RecordObject(this,"Behavior Tree (Create Node)");

        Nodes.Add(node);

        if(!Application.isPlaying) {
            AssetDatabase.AddObjectToAsset(node, this);
        }

        Undo.RegisterCreatedObjectUndo(node,"Behavior Tree (Create Node)");
        AssetDatabase.SaveAssets();
        return node;
    }

    public void DeleteNode(BTNode node) {
        Undo.RecordObject(this,"Behavior Tree (Delete Node)");
        Nodes.Remove(node);
        //AssetDatabase.RemoveObjectFromAsset(node);
        Undo.DestroyObjectImmediate(node);

        AssetDatabase.SaveAssets();
    }

    public void AddChild(BTNode parent, BTNode child) {
        DecoratorNode decorator = parent as DecoratorNode;
        if(decorator) {
            Undo.RecordObject(decorator,"Behavior Tree Node (Add Child)");
            decorator.Child = child;
            EditorUtility.SetDirty(decorator);
            return;
        }

        RootNode root = parent as RootNode;
        if(root) {
            Undo.RecordObject(root,"Behavior Tree Node (Add Child)");
            root.Child = child;
            EditorUtility.SetDirty(root);
            return;
        }

        ControlNode control = parent as ControlNode;
        if(control) {
            Undo.RecordObject(control,"Behavior Tree Node (Add Child)");
            control.Children.Add(child);
            EditorUtility.SetDirty(control);
        }
    }

    public void RemoveChild(BTNode parent, BTNode child) {
        DecoratorNode decorator = parent as DecoratorNode;
        if(decorator) {
            Undo.RecordObject(decorator,"Behavior Tree Node (Remove Child)");
            decorator.Child = null;
            EditorUtility.SetDirty(decorator);
            return;
        }

        RootNode root = parent as RootNode;
        if(root) {
            Undo.RecordObject(root,"Behavior Tree Node (Remove Child)");
            root.Child = null;
            EditorUtility.SetDirty(root);
            return;
        }

        ControlNode control = parent as ControlNode;
        if(control) {
            Undo.RecordObject(control,"Behavior Tree Node (Remove Child)");
            control.Children.Remove(child);
            EditorUtility.SetDirty(control);
        }
    }


    public List<BTNode> GetChildren(BTNode parent) {
        DecoratorNode decorator = parent as DecoratorNode;
        if(decorator && decorator.Child !=null) {
            return new() {decorator.Child};
        }

        RootNode root = parent as RootNode;
        if(root && root.Child !=null) {
            return new() {root.Child};
        }

        ControlNode control = parent as ControlNode;
        if(control) {
            return control.Children;
        }

        return new();
    }

    public void Initialize() {
        AI = Me.GetComponent<AstarAI>();
        Nodes.ForEach(n =>
        {
            n.tree = this;
            n.Register();
        });
    }

    public BehaviorTreeSO Clone() {
        var clone = Instantiate(this);
        clone.Root = Root.Clone();
        clone.Nodes = new List<BTNode>();
        Traverse(clone.Root, (n) => {
            clone.Nodes.Add(n);
        } );
        return clone;
    }

    void Traverse(BTNode node, Action<BTNode> visitor) {
        if(node) {
            visitor.Invoke(node);
            var children = GetChildren(node);
            children.ForEach(n => Traverse(n, visitor));
        }
    }

    public void Run() {
        Root.Run();
    }
}
