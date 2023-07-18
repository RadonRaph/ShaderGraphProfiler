using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

using UnityEngine.UIElements;
using static NodeCostData;

/* For the shader compiler 
using UnityEditor.ShaderGraph.Internal;
                    VisualElement additionalButtons = new VisualElement();
                    ShaderGraphVfxAsset vfx;
                    OutputMetadata[] outputMetadata = new OutputMetadata[10];
                    var code = vfx.GetCode(outputMetadata);*/

public class ShaderGraphProfiler : EditorWindow
{

    private EditorWindow _hookedWindow;
    private bool _hooked = false;
    public List<Node> Nodes;

    private NodeCostData _nodeCostData;
    private NodeCostStyle _nodeCostStyle;

    [SerializeField]
    private VisualTreeAsset m_UXMLTree;
    [SerializeField]
    private VisualTreeAsset m_Details_Tree;
    [SerializeField]
    private ShaderGraphProfilerResources _resources;

    //  delegate bool HookChange(bool val);

    public EventBase HookChange;

    private DropdownField _nodeCostField;
    private DropdownField _nodeStyleField;
    private Dictionary<String, NodeCostData> _nodeCostDict;
    private Dictionary<String, NodeCostStyle> _nodeCostStyleDict;

    private Dictionary<String, NodeCostData.NodeCost> _nodeCosts;

    private bool _o_nodeColor = true;
    private bool _o_nodeInfo = true;
    private bool _o_graphInfo = true;
    private bool _optionsChanged;

    private Button _RefeshButton;
    private GroupBox _waitingGroupBox;
    private ProgressBar _progressBar;

    private const bool showGenerateButton = true;

    private Node[] _nodes;

    [MenuItem("Albyon/ShaderGraphProfiler")]
    public static void ShowExample()
    {
        ShaderGraphProfiler wnd = GetWindow<ShaderGraphProfiler>();
        wnd.titleContent = new GUIContent("ShaderGraphProfiler");
    }

    /*
    public struct Node
    {
        public VisualElement Visual;
        public string Type;
    }*/

public void InitGui(VisualElement root)
    {
        _nodeCostField = root.Query<DropdownField>("ProfileDropDown");

        NodeCostData[] nodeCostSOs = _resources.nodeCostDatas;
        if (nodeCostSOs.Length <= 0)
        {
            Debug.LogError("[ShaderGraphProfiler] ERROR: No profiles found please reimport the package.");
            return;
        }

        List<String> profiles = new List<string>();
        _nodeCostDict = new Dictionary<string, NodeCostData>();
        for (int i = 0; i < nodeCostSOs.Length; i++)
        {
            profiles.Add(nodeCostSOs[i].name);
            _nodeCostDict.Add(nodeCostSOs[i].name, nodeCostSOs[i]);
        }
        _nodeCostField.choices = profiles;
        _nodeCostField.RegisterValueChangedCallback(ProfileChange);
        _nodeCostField.value = nodeCostSOs[0].name;



        _nodeStyleField = root.Query<DropdownField>("StyleDropDown");
        NodeCostStyle[] nodeCostStyleSOs = _resources.nodeCostStyles;
        if (nodeCostStyleSOs.Length <= 0)
        {
            Debug.LogError("[ShaderGraphProfiler] ERROR: No styles found please reimport the package.");
            return;
        }

        List<String> styles = new List<string>();
        _nodeCostStyleDict = new Dictionary<string, NodeCostStyle>();
        for (int i = 0; i < nodeCostStyleSOs.Length; i++)
        {
            styles.Add(nodeCostStyleSOs[i].name);
            _nodeCostStyleDict.Add(nodeCostStyleSOs[i].name, nodeCostStyleSOs[i]);
        }
        _nodeStyleField.choices = styles;
        _nodeStyleField.RegisterValueChangedCallback(StyleChange);
        _nodeStyleField.value = nodeCostStyleSOs[0].name;

        //OPTIONS

        root.Query<Toggle>("ColorCostToggle").First().RegisterValueChangedCallback(OptionColorCost);
        root.Query<Toggle>("PerNodeCost").First().RegisterValueChangedCallback(OptionNodeCost);
        root.Query<Toggle>("CumulToggle").First().RegisterValueChangedCallback(OptionGraphCost);

        _RefeshButton = root.Query<Button>("RefreshButton");
        _RefeshButton.RegisterCallback<ClickEvent>(RefreshEvent);

        _waitingGroupBox = root.Query<GroupBox>("WaitingBox");
        _progressBar = root.Q<ProgressBar>("ProgressBar");

    }

