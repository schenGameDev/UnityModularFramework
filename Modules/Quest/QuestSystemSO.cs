using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// load and save quests
/// </summary>
[CreateAssetMenu(fileName = "QuestSystem_SO", menuName = "Game Module/Quest/Quest System")]
public class QuestSystemSO : GameSystem<QuestSystemSO>
{
    [Header("Config")]
    [SerializeField] private SOBucket<Quest> quests;
    
    [SerializeField] private EventChannel<(string,Quest.QuestStage)> questChannel;
    [SerializeField] private EventChannel<(string,bool?)> questMilestoneChannel;
    [SerializeField] private EventChannel<string> activityChannel;
    [RuntimeObject] private readonly Dictionary<string,QuestMilestone> _milestones = new();
    [RuntimeObject] public int failedQuestCount;
    [RuntimeObject] public int completedQuestCount;

    private void OnEnable()
    {
        questMilestoneChannel?.AddListener(UpdateMilestone);
        questChannel?.AddListener(UpdateQuestStage);
    }

    private void OnDisable()
    {
        questMilestoneChannel?.RemoveListener(UpdateMilestone);
        questChannel?.RemoveListener(UpdateQuestStage);
    }

    protected override void OnAwake() { }
    protected override void OnStart()
    {
        if(!quests) return;
        quests.ForEach(q =>
        {
            q.milestones.ForEach(m =>
            {
                m.Parent = q;
                _milestones.Add(m.id, m);
            }); 
        });
        quests.ForEach(q => q.Reset());
        LoadQuestProgress();
    }

    protected override void OnSceneDestroy()
    {
        quests?.ForEach(q => q.Reset());
    }

    public void SaveQuestProgress()
    {
        Dictionary<string,int> questProgress = new();
        Dictionary<string, bool> milestoneProgress = new();
        quests.ForEach(q =>
        {
            if(q.stage != Quest.QuestStage.UNKNOWN) questProgress.Add(q.name, (int)q.stage);
        });
        
        if(questProgress.NonEmpty()) SaveUtil.SaveState(QuestConstants.KEY_QUEST, JsonUtility.ToJson(questProgress));
        
        _milestones.Values.Where(m=>m.Reached.HasValue).ForEach(m => milestoneProgress.Add(m.id, m.Reached.Value));
        
        if(milestoneProgress.NonEmpty()) SaveUtil.SaveState(QuestConstants.KEY_QUEST_MILESTONE, JsonUtility.ToJson(milestoneProgress));
    }

    public void LoadQuestProgress()
    {
        failedQuestCount = 0;
        completedQuestCount = 0;
        SaveUtil.GetState(QuestConstants.KEY_QUEST)
            .Do(json => JsonUtility.FromJson<Dictionary<string, int>>(json)
                .ForEach((questName, stage) => quests.Get(questName)
                    .Do(q =>
                    {
                        q.stage = (Quest.QuestStage)stage;
                        if (q.IsCompleted)
                        {
                            completedQuestCount++;
                        } else if (q.IsFailed)
                        {
                            failedQuestCount++;
                        }
                    })));
        SaveUtil.GetState(QuestConstants.KEY_QUEST_MILESTONE)
            .Do(json => JsonUtility.FromJson<Dictionary<string, bool>>(json)
                .ForEach((milestoneId, reached) => _milestones.Get(milestoneId)
                    .Do(q => q.Reached = reached)));
    }
    
    private void UpdateQuestStage((string, Quest.QuestStage) request) => UpdateQuestStage(request.Item1, request.Item2);
    public void UpdateQuestStage(string questName, Quest.QuestStage stage)
    {
        quests.Get(questName).Do(q =>
        {
            if(q.stage == Quest.QuestStage.COMPLETED || q.stage == Quest.QuestStage.FAILED) return; // can't alter completed or failed task
            q.stage = stage;
            string activity;
            if (stage == Quest.QuestStage.FAILED)
            {
                activity = "<s>" + q.title + "</s>";
                failedQuestCount++;
            }
            else if (stage == Quest.QuestStage.COMPLETED)
            {
                activity = q.title + " 完成";
                completedQuestCount++;
            }
            else
            {
                activity = q.title;
            }
            activityChannel?.Raise(activity);
        });

        int score = 5 + completedQuestCount - failedQuestCount;
        if (score == 0)
        {
            GameBuilder.Instance.LoadScene("End");
        }
        else if (score <= 2)
        {
            activityChannel?.Raise($"<color=red>信心不足 {score}/10</color>");
        }
                
    }
    
    private void UpdateMilestone((string, bool?) request) => UpdateMilestone(request.Item1, request.Item2);
    public void UpdateMilestone(string id, bool? reached)
    {
        _milestones.Get(id).Do(m => m.Reached = reached);
    }

    public List<Quest> GetActiveQuests()
    {
        var activeQuests = new List<Quest>();
        quests.ForEach(q =>
        {
            if(q.stage == Quest.QuestStage.ACTIVE) activeQuests.Add(q);
        });
        return activeQuests;
    }
    
    public List<Quest> GetKnownQuests()
    {
        var knownQuests = new List<Quest>();
        quests.ForEach(q =>
        {
            if(q.stage == Quest.QuestStage.ACTIVE ||
               q.stage == Quest.QuestStage.COMPLETED ||
               q.stage == Quest.QuestStage.FAILED ) knownQuests.Add(q);
        });
        return knownQuests;
    }
}