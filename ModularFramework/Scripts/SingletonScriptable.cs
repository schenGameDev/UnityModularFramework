
// using System;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEditor.AddressableAssets;
// using UnityEditor.AddressableAssets.Settings;
// #endif

// public class SingletonScriptable<T> : ScriptableObject where T : SingletonScriptable<T>
// {
//     private static T _instance;

//     public static T Instance
//     {
//         get
//         {
//             if (_instance == null)
//             {
//                 var op = Addressables.LoadAssetAsync<T>(typeof(T).Name);
//                 _instance = op.WaitForCompletion();
//             }
//             return _instance;
//         }
//     }

//     public static void ResetInstance()
//     {
//         _instance = null;
//     }
// }

// #if UNITY_EDITOR
// public class SingletonScriptablePostprocessor : AssetPostprocessor
// {
//     private static void OnPostprocessAllAssets(
//         string[] importedAssets,
//         string[] deletedAssets,
//         string[] movedAssets,
//         string[] movedFromAssetPaths)
//     {
//         foreach (string assetPath in importedAssets)
//         {
//             ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
//             if (asset != null &&
//                 asset.GetType().BaseType != null &&
//                 asset.GetType().BaseType.IsGenericType &&
//                 asset.GetType().BaseType.GetGenericTypeDefinition() == typeof(SingletonScriptable<>))
//             {
//                 MakeAddressable(asset);
//             }
//         }
//     }

//     private static void MakeAddressable(ScriptableObject asset)
//     {
//         AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//         if (settings == null)
//         {
//             Debug.LogError("Addressable Asset Settings not found! Please initialize Addressables first.");
//             return;
//         }

//         string assetPath = AssetDatabase.GetAssetPath(asset);
//         string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

//         AddressableAssetEntry entry = settings.FindAssetEntry(assetGUID);
//         if (entry != null)
//         {
//             return;
//         }

//         string address = asset.GetType().Name;
//         entry = settings.CreateOrMoveEntry(assetGUID, settings.DefaultGroup);
//         entry.address = address;

//         settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
//         AssetDatabase.SaveAssets();

//         Debug.Log($"Made {asset.name} addressable with key: {address}");
//     }

//     [MenuItem("Assets/Make Addressable Singleton")]
//     private static void MakeSelectedAddressable()
//     {
//         var selected = Selection.activeObject as ScriptableObject;
//         if (selected != null &&
//             selected.GetType().BaseType != null &&
//             selected.GetType().BaseType.IsGenericType &&
//             selected.GetType().BaseType.GetGenericTypeDefinition() == typeof(SingletonScriptable<>))
//         {
//             MakeAddressable(selected);
//         }
//     }

//     [MenuItem("Assets/Make Addressable Singleton", true)]
//     private static bool ValidateMakeSelectedAddressable()
//     {
//         var selected = Selection.activeObject as ScriptableObject;
//         return selected != null &&
//                selected.GetType().BaseType != null &&
//                selected.GetType().BaseType.IsGenericType &&
//                selected.GetType().BaseType.GetGenericTypeDefinition() == typeof(SingletonScriptable<>);
//     }
// }
// #endif