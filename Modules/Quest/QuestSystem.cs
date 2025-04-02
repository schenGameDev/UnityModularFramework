using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// load and save quests
/// </summary>
[CreateAssetMenu(fileName = "QuestSystem_SO", menuName = "Game Module/Quest/Quest System")]
public class QuestSystem : GameSystem
{
    [Header("Config")]
    [SerializeField] private SOBucket<Quest> quests;
    [SerializeField] private EventChannel<(string,Quest.QuestStage)> questChannel;
    [SerializeField] private EventChannel<(string,bool?)> questMilestoneChannel;
    [RuntimeObject] private readonly Dictionary<string,QuestMilestone> _milestones = new();

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

    public override void OnStart()
    {
        base.OnStart();
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

    public override void OnDestroy()
    {
        base.OnDestroy();
        quests.ForEach(q => q.Reset());
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
        SaveUtil.GetState(QuestConstants.KEY_QUEST)
            .Do(json => JsonUtility.FromJson<Dictionary<string, int>>(json)
                .ForEach((questName, stage) => quests.Get(questName)
                    .Do(q => q.stage = (Quest.QuestStage)stage)));
        SaveUtil.GetState(QuestConstants.KEY_QUEST_MILESTONE)
            .Do(json => JsonUtility.FromJson<Dictionary<string, bool>>(json)
                .ForEach((milestoneId, reached) => _milestones.Get(milestoneId)
                    .Do(q => q.Reached = reached)));
    }
    
    private void UpdateQuestStage((string, Quest.QuestStage) request) => UpdateQuestStage(request.Item1, request.Item2);
    public void UpdateQuestStage(string questName, Quest.QuestStage stage)
    {
        quests.Get(questName).Do(q => q.stage = stage);
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
}