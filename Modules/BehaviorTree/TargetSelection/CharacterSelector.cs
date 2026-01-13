using System;
using EditorAttributes;
using UnityEngine;

[Serializable]
public struct CharacterSelector : ITransformTargetSelector
{
    [Rename("distance x")]public int distanceWeight;
    [Rename("health x")] public int healthWeight;
    [Rename("dps x")] public int dpsWeight;
    public SortOrder sortOrder;

    public float GetScore(Transform target, Transform me)
    {
        float score = 0;
        if (target == null || me == null) return score;
        Character character = target.GetComponent<Character>();
        if(distanceWeight >= 0)
            score += Vector3.Distance(me.position, target.position) * distanceWeight;
        if (healthWeight >= 0)
            score += character.Health * healthWeight;
        if (dpsWeight >= 0)
            score += character.Dps * dpsWeight;
        
        return sortOrder==SortOrder.DESCENDING? - score : score;
    }
}