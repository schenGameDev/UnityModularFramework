using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ModularFramework
{
    /// <summary>
    /// Provide an id unique to each prefab asset
    /// </summary>
    public class AssetIdentity : MonoBehaviour
    {
        [SerializeField, HideInInspector] uint _assetId;
        
        public uint assetId
        {
            get
            {
    #if UNITY_EDITOR
                if (_assetId == 0)
                    SetupIDs();
    #endif
                return _assetId;
            }
            // assetId is set internally when creating or duplicating a prefab
            internal set
            {
                // should never be empty
                if (value == 0)
                {
                    Debug.LogError($"Can not set AssetId to empty guid on AssetIdentity '{name}', old assetId '{_assetId}'");
                    return;
                }

                // always set it otherwise.
                // for new prefabs,        it will set from 0 to N.
                // for duplicated prefabs, it will set from N to M.
                // either way, it's always set to a valid GUID.
                assetId = value;
            }
        }
        
        void OnValidate()
        {
            // OnValidate is not called when using Instantiate
    #if UNITY_EDITOR
            DisallowChildNetworkIdentities();
            SetupIDs();
    #endif
        }
        
    #if UNITY_EDITOR
        void SetupIDs()
            {
                // is this a prefab?
                if (IsPrefab(gameObject))
                {
                    AssignAssetID(gameObject);
                }
                // are we currently in prefab editing mode? aka prefab stage
                // => check prefabstage BEFORE SceneObjectWithPrefabParent
                // => if we don't check GetCurrentPrefabStage and only check
                //    GetPrefabStage(gameObject), then the 'else' case where we
                //    clear the assetId would still be triggered for prefabs.
                //    in other words: if we are in prefab stage, do not bother with anything else ever!
                else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    // when modifying a prefab in prefab stage, Unity calls
                    // OnValidate for that prefab and for all scene objects based on
                    // that prefab.
                    //
                    // is this GameObject the prefab that we modify, and not just a
                    // scene object based on the prefab?
                    //   * GetCurrentPrefabStage = 'are we editing ANY prefab?'
                    //   * GetPrefabStage(go) = 'are we editing THIS prefab?'
                    if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                    {
                        // get path from PrefabStage for this prefab
                        string path = PrefabStageUtility.GetPrefabStage(gameObject).assetPath;
                        AssignAssetID(path);
                    }
                }
                // is this a scene object with prefab parent? save the assetId of the prefab parent.
                else if (TryGetSceneObjectPrefabParent(gameObject, out GameObject prefab))
                {
                    AssignAssetID(prefab);
                }
                else
                {
                    // IMPORTANT: DO NOT clear assetId at runtime!
                    // => fixes a bug where clicking any of the NetworkIdentity
                    //    properties (like ServerOnly/ForceHidden) at runtime would
                    //    call OnValidate
                    // => OnValidate gets into this else case here because prefab
                    //    connection isn't known at runtime
                    // => then we would clear the previously assigned assetId
                    // => and NetworkIdentity couldn't be spawned on other clients
                    //    anymore because assetId was cleared
                    if (!EditorApplication.isPlaying)
                    {
                        _assetId = 0;
                    }
                }
            }
    #endif
        
        private void DisallowChildNetworkIdentities()
        {
            AssetIdentity[] identities = GetComponentsInChildren<AssetIdentity>(true);
            if (identities.Length > 1)
            {
                // always log the next child component so it's easy to fix.
                // if there are multiple, then after removing it'll log the next.
                Debug.LogError($"'{name}' has another AssetIdentity component on '{identities[1].name}'. There should only be one AssetIdentity, and it must be on the root object. Please remove the other one.", this);
            }
        }
        
         private void AssignAssetID(string path)
        {
            // only set if not empty. 
            if (!string.IsNullOrWhiteSpace(path))
            {
                // if we generate the assetId then we MUST be sure to set dirty
                // in order to save the prefab object properly. otherwise it
                // would be regenerated every time we reopen the prefab.
                // -> Undo.RecordObject is the new EditorUtility.SetDirty!
                // -> we need to call it before changing.
                //
                // to verify this, duplicate a prefab and double click to open it.
                // add a log message if "_assetId != before_".
                // without RecordObject, it'll log every time because it's not saved.
                Undo.RecordObject(this, "Assigned AssetId");

                // uint before = _assetId;
                Guid guid = new Guid(AssetDatabase.AssetPathToGUID(path));
                assetId = AssetGuidToUint(guid);
                // if (_assetId != before) Debug.Log($"Assigned assetId={assetId} to {name}");
            }
        }
         
        private static uint AssetGuidToUint(Guid guid) => (uint)guid.GetHashCode(); // deterministic
        
        private static bool IsPrefab(GameObject obj)
        {
    #if UNITY_EDITOR
            return PrefabUtility.IsPartOfPrefabAsset(obj);
    #else
                return false;
    #endif
        }
        /// <summary>
        /// Should only be called in editor when we know for sure that this is a scene object with prefab parent. otherwise it would log an error.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private static bool TryGetSceneObjectPrefabParent(GameObject gameObject, out GameObject prefab)
        {
            prefab = null;

    #if UNITY_EDITOR
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                return false;
            }
            prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
    #endif

            if (prefab == null)
            {
                Debug.LogError($"Failed to find prefab parent for scene object [name:{gameObject.name}]");
                return false;
            }
            return true;
        }
        
        private void AssignAssetID(GameObject prefab) => AssignAssetID(AssetDatabase.GetAssetPath(prefab));
    }
}
