

using BayatGames.SaveGameFree;
using System;
using System.Collections.Generic;
using ModularFramework.Commons;
using System.Linq;

namespace ModularFramework.Utility
{
    using static EnvironmentConstants;
    public static class SaveUtil
    {
        public static int CurrentSlot = DEFAULT_AUTO_SAVE_SLOT;
        private static SaveFile _saveFile;

        public static void Initialize() {
            SaveGame.Encode = true;
            Load();
        }

        public static void Save<T>(string key, T value) {
            if(_saveFile == null) Load();
            _saveFile.Add(key, value);
        }

        public static void Flush() {
            if(_saveFile == null) return;
            _saveFile.SaveTime = DateTime.UtcNow;
            SaveGame.Save(CurrentSlot.ToString(), _saveFile);
        }

        public static bool FlushToSlot(int slot) {
            if(_saveFile==null || slot < 0 || slot>MAX_MANUAL_SAVE_SLOT) return false;
            _saveFile.SaveTime = DateTime.UtcNow;
            SaveGame.Save(slot.ToString(), _saveFile);
            return true;
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


        public static void Load() {
            _saveFile = SaveGame.Load(CurrentSlot.ToString(), _defaultSaveFile);
        }

        private static SaveFile _defaultSaveFile => new SaveFile() { SaveTime = DateTime.UtcNow };

        public static Optional<AnyValue> Get(string key) {
            if(_saveFile == null) Load();
            return _saveFile.Get(key);
        }


        public static (int,SaveFile)[] GetOccupiedSlots() {
            return MathUtil.Repeat(MAX_MANUAL_SAVE_SLOT+1)
                            .Where(i=>SaveGame.Exists(i.ToString()))
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

    public class SaveFile {
        public Dictionary<string,AnyValue> Values;
        public DateTime SaveTime;

        public Optional<AnyValue> Get(string key) {
            if(Values.TryGetValue(key, out AnyValue anyValue))
            {
                return new Optional<AnyValue>(anyValue);
            }
            return Optional<AnyValue>.None();
        }
        public void Add<T>(string key, T value) {
            Values[key] = AnyValue.Create(value);
        }

        public string GetSaveTime() {
            return SaveTime.ToLocalTime().ToString(DATE_FORMAT);
        }
    }
}
