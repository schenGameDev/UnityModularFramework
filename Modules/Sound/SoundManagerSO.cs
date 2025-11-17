using System.Collections.Generic;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

/// <summary>
/// Manage all one-shot sound in the scene. It has higher priority than MusicSystem<br/>
/// 1. Must bootup before any module that calls it.<br/>
/// 2. Must use the same soundManager throughout the game.
/// </summary>
[CreateAssetMenu(fileName = "SoundManager_SO", menuName = "Game Module/Sound/Sound Manager")]
public class SoundManagerSO : GameModule<SoundManagerSO>
{
    [Header("Config")]
    [SerializeField] SoundPlayer soundPlayerPrefab;
    public SoundProfileBucket soundFxs;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 5;
    [SerializeField] int maxPoolSize = 100;
    [SerializeField] int maxSoundInstances = 15;
    
    
    [FoldoutGroup("Event Channels", nameof(sfxChannel))]
    [SerializeField] private Void eventChannelGroup;
    [HideInInspector,SerializeField] StringEventChannelSO sfxChannel;
    
    [Header("Runtime")]
    [RuntimeObject] public Transform SoundParent {get; private set;}
    [RuntimeObject] readonly List<SoundPlayer> _activeSoundPlayers = new();
    [RuntimeObject(nameof(ResetFrequentSoundPlayer),nameof(ResetFrequentSoundPlayer))]
    public readonly LinkedList<SoundPlayer> FrequentSoundPlayers = new();
    
    public AudioMixerGroup MixerGroup => soundFxs.mixerGroup;
    
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
    
    protected override void OnAwake() { }
    protected override void OnStart()
    {
        BuildParent();
        InitializePool();
    }
    
    protected override void OnUpdate() { }
    
    protected override void OnDestroy() { }
    protected override void OnDraw() { }
    
    private SoundBuilder CreateSoundBuilder() => new SoundBuilder();

    void BuildParent() {
        SoundParent = new GameObject("=== Sounds ===").transform;
        SoundParent.position = Vector3.zero;
    }

    #region Play SoundFx

    public SoundProfile GetSound(string profileName)
    {
        return soundFxs.Get(profileName).OrElseThrow(new KeyNotFoundException(profileName));
    }

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

    public void Dispose(SoundPlayer soundPlayer)
    {
        ReturnToPool(soundPlayer);
    }

    public SoundPlayer GetPlayer() => _soundPlayerPool.Get();
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
