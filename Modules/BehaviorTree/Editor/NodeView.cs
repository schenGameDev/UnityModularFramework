using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeView : Node {
    public Action<NodeView> OnNodeSelected;
    public Port InputPort;
    public Port[] OutputPorts;
    public BTNode Node;
    public NodeView(BTNode node) : base(AssetDatabase.GetAssetPath(Resources.Load<VisualTreeAsset>("NodeView"))) {
        //styleSheets.Add(Resources.Load<StyleSheet>("NodeViewStyleSheet"));
        Node = node;
        title = node.ToString();

        viewDataKey = Node.guid;
        style.left = Node.position.x;
        style.top = Node.position.y;

        CreateInputPorts();
        CreateOutputPorts();
    }
    
    public NodeView(BTNode node, SingletonNode.NodePosition nodePosition) : base(AssetDatabase.GetAssetPath(Resources.Load<VisualTreeAsset>("NodeView"))) {
        //styleSheets.Add(Resources.Load<StyleSheet>("NodeViewStyleSheet"));
        Node = node;
        title = node.ToString();

        viewDataKey = nodePosition.guid;
        style.left = nodePosition.position.x;
        style.top = nodePosition.position.y;

        CreateInputPorts();
        CreateOutputPorts();
    }

    public void HighlightSubTree(string title)
    {
        if (Node is SubTreeRootNode or SubTreeOutletNode && Node.title == title)
        {
            AddToClassList("current");
        }
        else
        {
            RemoveFromClassList("current");
        }
        
    }
    
    public void UpdateState()
    {
        RemoveFromClassList("running");
        RemoveFromClassList("success");
        RemoveFromClassList("failure");

        switch(Node.nodeState) {
            case BTNode.State.Running:
                if(Node.started) AddToClassList("running");
                break;
            case BTNode.State.Success:
                AddToClassList("success");
                break;
            case BTNode.State.Failure:
                AddToClassList("failure");
                break;
        }
    }

    public void UpdateTitle()
    {
        if(Application.isPlaying) return;
        title = Node.ToString();
    }

    private void CreateInputPorts()
    {
        if (!Node.HideInputPort()) {
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
        }

        if(InputPort!=null) {
            InputPort.portName = "";
            InputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(InputPort);
        }
        
        inputContainer.style.backgroundColor = Node.HeaderColor;
        inputContainer.style.minHeight = 24;
    }

    private void CreateOutputPorts()
    {
        List<Port> outputPorts = new List<Port>();
        foreach (var def in Node.OutputPortDefinitions)
        {
            var port = InstantiatePort(Orientation.Vertical, Direction.Output, def.portCapacity, typeof(bool));
            port.portName = def.portName;
            port.style.flexDirection = FlexDirection.ColumnReverse;
            outputPorts.Add(port);
            outputContainer.Add(port);
        }
        
        OutputPorts = outputPorts.ToArray();
        if (outputPorts.Count > 1)
        {
            outputContainer.style.flexDirection = FlexDirection.Row;
            outputContainer.style.justifyContent = Justify.SpaceBetween;
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Undo.RecordObject(Node, "Behavior Tree Node (Set Position)");
        Node.position.x = newPos.xMin;
        Node.position.y = newPos.yMin;
        EditorUtility.SetDirty(Node);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        if(OnNodeSelected!=null) {
            OnNodeSelected.Invoke(this);
        }
    }
}