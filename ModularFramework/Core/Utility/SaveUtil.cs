using System;
using System.Collections.Generic;
using System.Linq;
using BayatGames.SaveGameFree;
using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework.Utility
{
    using static EnvironmentConstants;
    public static class SaveUtil
    {
        private static SaveFile _saveFile;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            SaveGame.Encode = true;
            Load();
        }

        public static void SaveValue<T>(string key, T value) {
            if(_saveFile == null) Load();
            _saveFile.Add(key, value);
        }

        public static void SaveState(string key, string state) {
            if(_saveFile == null) Load();
            _saveFile.AddState(key, state);
        }

        public static void Flush() {
            if(_saveFile == null) return;
            _saveFile.saveTime = DateTime.UtcNow;
            SaveGame.Save(DEFAULT_AUTO_SAVE_SLOT.ToString(), _saveFile);
        }

        public static bool FlushToSlot(int slot) {
            if(_saveFile==null || slot < 0 || slot>MAX_MANUAL_SAVE_SLOT) return false;
            Debug.Log("game saved");
            _saveFile.saveTime = DateTime.UtcNow;
            SaveGame.Save(slot.ToString(), _saveFile);
            return true;
        }

        public static bool FlushToNextAvailableSlot()
        {
            
            int slot = MathUtil
                .Repeat(MAX_MANUAL_SAVE_SLOT+1)
                .FirstOrDefault(i => i!=DEFAULT_AUTO_SAVE_SLOT && !SaveGame.Exists(i.ToString()));
            if (slot == 0)
            {
                slot = GetOldestSaveFile();
            }
            return FlushToSlot(slot);
        }

        private static int GetOldestSaveFile()
        {
            var pair = GetOccupiedSlots().Where(pair => pair.Item1 != DEFAULT_AUTO_SAVE_SLOT)
                .OrderBy(pair => pair.Item2.saveTime)
                .FirstOrDefault();
            return pair.Item1;
        }

        /// <summary>
        /// load an existing saveFile or start a new saveFile at slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool LoadFromSlot(int slot) {
            if(slot < 0 || slot>MAX_MANUAL_SAVE_SLOT) return false;
            _saveFile = SaveGame.Load(slot.ToString(), _defaultSaveFile);
            return true;
        }


        public static void Load()
        {
            _saveFile = SaveGame.Exists(DEFAULT_AUTO_SAVE_SLOT.ToString()) ? 
                SaveGame.Load(DEFAULT_AUTO_SAVE_SLOT.ToString(), _defaultSaveFile) : _defaultSaveFile;
        }

        private static SaveFile _defaultSaveFile => new SaveFile() { saveTime = DateTime.UtcNow };

        public static Dictionary<string,AnyValue> GetAllValues() {
            if(_saveFile == null) Load();
            return _saveFile.values;
        }
        
        public static Optional<AnyValue> GetValue(string key) {
            if(_saveFile == null) Load();
            return _saveFile.Get(key);
        }
        
        public static Optional<string> GetState(string key) {
            if(_saveFile == null) Load();
            return _saveFile.GetState(key);
        }

        public static (int,SaveFile)[] GetOccupiedSlots() {
            return MathUtil.Repeat(MAX_MANUAL_SAVE_SLOT+1)
                            .Where(i=> SaveGame.Exists(i.ToString()))
                            .Select(i =>(i,SaveGame.Load<SaveFile>(i.ToString())))
                            .ToArray();

        }

        public static void ClearAll() {
            SaveGame.Clear();
        }

        public static void ClearSlot(int slot) {
            SaveGame.Delete(slot.ToString());
        }
    }

    public class SaveFile
    {
        public Dictionary<string,string> states = new(); // jsons, story state, note
        public Dictionary<string,AnyValue> values = new();
        public DateTime saveTime;
        // Transient
        public int slot;

        public Optional<AnyValue> Get(string key) {
            if(values.TryGetValue(key, out AnyValue anyValue))
            {
                return anyValue;
            }
            return Optional<AnyValue>.None();
        }
        public void Add<T>(string key, T value) {
            values[key] = AnyValue.Of(value);
        }

        public Optional<string> GetState(string key) {
            if(states.TryGetValue(key, out string state))
            {
                return state;
            }
            return Optional<string>.None();
        }

        public void AddState(string key, string state) {
            states[key] = state;
        }

        public string GetSaveTime() {
            return saveTime.ToLocalTime().ToString(DATE_FORMAT);
        }
    }
}
