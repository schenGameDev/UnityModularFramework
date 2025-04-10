using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeView : Node {
    public Action<NodeView> OnNodeSelected;
    public Port InputPort;
    public Port OutputPort;
    public BTNode Node;
    public NodeView(BTNode node) : base("Assets/Scripts/BehaviorTree/Editor/Resources/NodeView.uxml") {
        //styleSheets.Add(Resources.Load<StyleSheet>("NodeViewStyleSheet"));
        Node = node;
        title = node.name;

        viewDataKey = Node.Guid;
        style.left = Node.Position.x;
        style.top = Node.Position.y;

        CreateInputPorts();
        CreateOutputPorts();
        SetupClasses();
    }

    private void SetupClasses()
    {
        if(Node is ActionNode) AddToClassList("action");
        else if(Node is ControlNode) AddToClassList("control");
        else if(Node is DecoratorNode) AddToClassList("decorator");
        else if(Node is RootNode) AddToClassList("root");
    }


    public void UpdateState()
    {
        RemoveFromClassList("running");
        RemoveFromClassList("success");
        RemoveFromClassList("failure");

        if(!Application.isPlaying) return;

        switch(Node.NodeState) {
            case BTNode.State.Running:
                if(Node.Started) AddToClassList("running");
                break;
            case BTNode.State.Success:
                AddToClassList("success");
                break;
            case BTNode.State.Failure:
                AddToClassList("failure");
                break;
        }
    }

    private void CreateInputPorts()
    {
        if (Node is ActionNode) {
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
        } else if (Node is DecoratorNode) {
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
        } else if (Node is ControlNode) {
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
        } else if(Node is RootNode) {
            ;
        }

        if(InputPort!=null) {
            InputPort.portName = "";
            InputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(InputPort);
        }
    }

    private void CreateOutputPorts()
    {
        if (Node is DecoratorNode) {
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
        } else if (Node is ControlNode) {
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
        } else if (Node is RootNode) {
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
        }

        if(OutputPort!=null) {
            OutputPort.portName = "";
            OutputPort.style.flexDirection = FlexDirection.ColumnReverse;
            outputContainer.Add(OutputPort);
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Undo.RecordObject(Node, "Behavior Tree Node (Set Position)");
        Node.Position.x = newPos.xMin;
        Node.Position.y = newPos.yMin;
        EditorUtility.SetDirty(Node);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        if(OnNodeSelected!=null) {
            OnNodeSelected.Invoke(this);
        }
    }

    public void SortChildren() {
        ControlNode control = Node as ControlNode;
        if(control) {
            control.Children.Sort(SortByHorizontalPosition);
        }
    }

    private int SortByHorizontalPosition(BTNode left, BTNode right)
    {
        return left.Position.x < right.Position.x? -1 : 1;
    }
}