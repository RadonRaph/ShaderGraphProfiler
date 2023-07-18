using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Albyon/Optimisation/ShaderGraphProfilerResources", fileName = "shaderGraphProfilerResources")]
public class ShaderGraphProfilerResources : ScriptableObject
{
    public NodeCostData[] nodeCostDatas;
    public NodeCostStyle[] nodeCostStyles;
}
