using UnityEngine;
using ModularFramework;
using EditorAttributes;
using UnityEngine.Pool;
using System.Collections.Generic;
using ModularFramework.Utility;

/// <summary>
/// Manage all one-shot sound in game. <br/>
/// 1. Must bootup before any module that calls it.<br/>
/// 2. Must use the same soundManager throughout the game.
/// </summary>
[CreateAssetMenu(fileName = "SoundManager_SO", menuName = "Game Module/Sound/Sound Manager")]
public class SoundManagerSO : GameModule
{
    [Header("Config")]
    [SerializeField] SoundPlayer soundPlayerPrefab;
    public SoundProfileBucket soundFxs;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 5;
    [SerializeField] int maxPoolSize = 100;
    [SerializeField] int maxSoundInstances = 15;
    
    
    [FoldoutGroup("Event Channels", nameof(sfxChannel))]
    [SerializeField] private EditorAttributes.Void eventChannelGroup;
    [HideInInspector,SerializeField] EventChannel<string> sfxChannel;
    
    [Header("Runtime")]
    [RuntimeObject] public Transform SoundParent {get; private set;}
    [RuntimeObject] readonly List<SoundPlayer> _activeSoundPlayers = new();
    [RuntimeObject(nameof(ResetFrequentSoundPlayer),nameof(ResetFrequentSoundPlayer))]
    public readonly LinkedList<SoundPlayer> FrequentSoundPlayers = new();


    public SoundManagerSO() {
        updateMode = UpdateMode.NONE;
    }

    private void OnEnable()
    {
        sfxChannel?.AddListener(PlaySound);
    }
    
    private void OnDisable()
    {
        sfxChannel?.RemoveListener(PlaySound);
    }
    
    public override void OnStart()
    {
        base.OnStart();
        BuildParent();
        InitializePool();
    }
    
    private SoundBuilder CreateSoundBuilder() => new SoundBuilder(this);

    void BuildParent() {
        SoundParent = new GameObject("=== Sounds ===").transform;
        SoundParent.position = Vector3.zero;
    }

    #region Play SoundFx

    public void PlaySound(string soundName) => CreateSoundBuilder().Play(soundName);

    public SoundPlayer PlayLoopSound(string soundName) => CreateSoundBuilder().Play(soundName,true);

    public bool CanPlaySound(SoundProfile data) {
        if (!data.frequentSound) return true;

        if (FrequentSoundPlayers.Count >= maxSoundInstances) {
            try {
                FrequentSoundPlayers.First.Value.Stop();
                return true;
            } catch {
                DebugUtil.DebugLog("SoundEmitter is already released");
            }
            return false;
        }
        return true;
    }

    public SoundPlayer Get() => _soundPlayerPool.Get();
    public void ReturnToPool(SoundPlayer soundPlayer) {
        if (soundPlayer.Node != null) {
            FrequentSoundPlayers.Remove(soundPlayer.Node);
            soundPlayer.Node = null;
        }
        _soundPlayerPool.Release(soundPlayer);
    }
    public void StopAll() {
        _activeSoundPlayers.ForEach(x=>x.Stop());
    }
    [RuntimeObject] ObjectPool<SoundPlayer> _soundPlayerPool;
    void InitializePool() {
        _soundPlayerPool = new ObjectPool<SoundPlayer>(
            () => {
                var soundPlayer = Instantiate(soundPlayerPrefab);
                soundPlayer.gameObject.SetActive(false);
                return soundPlayer;
            },
            sp => {
                sp.gameObject.SetActive(true);
                _activeSoundPlayers.Add(sp);
            },
            sp => {
                sp.gameObject.SetActive(false);
                _activeSoundPlayers.Remove(sp);
            },
            sp => {
                _activeSoundPlayers.Remove(sp);
                if (sp.Node != null) {
                    FrequentSoundPlayers.Remove(sp.Node);
                }
                Destroy(sp.gameObject);
            },
            collectionCheck,
            defaultCapacity,
            maxPoolSize);
    }

    public void ResetFrequentSoundPlayer() => FrequentSoundPlayers.Clear();
#endregion
}