    public void CreateGUI()
    {

     //   var shaderWindow = GetWindow<MaterialGraphEditWindow>();
        
        
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        root.Add(m_UXMLTree.Instantiate());
        InitGui(root);

       
    }

    public void ProfileChange(ChangeEvent<string> evt)
    {
        if (_nodeCostDict.TryGetValue(evt.newValue, out _nodeCostData))
        {
            Debug.Log("[ShaderGraphProfiler] Successfully loaded profile.");
            _optionsChanged  = true;
            _nodeCosts = new Dictionary<string, NodeCostData.NodeCost>();
            foreach(NodeCostData.NodeCost cost in _nodeCostData.nodeCosts)
            {
                _nodeCosts.Add(cost.nodeTitle, cost);
            }

            Refresh();
        }
        else
        {
            Debug.LogError("[ShaderGraphProfiler] ERROR: Profile not found");
        }
    }

    public void StyleChange(ChangeEvent<string> evt)
    {
        if (_nodeCostStyleDict.TryGetValue(evt.newValue, out _nodeCostStyle))
        {
            Debug.Log("[ShaderGraphProfiler] Sucessfully loaded style.");
            _optionsChanged = true;
            UpdateNodesStyle();
            _optionsChanged = false;
        }
        else
        {
            Debug.LogError("[ShaderGraphProfiler] ERROR: Style not found");
        }
    }

    public void OptionColorCost(ChangeEvent<bool> evt)
    {
        _o_nodeColor = evt.newValue;
        _optionsChanged = true;
        UpdateNodesColor();
        _optionsChanged = false;
    }

    public void OptionNodeCost(ChangeEvent<bool> evt)
    {
        _o_nodeInfo = evt.newValue;
        _optionsChanged = true;
        UpdateNodesDetails();
        _optionsChanged = false;
    }

    public void OptionGraphCost(ChangeEvent<bool> evt) 
    {
        _o_graphInfo = evt.newValue;
        _optionsChanged = true;
        Refresh();
    }

    public void RefreshEvent(ClickEvent evt)
    {
        _hooked = false;
        _hookedWindow = null;
    }

    public void Refresh()
    {
        if (_hookedWindow == null)
            _hooked = false;

        if (!_hooked)
            return;


        var childs = _hookedWindow.rootVisualElement.Children();
        if (childs.Count() == 0)
            return;

        VisualElement otherRoot = childs.First();
        VisualElement content = otherRoot.ElementAt(1);
        VisualElement matGraphView = content.ElementAt(0);


        GraphView gv = (GraphView)matGraphView;
        _nodes = gv.nodes.ToArray();
      //  gv.edges.First().

        UpdateNodesStyle();
        UpdateNodesColor();
        UpdateNodesDetails();
    }

    private void UpdateNodesColor()
    {
        if (!_hooked) return;

        foreach (Node node in _nodes)
        {
            NodeCostData.NodeCost cost;
            if (_nodeCosts.TryGetValue(node.title, out cost))
            {
                string c = "Perf_" + cost.globalCost.ToString();

                if (_o_nodeColor)
                {
                    node.AddToClassList(c);
                }
                else
                {
                    if (node.ClassListContains(c))
                    {
                        node.RemoveFromClassList(c);
                    }
                }
            }
        }
    }

    private void UpdateNodesStyle()
    {
        if (!_hooked) return;

        if (_nodes == null || _nodes.Length == 0)
            return;

        foreach (Node node in _nodes)
        {
            if (_optionsChanged)
            {
                for (int i = 0; i < _nodeCostStyleDict.Values.Count; i++)
                {
                    var s = _nodeCostStyleDict.Values.ElementAt(i).styleSheet;
                    if (node.styleSheets.Contains(s))
                    {
                        node.styleSheets.Remove(s);
                    }
                }
            }

            node.styleSheets.Add(_nodeCostStyle.styleSheet);
        }
    }

