using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "BehaviorTree_SO", menuName = "Game Module/Behavior Tree/Behavior Tree")]
public class BehaviorTreeSO : ScriptableObject
{
    [ReadOnly] public BTNode root;
    [ReadOnly] public BTBlackboard blackboard;
    [HideInInspector] public BTRunner runner;
    
    public Transform Me { get; set; }
    public AstarAI AI { get; private set; }
    public List<BTNode> nodes = new();
    
    #if UNITY_EDITOR

    #region Editor
    public BTNode CreateNode(Type type) {
        BTNode node = CreateInstance(type) as BTNode;
        node.Initialize(GUID.Generate().ToString());
        Undo.RecordObject(this,"Behavior Tree (Create Node)");

        nodes.Add(node);

        if(!Application.isPlaying) {
            AssetDatabase.AddObjectToAsset(node, this);
        }

        Undo.RegisterCreatedObjectUndo(node,"Behavior Tree (Create Node)");
        AssetDatabase.SaveAssets();
        return node;
    }
    
    public BTNode DuplicateNode(BTNode originalNode) {
        if(originalNode is RootNode) return null;
        BTNode node = CreateInstance(originalNode.GetType()) as BTNode;
        EditorUtility.CopySerialized(originalNode, node);
        node.guid = GUID.Generate().ToString();
        node.parentPortName = null;
        node.ClearChildren();
        Undo.RecordObject(this,"Behavior Tree (Duplicate Node)");

        nodes.Add(node);

        if(!Application.isPlaying) {
            AssetDatabase.AddObjectToAsset(node, this);
        }
        EditorUtility.SetDirty(node);

        Undo.RegisterCreatedObjectUndo(node,"Behavior Tree (Duplicate Node)");
        AssetDatabase.SaveAssets();
        return node;
    }

    public void CreateBlackboard()
    {
        blackboard = CreateInstance(typeof(BTBlackboard)) as BTBlackboard;
        blackboard.name = "Blackboard";
        
        Undo.RecordObject(this,"Behavior Tree (Create Blackboard)");

        if(!Application.isPlaying) {
            AssetDatabase.AddObjectToAsset(blackboard, this);
        }

        Undo.RegisterCreatedObjectUndo(blackboard,"Behavior Tree (Create Blackboard)");
        AssetDatabase.SaveAssets();
    }

    public void DeleteNode(BTNode node) {
        Undo.RecordObject(this,"Behavior Tree (Delete Node)");
        nodes.Remove(node);
        foreach (var n in nodes)
        {
            n.RemoveChild(node);
        }
        //AssetDatabase.RemoveObjectFromAsset(node);
        Undo.DestroyObjectImmediate(node);
        AssetDatabase.SaveAssets();
    }

    public void DeleteSingletonNode(SingletonNode node, string guid)
    {
        if (node.nodePositions.Count <= 1)
        {
            DeleteNode(node);
            return;
        }
        
        Undo.RecordObject(this,"Behavior Tree (Delete Node)");
        node.nodePositions.RemoveAll(np =>
            {
                if (np.guid == guid)
                {
                    if (!string.IsNullOrEmpty(np.parentGuid)) {
                        nodes.Where(n => n.guid == np.parentGuid)
                            .ForEach(n => n.RemoveChild(node));
                    }
                    return true;
                }
                return false;
            });

        AssetDatabase.SaveAssets();
    }

    public void AddChild(BTNode parent, BTNode child, String parentPortName) {
        Undo.RecordObject(parent,"Behavior Tree Node (Add Child)");
        parent.AddChild(child);
        child.parentPortName = parentPortName;
        EditorUtility.SetDirty(parent);
        AssetDatabase.SaveAssets();
    }

    public void RemoveChild(BTNode parent, BTNode child) {
        Undo.RecordObject(parent,"Behavior Tree Node (Remove Child)");
        parent.RemoveChild(child);
        child.parentPortName = null;

        if (child is SingletonNode s)
        {
            s.Remove(parent.guid);
        }
        
        EditorUtility.SetDirty(parent);
        AssetDatabase.SaveAssets();
    }
    
    [Button]
    public void Fix()
    {
        this.nodes.RemoveAll(n => n == null);
        RemoveExtraRootFromNodes();
        string assetPath = AssetDatabase.GetAssetPath(this);
        // remove unused nodes
        BTNode[] nodes = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<BTNode>().ToArray();
        foreach (var n in nodes)
        {
            if(!this.nodes.Contains(n)) DestroyImmediate(n,true);
            else
            {
                n.name = n.guid;
            }
        }
        // keep only one blackboard
        BTBlackboard[] blackboards = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<BTBlackboard>().ToArray();
        if (blackboards.Length > 0 && blackboard == null)
        {
            blackboard = blackboards[0];
        }
        foreach (var b in blackboards)
        {
            if(blackboard != b) DestroyImmediate(b, true);
        }
        blackboard.Clear();
        // remove duplicates
        foreach (var controlNode in this.nodes.OfType<ControlNode>())
        {
            controlNode.Children = controlNode.Children.Where(x=> x!=null).Distinct().ToList();
        }
        
        AssetDatabase.SaveAssets();
    }

    private void RemoveExtraRootFromNodes()
    {
        RootNode[] roots = nodes.OfType<RootNode>().ToArray();
        if (roots.Length > 0 && root == null)
        {
            root = roots[0];
        }
        
        foreach (var r in roots)
        {
            if(root != r) DeleteNode(r);
        }
    }
    #endregion
#endif

    public void Initialize() {
        AI = Me.GetComponent<AstarAI>();
        runner = Me.GetComponent<BTRunner>();
        nodes.ForEach(n =>
        {
            n.tree = this;
        });
    }

    public BehaviorTreeSO Clone() {
        var clone = Instantiate(this);
        clone.root = root.Clone();
        clone.nodes = new List<BTNode>();
        Traverse(clone.root, (n) => {
            clone.nodes.Add(n);
        } );
        if(blackboard) clone.blackboard = Instantiate(blackboard);
        return clone;
    }

    void Traverse(BTNode node, Action<BTNode> visitor) {
        if(node) {
            visitor.Invoke(node);
            node.GetChildren().ForEach(n => Traverse(n, visitor));
        }
    }

    public void Run() {
        root.Run();
        blackboard.changed = false;
    }
}
