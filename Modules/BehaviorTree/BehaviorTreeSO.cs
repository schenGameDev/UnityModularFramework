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
    
    public SingletonNode CreateSingletonNode(Type type, Vector2 position)
    {
        var existingNode = nodes.FirstOrDefault(n => type == n.GetType());
        if (existingNode != null && existingNode is SingletonNode s)
        {
            s.Add(position);
            return s;
        }
        
        var node = CreateNode(type) as SingletonNode;
        node.position = position;
        node.Add(position);
        return node;
    }
    
    public BTNode DuplicateNode(BTNode originalNode, Vector2 position) {
        if(originalNode is RootNode) return null;
        if (originalNode is SingletonNode s)
        {
            s.Add(position);
            Undo.RecordObject(this,"Behavior Tree (Duplicate Node)");
            return s;
        }
        BTNode node = CreateInstance(originalNode.GetType()) as BTNode;
        EditorUtility.CopySerialized(originalNode, node);
        node.guid = GUID.Generate().ToString();
        node.parentPortName = null;
        node.ClearChildren();
        node.position = position;
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
        // fix switch node children port names
        foreach (var switchNode in nodes.OfType<SwitchNode>())
        {
            bool yesFound = false, noFound = false;
            foreach (var child in switchNode.Children)
            {
                if (child.parentPortName == SwitchNode.PORT_YES)
                {
                    if (yesFound) Debug.LogWarning($"two children with YES port found in {switchNode.title} Node."); 
                    else yesFound = true;
                } else if(child.parentPortName == SwitchNode.PORT_NO)
                {
                    if (noFound) Debug.LogWarning($"two children with NO port found in {switchNode.title} Node."); 
                    else noFound = true;
                } else if (yesFound && noFound) {
                    Debug.LogWarning($"a third child found in {switchNode.title} Node.");
                } else
                {
                    child.parentPortName = yesFound ? SwitchNode.PORT_NO : SwitchNode.PORT_YES;
                }
            }
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

    public void Initialize(Transform me) {
        Me = me;
        AI = Me.GetComponent<AstarAI>();
        runner = Me.GetComponent<BTRunner>();
        foreach (var n in nodes)
        {
            n.Prepare();
        }
    }

    public BehaviorTreeSO Clone() {
        var clone = Instantiate(this);
        clone.root = root.Clone();
        clone.nodes = new List<BTNode>();
        Traverse(clone.root, (n) => {
            n.tree = clone;
            clone.nodes.Add(n);
        } );
        foreach (var subroot in CloneSubTree(nodes, clone.nodes))
        {
            Traverse(subroot, (n) => {
                n.tree = clone;
                clone.nodes.Add(n);
            } );
        }
        if(blackboard) clone.blackboard = Instantiate(blackboard);
        return clone;
    }

    private void Traverse(BTNode node, Action<BTNode> visitor) {
        if(node) {
            visitor.Invoke(node);
            node.GetChildren().ForEach(n => Traverse(n, visitor));
        }
    }
    
    private IEnumerable<BTNode> CloneSubTree(List<BTNode> nodes, List<BTNode> clonedNodes)
    {
        Dictionary<string,BTNode> visited = new ();
        try
        {
            var subroots = nodes.OfType<SubTreeRootNode>()
                .ToDictionary(sr => sr.title, sr => sr);
            clonedNodes.OfType<SubTreeOutletNode>().ForEach(outlet =>
            {
                if (subroots.TryGetValue(outlet.title, out var subroot))
                {
                    if (!visited.TryGetValue(subroot.guid, out BTNode node))
                    {
                        node = (SubTreeRootNode) subroot.Clone();
                        visited.Add(subroot.guid, node);
                    }
                    
                    outlet.subTreeRootNode = (SubTreeRootNode)node;
                }
            });
        }
        catch (ArgumentException e)
        {
            Debug.LogError("Duplicate SubTreeRootNode titles found. Make sure all SubTreeRootNode have unique titles.");
            Debug.LogException(e);
        }
        return visited.Values;
    }

    public void Run() {
        root.Run();
        blackboard.changed = false;
    }
}
