using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BehaviorTreeEditor : EditorWindow
{
    BehaviorTreeView _treeView;
    InspectorView _inspectorView;
    BlackboardView _blackboardView;
    NodeView _currentInspectorNodeView;
    

    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

    [SerializeField] private StyleSheet m_StyleSheet=default;

    [MenuItem("BehaviorTreeEditor/Editor")]
    public static void OpenWindow()
    {
        BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
        wnd.titleContent = new GUIContent("BehaviorTreeEditor");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line) {
        if(Selection.activeObject is BehaviorTreeSO) {
            OpenWindow();
            return true;
        }
        return false;
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        if(m_VisualTreeAsset==null) m_VisualTreeAsset = Resources.Load<VisualTreeAsset>("BehaviorTreeEditor");
        if(m_StyleSheet==null) m_StyleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditorStyleSheet");

        m_VisualTreeAsset.CloneTree(root);
        root.styleSheets.Add(m_StyleSheet);
        
        var assetsMenu = root.Q<ToolbarMenu>("Assets");
        assetsMenu.menu.AppendAction("Open Behavior Tree", (a) => OpenAsset());
        assetsMenu.menu.AppendAction("Create New Behavior Tree", (a) => CreateNewAsset());
        assetsMenu.menu.AppendSeparator();
        PopulateRecentTreesMenu(assetsMenu);

        if (Selection.activeObject is BehaviorTreeSO)
        {
            AddToRecentTrees(AssetDatabase.GetAssetPath(Selection.activeObject));
        }
        
        var saveButton = root.Q<ToolbarButton>("Save");
        var fixButton = root.Q<ToolbarButton>("Fix");
        if (Application.isPlaying)
        {
            saveButton.SetEnabled(false);
            fixButton.SetEnabled(false);
        }
        else
        {
            saveButton.clicked += () =>
            {
                if(!Application.isPlaying) AssetDatabase.SaveAssets();
            };
            
            fixButton.clicked += () =>
            {
                if(!Application.isPlaying) _treeView.FixTree();
            };
        }
        
        _treeView = root.Q<BehaviorTreeView>();
        _inspectorView = root.Q<InspectorView>();
        _blackboardView = root.Q<BlackboardView>();
        _treeView.OnNodeSelected = OnNodeSelectionChanged;
        OnSelectionChange();
    }
    
    private void PopulateRecentTreesMenu(ToolbarMenu assetsMenu)
    {
        var recentTrees = EditorPrefs.GetString("BehaviorTreeEditor.RecentTrees", "").Split(';');
    
        if (recentTrees.Length > 0 && !string.IsNullOrEmpty(recentTrees[0]))
        {
            foreach (var path in recentTrees)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(path);
                if (tree != null)
                {
                    assetsMenu.menu.AppendAction($"Recent/{tree.name}", (a) => LoadBehaviorTree(tree));
                }
            }
        }
        else
        {
            assetsMenu.menu.AppendAction("Recent/No recent trees", null, DropdownMenuAction.Status.Disabled);
        }
    }

    private void LoadBehaviorTree(BehaviorTreeSO tree)
    {
        Selection.activeObject = tree;
        OnSelectionChange();
    }

    private void OpenAsset()
    {
        string path = EditorUtility.OpenFilePanel("Select Behavior Tree", "Assets", "asset");
        if (!string.IsNullOrEmpty(path))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(path);
            if (tree != null)
            {
                AddToRecentTrees(path);
                LoadBehaviorTree(tree);
            }
        }
    }

    private void CreateNewAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Behavior Tree",
            "NewBehaviorTree",
            "asset",
            "Choose a location to save the new Behavior Tree"
        );
    
        if (!string.IsNullOrEmpty(path))
        {
            var newTree = CreateInstance<BehaviorTreeSO>();
            newTree.name = Path.GetFileNameWithoutExtension(path);
        
            AssetDatabase.CreateAsset(newTree, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        
            AddToRecentTrees(path);
            LoadBehaviorTree(newTree);
        }
    }

    private void AddToRecentTrees(string path)
    {
        var recent = EditorPrefs.GetString("BehaviorTreeEditor.RecentTrees", "").Split(';');
        var recentList = new List<string>(recent);
        recentList.Remove(path);
        recentList.Insert(0, path);
        if (recentList.Count > 10) recentList.RemoveAt(10);
        EditorPrefs.SetString("BehaviorTreeEditor.RecentTrees", string.Join(";", recentList));
    }

    private void OnSelectionChange() {
        BehaviorTreeSO tree = Selection.activeObject as BehaviorTreeSO;
        if(!tree) {
            if(Selection.activeGameObject) {
                BTRunner runner = Selection.activeGameObject.GetComponent<BTRunner>();
                if(runner) {
                    tree = runner.tree;
                }
            }
        }

        if(tree && (Application.isPlaying || AssetDatabase.CanOpenAssetInEditor(tree.GetEntityId()))) {
            _treeView?.PopulateView(tree);
            _blackboardView?.UpdateBlackboard(tree.blackboard);
        }
    }

    private void OnNodeSelectionChanged(NodeView node)
    {
        _currentInspectorNodeView = node;
        _inspectorView.UpdateSelection(node);
        _treeView.HighlightSubTreeNode(node.Node.title);
    }
    
    private void OnEnable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }


    private void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.EnteredEditMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingEditMode:
                break;
            case PlayModeStateChange.EnteredPlayMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                break;
        }
    }

    private void OnInspectorUpdate() {
        _treeView?.UpdateNodeState();
        if (_currentInspectorNodeView != null)
        {
            _currentInspectorNodeView.UpdateTitle();
        }
    }
}
