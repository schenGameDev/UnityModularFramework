using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BehaviorTreeView : GraphView
{
    public Action<NodeView> OnNodeSelected;
    private BehaviorTreeSO _tree;
    private IEventHandler _rightClickTarget;
    private Vector2 _rightClickPosition;

    // public new class UxmlFactory : UxmlFactory<BehaviorTreeView,UxmlTraits> {}
    public BehaviorTreeView() {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        PopulateView(_tree);
        AssetDatabase.SaveAssets();
    }

    private NodeView FindNodeView(BTNode node) 
    {
        return GetNodeByGuid(node.guid) as NodeView;
    }

    private NodeView FindChildNodeView(BTNode node, string parenGuid = null)
    {
        if(node is not SingletonNode s) return FindNodeView(node);
        return s.nodePositions.Where(np => np.parentGuid == parenGuid)
            .Select(np => GetNodeByGuid(np.guid) as NodeView)
            .FirstOrDefault();
    }

    internal void PopulateView(BehaviorTreeSO tree)
    {
        _tree = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        if(_tree.root == null) {
            _tree.root = _tree.CreateNode(typeof(RootNode)) as RootNode;
            EditorUtility.SetDirty(_tree);
            AssetDatabase.SaveAssets();
        }

        if (_tree.blackboard == null)
        {
            _tree.CreateBlackboard();
            EditorUtility.SetDirty(_tree);
            AssetDatabase.SaveAssets();
        }

        // create node view
        _tree.nodes.ForEach(CreateNodeView);
        // create edges
        _tree.nodes.ForEach(n=> {
            var children = n.GetChildren();
            NodeView parentView = FindNodeView(n);
            if(parentView==null || parentView.OutputPorts==null || parentView.OutputPorts.Length==0) return;
            foreach (var c in children)
            {
                NodeView childView = FindChildNodeView(c, n.guid);
                if(childView==null || childView.InputPort==null) continue;
                var portIndex = parentView.Node.GetOutputPortIndex(childView.Node.parentPortName);
                Edge edge = parentView.OutputPorts[portIndex].ConnectTo(childView.InputPort);
                // Debug.Log($"Edge created between {parentView.Node.title} and {childView.Node.title}");
                AddElement(edge);
            }
        });
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if(graphViewChange.elementsToRemove != null) {
            graphViewChange.elementsToRemove.ForEach(ele => {
                if(ele is NodeView nodeView) {
                    if(nodeView.Node is SingletonNode s) _tree.DeleteSingletonNode(s, nodeView.viewDataKey);
                    else _tree.DeleteNode(nodeView.Node);
                } else if(ele is Edge edge) {
                    if (edge.output.node is NodeView parentView && 
                        edge.input.node is NodeView childView)
                        _tree.RemoveChild(parentView.Node, childView.Node);
                }

            });
        }

        if(graphViewChange.edgesToCreate !=null) {
            graphViewChange.edgesToCreate = graphViewChange.edgesToCreate.Where(edge => {
                NodeView parentView = edge.output.node as NodeView;
                NodeView childView = edge.input.node as NodeView;
                if (childView == null || parentView == null)
                {
                    return false;
                }
                if (parentView.Node.IsNodeChildrenFull())
                {
                    Debug.LogWarning($"{parentView.Node} output port is full");
                    return false;
                }
                if (!parentView.Node.IsChildTypeAllowed(childView.Node) || !childView.Node.IsParentTypeAllowed(parentView.Node))
                {
                    Debug.LogWarning($"{childView.Node.GetType()} is not allowed to be child of type {parentView.Node.GetType()}");
                    return false;
                }
                return true;
            }).ToList();
            graphViewChange.edgesToCreate.ForEach(edge => {
                if (edge.output.node is NodeView parentView &&
                    edge.input.node is NodeView childView)
                {
                    _tree.AddChild(parentView.Node, childView.Node, edge.output.portName);
                }
            });
        }

        if(graphViewChange.movedElements != null) {
            HashSet<BTNode> movedNodes = graphViewChange.movedElements.OfType<NodeView>().Select(n => n.Node).ToHashSet();
            if (movedNodes.Count > 0)
            {
                foreach (var node in _tree.nodes)
                {
                    if (node is not ControlNode controlNode || node is SwitchNode) continue;
                    if (controlNode.GetChildren().Count <= 1) continue;
                    if (controlNode.GetChildren().Count(child => movedNodes.Remove(child)) <= 0) continue;
                    controlNode.SortChildren();
                    if(movedNodes.Count==0) break;

                }
            }
        }


        return graphViewChange;
    }

    private void CreateNodeView(BTNode node)
    {
        if (node is SingletonNode s)
        {
            s.nodePositions.ForEach(np =>CreateSingletonNodeView(s, np));
            return;
        }
        
        NodeView nodeView = new NodeView(node)
        {
            OnNodeSelected = OnNodeSelected
        };
        Label visualText = nodeView.Q<Label>("description");
        if(visualText!=null) visualText.text = node.GetType().Name;

        AddElement(nodeView);

        
    }
    
    private void CreateSingletonNodeView(SingletonNode node, SingletonNode.NodePosition position)
    {
        NodeView nodeView = GetNodeByGuid(position.guid) as NodeView;
        if (nodeView != null)
        {
            nodeView.UpdateTitle();
            return;
        }
        
        nodeView = new NodeView(node, position)
        {
            OnNodeSelected = OnNodeSelected
        };
        Label visualText = nodeView.Q<Label>("description");
        if(visualText!=null) visualText.text = node.GetType().Name;

        AddElement(nodeView);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if (Application.isPlaying)
        {
            return;
        }
        
        _rightClickTarget = evt.target;
        _rightClickPosition = contentViewContainer.WorldToLocal(evt.mousePosition);

        //base.BuildContextualMenu(evt);
        var groupSelectedNodes = selection.OfType<NodeView>().ToList();
        var groupSelectedEdges = selection.OfType<Edge>().ToList();
        bool multiNodeViewSelected = groupSelectedNodes.Count > 1;
        bool containRoot = (!multiNodeViewSelected &&
                            (evt.target is NodeView { Node: RootNode } ||
                             (evt.target is Group group && group.Children()
                                 .Where(c => c is NodeView)
                                 .Any(c => (c as NodeView)?.Node is RootNode)))) ||
                           groupSelectedNodes.Any(c => c.Node is RootNode);
        bool canDelete = (groupSelectedNodes.Count > 0 && !containRoot) || 
                         (groupSelectedNodes.Count ==0 && groupSelectedEdges.Count > 0);
        evt.menu.AppendSeparator();
        
        
        if (canDelete)
        {
            evt.menu.AppendAction("Delete", delegate
            {
                DeleteSelectionCallback(AskUser.DontAskUser);
            }, (a) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        if (!multiNodeViewSelected && evt.target is NodeView { Node: not RootNode and not SingletonNode })
        {
            evt.menu.AppendAction("Reset", (a)  => ResetNode());
            evt.menu.AppendAction("Duplicate", (a)  => DuplicateNode());
        }
        
        evt.menu.AppendSeparator();
        
        var types = TypeCache.GetTypesDerivedFrom<SingletonNode>();
        foreach(var type in types) 
        {
            if(type.IsAbstract) continue;
            evt.menu.AppendAction($"Special/{type.Name}", (a) => CreateNode(type));
        }
        
        evt.menu.AppendAction($"Special/SubTree", (a) =>
        {
            CreateNode(typeof(SubTreeOutletNode));
            CreateNode(typeof(SubTreeRootNode), new Vector2(10,10));
        });
        
        types = TypeCache.GetTypesDerivedFrom<ActionNode>();
        foreach(var type in types) {
            if(type.IsAbstract) continue;
            if (type.IsSubclassOf(typeof(AstarAINode)))
            {
                evt.menu.AppendAction($"Move/{type.Name}", (a) => CreateNode(type));
                continue;
            }
            evt.menu.AppendAction($"Action/{type.Name}", (a) => CreateNode(type));
        }
        types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
        foreach(var type in types) {
            if(type.IsAbstract) continue;
            evt.menu.AppendAction($"Decorator/{type.Name}", (a) => CreateNode(type));
        }
        types = TypeCache.GetTypesDerivedFrom<ControlNode>();
        foreach(var type in types) {
            if(type.IsAbstract) continue;
            string menuName = type.Name;
            if (!type.IsSubclassOf(typeof(SwitchNode)))
            {
                menuName = "Control/" + menuName;
            }
            else if (menuName.StartsWith("Find"))
            {
                menuName = "Find Target/" + menuName;
            }
            else
            {
                menuName = "Switch/" + menuName;
            }
            evt.menu.AppendAction(menuName, (a) => CreateNode(type));
        }
    }

    private void CreateNode(Type type, Vector2 offset = default) {
        if (type.IsSubclassOf(typeof(SingletonNode)))
        {
            CreateSingletonNode(type);
            return;
        }
        
        BTNode node = _tree.CreateNode(type);
        node.position = _rightClickPosition;
        if(offset != default) node.position += offset;
        CreateNodeView(node);
    }

    public void CreateSingletonNode(Type type)
    {
        var existingNode = _tree.nodes.FirstOrDefault(n => type == n.GetType());
        if (existingNode != null && existingNode is SingletonNode s)
        {
            s.Add(_rightClickPosition);
            CreateSingletonNodeView(s, s.nodePositions[^1]);
            return;
        }
        
        var node = _tree.CreateNode(type) as SingletonNode;
        node.position = _rightClickPosition;
        node.Add(_rightClickPosition);
    }
    
    private void ResetNode()
    {
        if (_rightClickTarget is not NodeView nodeView || nodeView.Node == null) return;
        
        Undo.RecordObject(nodeView.Node, "Behavior Tree (Reset Node)");
        
        var node = nodeView.Node;
        var type = node.GetType();
        
        var newInstance = ScriptableObject.CreateInstance(type) as BTNode;
        newInstance.Initialize(node.guid);
        node.GetChildren().ForEach(c => c.parentPortName = null);
        node.ClearChildren();
        EditorUtility.CopySerialized(newInstance, node);
        ScriptableObject.DestroyImmediate(newInstance);
        
        EditorUtility.SetDirty(node);
        AssetDatabase.SaveAssets();

        foreach (var outputPort in nodeView.OutputPorts)
        {
            var connections = outputPort.connections.ToList(); // ToList() to avoid modification during iteration
            foreach (var edge in connections)
            {
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                RemoveElement(edge);
            }
        }
        nodeView.OnSelected();
    }
    
    private void DuplicateNode()
    {
        if (_rightClickTarget is not NodeView nodeView || 
            nodeView.Node == null || 
            nodeView.Node is RootNode) return;

        if (nodeView.Node is SingletonNode s)
        {
            s.Add(s.position + new Vector2(10,10));
            CreateSingletonNodeView(s, s.nodePositions[^1]);
            return;
        }
        
        BTNode node = _tree.DuplicateNode(nodeView.Node);
        node.position += new Vector2(10,10);
        CreateNodeView(node);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        NodeView parentView = startPort.node as NodeView;
        if(parentView.Node.IsNodeChildrenFull()) {
            return new();
        }

        return ports.ToList()
            .Where(endPort => endPort.direction != startPort.direction &&
                   endPort.node != startPort.node)
            .Where(endPort =>
            {
                NodeView childView = endPort.node as NodeView;
                return parentView.Node.IsChildTypeAllowed(childView.Node) &&
                       childView.Node.IsParentTypeAllowed(parentView.Node);
            })
            .ToList();
    }

    public void UpdateNodeState() {
        if(!Application.isPlaying) return;
        nodes.ForEach(n => {
            NodeView view = n as NodeView;
            if(view!=null) {
                view.UpdateState();
            }
        });
    }
    
    public void HighlightSubTreeNode(string selectedNodeTitle)
    {
        if(Application.isPlaying) return;
        nodes.ForEach(n => {
            NodeView view = n as NodeView;
            if(view!=null) {
                view.HighlightSubTree(selectedNodeTitle);
            }
        });
    }

    public void FixTree() => _tree.Fix();
}