using EditorAttributes;
using ModularFramework.Modules.Targeting;
using Sisus.ComponentNames;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Range", 0), RequireComponent(typeof(BTRunner))]
    public class BTRange : MonoBehaviour, IUniqueIdentifiable
    {
        [SerializeField, OnValueChanged(nameof(RenameComponent))]
        private string rangeName;

        [SerializeReference, SubclassSelector] public ITransformTargetSelector targetSelector;
        [SerializeReference, SubclassSelector] public ITransformTargetFilter[] targetFilters;

        [SerializeField, ToggleGroup("Gizmos", nameof(gizmosColor))]
        private bool showGizmos = true;

        [SerializeField, HideProperty, HideLabel]
        private Color gizmosColor = Color.red;

        private void OnDrawGizmos()
        {
            if (showGizmos && Application.isEditor && targetFilters != null)
            {

                foreach (var filter in targetFilters)
                {
                    if (filter is RangeFilter rangeFilter)
                    {
                        DrawRangeFilter(rangeFilter);
                    }
                }

            }
        }

        private void DrawRangeFilter(RangeFilter rangeFilter)
        {
            Gizmos.color = gizmosColor;
            GizmosExtension.DrawPolygons(rangeFilter.GetRangeSector(transform));

        }

        public string UniqueId => rangeName;
        private void RenameComponent() => this.SetName($"Range: {rangeName}");
    }
}