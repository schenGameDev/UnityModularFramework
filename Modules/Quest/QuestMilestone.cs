using System;

[Serializable]
public class QuestMilestone
{
    public string id;
    public string title;
    public string description;
    public bool skippable;

    public Quest Parent { get; set; }
    
    public bool? Reached = null; // false fail, true reached, null pending

    public void Reset()
    {
        Parent = null;
        Reached = null;
    }
}