using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "CharacterBucket_SO", menuName = "Game Module/ink/CharacterBucket")]
    public class CharacterBucket : CustomTypeBucket<CharacterDatum>
    {
    }

    [Serializable]
    public class CharacterDatum
    {
        public string id;
        public string name;
        public Sprite sprite;
        public Sprite icon;
        public LineCard lineCard;
        public string[] documents;
        public string coreDesire;
        public int documentUnlocked = -1;
        
        Dictionary<string, float[]> _stats;

        public void SetStats(string topic, float[] stats)
        {
            _stats[topic] = stats;
        }

        public void UpdateStats(string topic, int index, float stat)
        {
            _stats[topic][index] = stat;
        }

        public Dictionary<string, float[]> GetStats() => _stats;
    }
}