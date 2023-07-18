using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Albyon/Optimisation/NodeCostData", fileName = "nodeCostData")]
public class NodeCostData : ScriptableObject
{
    [System.Serializable]
    public struct NodeCost
    {
        [SerializeField]
        public string nodeTitle;
        [SerializeField]
        public UnitCost unitCost;
        [SerializeField]
        public GlobalCost globalCost;

        public static NodeCost operator+(NodeCost A, NodeCost B)
        {
            NodeCost result;
            result.unitCost.FMA = A.unitCost.FMA+B.unitCost.FMA;
            result.unitCost.CVT = A.unitCost.CVT + B.unitCost.CVT;
            result.unitCost.SFU = A.unitCost.SFU+ B.unitCost.SFU;
            result.unitCost.MEM = A.unitCost.MEM + B.unitCost.MEM;
            result.unitCost.VAR = A.unitCost.VAR + B.unitCost.VAR;
            result.unitCost.TEX = A.unitCost.TEX + B.unitCost.TEX;

            result.globalCost = (GlobalCost)Mathf.Max(((float)A.globalCost), ((float)B.globalCost));
            result.nodeTitle = "Sum_" + A.nodeTitle + "_" + B.nodeTitle;
            return result;

        }

    }

    [System.Serializable]
    public struct UnitCost
    {
        public float FMA;
        public float CVT;
        public float SFU;
        public float MEM;
        public float VAR;
        public float TEX;
    }

    public enum GlobalCost
    {
        Free,
        Simple,
        Moderate,
        Complex,
        ProjectKiller
    }

    [TextArea]
    public string description;
    
    [SerializeField]
    public NodeCost[] nodeCosts;

    [SerializeField]
    public StyleSheet styleSheet;
}
