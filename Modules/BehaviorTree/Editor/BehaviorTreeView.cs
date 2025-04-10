using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
[UxmlElement]
public partial class BehaviorTreeView : GraphView
{
    public Action<NodeView> OnNodeSelected;
    BehaviorTreeSO _tree;

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

    private NodeView FindNodeView(BTNode node) {
        return GetNodeByGuid(node.Guid) as NodeView;
    }

    internal void PopulateView(BehaviorTreeSO tree)
    {
        _tree = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        if(_tree.Root == null) {
            _tree.Root = _tree.CreateNode(typeof(RootNode)) as RootNode;
            EditorUtility.SetDirty(_tree);
            AssetDatabase.SaveAssets();
        }

        // create node view
        _tree.Nodes.ForEach(n=> CreateNodeView(n));
        // create edges
        _tree.Nodes.ForEach(n=> {
            var children = _tree.GetChildren(n);
            NodeView parentView = FindNodeView(n);
            if(parentView==null || parentView.OutputPort==null) return;
            children.ForEach(c => {
                NodeView childView = FindNodeView(c);
                if(childView==null || childView.InputPort==null) return;
                Edge edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                AddElement(edge);
            });
        });
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if(graphViewChange.elementsToRemove != null) {
            graphViewChange.elementsToRemove.ForEach(ele => {
                NodeView nodeView = ele as NodeView;
                if(nodeView != null) {
                    _tree.DeleteNode(nodeView.Node);
                }

                Edge edge = ele as Edge;
                if(edge!=null) {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    _tree.RemoveChild(parentView.Node, childView.Node);
                }

            });
        }

        if(graphViewChange.edgesToCreate !=null) {
            graphViewChange.edgesToCreate = graphViewChange.edgesToCreate.Where(edge => {
                NodeView parentView = edge.output.node as NodeView;
                return !parentView.Node.IsNodeChildrenFull();
            }).ToList();
            graphViewChange.edgesToCreate.ForEach(edge => {
                NodeView parentView = edge.output.node as NodeView;
                NodeView childView = edge.input.node as NodeView;
                _tree.AddChild(parentView.Node, childView.Node);
            });
        }

        if(graphViewChange.movedElements != null) {
            nodes.ForEach(n=> {
                NodeView view = n as NodeView;
                if(n!=null) {
                    view.SortChildren();
                }
            });
        }


        return graphViewChange;
    }

    private void CreateNodeView(BTNode node)
    {
        NodeView nodeView = new NodeView(node)
        {
            OnNodeSelected = OnNodeSelected
        };
        Label visualText = nodeView.Q<Label>("description");
        if(visualText!=null) visualText.text = node.Description();

        AddElement(nodeView);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        //base.BuildContextualMenu(evt);
         if (evt.target is Node || evt.target is Group || evt.target is Edge)
        {
            if ((evt.target is NodeView && (evt.target as NodeView).Node is RootNode) ||
             (evt.target is Group && (evt.target as Group).Children()
                                                            .Where(c=>c is NodeView)
                                                            .Any(c => (c as NodeView).Node is RootNode)))
            {
                return;
            }


            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", delegate
            {
                DeleteSelectionCallback(AskUser.DontAskUser);
            }, (DropdownMenuAction a) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            return;
        }

        var types = TypeCache.GetTypesDerivedFrom<ActionNode>();
        foreach(var type in types) {
            if(type ==typeof(AstarAINode)) continue;
            evt.menu.AppendAction($"ActionNode/{type.Name}", (a) => CreateNode(type));
        }
        types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
        foreach(var type in types) {
            evt.menu.AppendAction($"DecoratorNode/{type.Name}", (a) => CreateNode(type));
        }
        types = TypeCache.GetTypesDerivedFrom<ControlNode>();
        foreach(var type in types) {
            if(type ==typeof(SwitchNode)) continue;
            evt.menu.AppendAction($"ControlNode/{type.Name}", (a) => CreateNode(type));
        }
    }

    private void CreateNode(System.Type type) {
        BTNode node = _tree.CreateNode(type);
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
            .ToList();
    }

    public void UpdateNodeState() {
        nodes.ForEach(n => {
            NodeView view = n as NodeView;
            if(view!=null) {
                view.UpdateState();
            }
        });
    }
}
