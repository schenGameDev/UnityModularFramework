using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[Serializable]
public class TagFilter : ITransformTargetFilter
{
    [SerializeReference] private List<TagFieldWrapper> tags;
    
    private HashSet<string> tagNames;
    public bool IsIncluded(Transform target, Transform me)
    {
        if(tagNames == null) 
        {
            tagNames = new HashSet<string>();
            if(tags != null)
            {
                foreach(var tagWrapper in tags)
                {
                    tagNames.Add(tagWrapper.tag);
                }
            }
        }
        return tags== null || tagNames.Contains(target.tag);
    }
    
    [Serializable]
    public struct TagFieldWrapper
    {
        [TagDropdown] public string tag;
    }
}
