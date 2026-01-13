using EditorAttributes;
using ModularFramework;
using Sisus.ComponentNames;
using UnityEngine;

[AddComponentMenu("Behavior Tree/Range", 0), RequireComponent(typeof(BTRunner))]
public class BTRange : MonoBehaviour, IMultiComponent<BTRange>
{
    [SerializeField,OnValueChanged(nameof(RenameComponent))] private string rangeName;

    [SerializeReference] public ITransformTargetSelector targetSelector;
    [SerializeReference] public ITransformTargetFilter[] targetFilters;

    [SerializeField, HorizontalGroup(nameof(showGizmos), nameof(gizmosColor))]
    private Void gizmosGroup;
    [HideProperty] private bool showGizmos = true;
    [HideProperty, ShowField(nameof(showGizmos)),HideLabel] private Color gizmosColor = Color.red;

    private void OnDrawGizmos()
    {
        if (showGizmos && Application.isEditor && targetFilters != null)
        {
            
            foreach (var filter in targetFilters)
            {
                if(filter is RangeFilter rangeFilter)
                {
                    DrawRangeFilter(rangeFilter);
                }
            }
            
        }
    }

    private void DrawRangeFilter(RangeFilter rangeFilter)
    {
        Gizmos.color = gizmosColor;
        var pointsCollection = rangeFilter.GetRangeSector(transform);
        foreach (var points in pointsCollection)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % points.Count]);
            }
        }
        
    }

    public string UniqueId => rangeName;
    private void RenameComponent() => this.SetName($"Range: {rangeName}");
}