    private void UpdateNodesDetails()
    {
        if (!_hooked) return;



        foreach (Node node in _nodes)
        {
            NodeCost cost;
            if (_nodeCosts.TryGetValue(node.title, out cost))
            {
                var contents = node.Q("node-border").Q("contents");

                var details = node.Q<VisualElement>("ShaderGraphProfilerDetailsContainer");
                if (details != null)
                {
                    details.parent.Remove(details);
                }

                details = new VisualElement();
                details.name = "ShaderGraphProfilerDetailsContainer";
                if (_o_nodeInfo)
                {
                    VisualElement divider = new VisualElement();
                    divider.name = "divider";
                    divider.AddToClassList("horizontal");
                    
                    
                    var nodeInfo = m_Details_Tree.Instantiate();
                    nodeInfo.Q<Label>("DetailsTitle").text = "Node Cost";
                    details.Add(divider);
                    details.Add(nodeInfo);
                    
                    SetDetailLabel(nodeInfo, "FMA", cost.unitCost.FMA);
                    SetDetailLabel(nodeInfo, "CVT", cost.unitCost.CVT);
                    SetDetailLabel(nodeInfo, "SFU", cost.unitCost.SFU);
                    SetDetailLabel(nodeInfo, "MEM", cost.unitCost.MEM);
                    SetDetailLabel(nodeInfo, "VAR", cost.unitCost.VAR);
                    SetDetailLabel(nodeInfo, "TEX", cost.unitCost.TEX);
                }
                
                if (_o_graphInfo)
                {
                    VisualElement divider = new VisualElement();
                    divider.name = "divider";
                    divider.AddToClassList("horizontal");
                    
                    
                    cost = GetRecursivelyNodeCost(node, 0, 150);
                    
                    var nodeInfo = m_Details_Tree.Instantiate();
                    nodeInfo.Q<Label>("DetailsTitle").text = "Cumulative Cost";
                    details.Add(divider);
                    details.Add(nodeInfo);
                    
                    SetDetailLabel(nodeInfo, "FMA", cost.unitCost.FMA);
                    SetDetailLabel(nodeInfo, "CVT", cost.unitCost.CVT);
                    SetDetailLabel(nodeInfo, "SFU", cost.unitCost.SFU);
                    SetDetailLabel(nodeInfo, "MEM", cost.unitCost.MEM);
                    SetDetailLabel(nodeInfo, "VAR", cost.unitCost.VAR);
                    SetDetailLabel(nodeInfo, "TEX", cost.unitCost.TEX);
                }
                


                contents.contentContainer.Add(details);
                contents.Q("previewFiller").BringToFront();
                node.RefreshExpandedState();




            }
            
        }
        
    }

    NodeCost GetRecursivelyNodeCost( Node node, int it, int maxIteration)
    {
        NodeCost cost;
        if (!_nodeCosts.TryGetValue(node.title, out cost))
        {
            cost.nodeTitle = "UnsampledNode";
            cost.unitCost.FMA = 0;
            cost.unitCost.CVT = 0;
            cost.unitCost.SFU = 0;
            cost.unitCost.MEM = 0;
            cost.unitCost.VAR = 0;
            cost.unitCost.TEX = 0;

        }

        it++;
         if (it < maxIteration)
        {
            var ports = node.Query<Port>().ToList();
            foreach (var port in ports) { 
                if (port.connected && port.direction == Direction.Input)
                {
                   var inputNode = port.connections.First().output.node;
                   cost += GetRecursivelyNodeCost(inputNode, it, maxIteration);
                   it++;
                    
                }
            }
        }
         else
         {
                Debug.LogError("[ShaderGraphProfiler] Error iterating over more than 150 nodes.");
         }

        return cost;
    }


    void SetDetailLabel(VisualElement parent, string label, float val)
    {
        Label l = parent.Q<Label>(label + "_Label");
        if (l != null)
        {
            if (val <= 0)
            {
                l.parent.parent.Remove(l.parent);
            }
            else
            {
                l.text = val.ToString("F2");
            }
        }
    }


    public void Update()
    {
       // Debug.Log(focusedWindow.name);
    }


    public void OnInspectorUpdate()
    {
        if(_waitingGroupBox == null)
        {
            InitGui(rootVisualElement);
            _hooked = false;
            return;
        }

        if (_hookedWindow == null)
        {
            _hooked = false;
        }
        
        if (!_hooked)
        {
            if (focusedWindow.GetType().Name == "MaterialGraphEditWindow")
            {
                Debug.Log("[ShaderGraphProfiler] Successfully hooked");
                _hooked = true;
                _hookedWindow = focusedWindow;
                

               Refresh();
                return;
               
               
                
            }
            _waitingGroupBox.visible = true;
            _progressBar.value += Time.deltaTime * 75;
            _progressBar.value %= 100;
            _RefeshButton.visible = false;
        }
        else
        {
            _waitingGroupBox.visible = false;
            _RefeshButton.visible = true;
            // _graphEditorView = matGraphView;

        }


    }

    private void OnDestroy()
    {
        _o_graphInfo = false;
        _o_nodeColor = false;
        _o_nodeInfo = false;
        _optionsChanged = true;
        Refresh();
    }

}
