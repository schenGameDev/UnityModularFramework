using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest_SO", menuName = "Game Module/Quest/Quest")]
public class Quest : ScriptableObject
{
    public enum QuestStage { UNKNOWN, MENTIONED, ACTIVE, COMPLETED, FAILED, ARCHIVED }
    
    public string title;
    public string description;
    //public string QuestType;
    //public Sprite QuestIcon;
    public List<QuestMilestone> milestones;
    public QuestStage stage;
    public List<string[]> CompleteConditions = new();
    
    public QuestStage UpdateActiveQuest()
    {
        if(stage != QuestStage.ACTIVE) return stage;
        
        List<string> completed = new();
        List<string> failed = new();
        
        milestones.ForEach(m =>
        {
            if(!m.Reached.HasValue)  return;
            if (m.Reached.Value) completed.Add(m.id);
            else failed.Add(m.id);
        });
        stage = QuestStage.FAILED;
        foreach (var c in CompleteConditions)
        {
            if (completed.ContainsAll(c))
            {
                stage = QuestStage.COMPLETED;
                break;
            }

            if (!failed.ContainsAny(c))
            {
                stage = QuestStage.ACTIVE;
                break;
            }
        }
        return stage;
    }

    public void Reset()
    {
        stage = QuestStage.UNKNOWN;
        milestones.ForEach(m => m.Reset());
    }
    
    
    public bool IsCompleted => stage == QuestStage.COMPLETED;
    public bool IsFailed => stage == QuestStage.FAILED;
}