using System;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Modules.Ability;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player take damage and effects implementation
/// </summary>
[AddComponentMenu("Player/Player", 0), DisallowMultipleComponent]
public class Player : Character,IDamageable
{
    [Header("Config")]
    public int maxHealth;
    
    [Header("UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject highlightMarker;
    
    [Header("Runtime")]
    public float health;
    private EffectResolver _effectResolver;

    public DamageTarget TargetType { get; }
    public Transform Transform => transform;
    [Self,SerializeField] private PlayerMoveView playerMoveView;

#if UNITY_EDITOR
    private void OnValidate()=> this.ValidateRefs();
    
#endif

    private void Awake()
    {
        _effectResolver = new EffectResolver(this, 1);
        _effectResolver.onSpecialConditionChanged += ShowSpecialCondition;
    }

    private void OnEnable()
    {
        SingletonRegistry<Player>.Replace(this);
        DictSetRegistry<DamageTarget, Transform>.TryAdd(TargetType, Transform);
    }

    private void OnDisable()
    {
        SingletonRegistry<Player>.Unregister(this);
        DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
    }
    
    private void OnDestroy()
    {
        SingletonRegistry<Player>.Clear();
        DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
    }
    
    private void Start()
    {
        health = maxHealth;
    }
    
    private void OnHealthChanged()
    {
        healthBar.fillAmount = health / maxHealth;
    }

    public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
    {
        highlightMarker.SetActive(isAiming);
    }
    
    
    public void TakeDamage(float amount, DamageType damageType, Transform source)
    {
        if(damageType == DamageType.Physical) TakePhysicalDamage(amount);
    }

    #region Knock Back
    Action _knockBackCompleteCallback;
    public void KnockBack(Vector3 direction, float duration, float distance, Action onComplete)
    {
        direction.Normalize();
        Vector3 knockBackVelocity = direction * (distance / duration);
        // keep moving until duration is up or hit obstacle
        _knockBackCompleteCallback = onComplete;
        playerMoveView.ApplyExternalVelocity(knockBackVelocity, duration);
    }
    
    public void KnockBackComplete()
    {
        _knockBackCompleteCallback?.Invoke();
        _knockBackCompleteCallback = null;
    }
    

    #endregion

    public EffectResolver EffectResolver => _effectResolver;

    private void TakePhysicalDamage(float amount)
    {
        health -= amount;
        OnHealthChanged();
        Debug.Log($"player has taken {amount} damage.");
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void ShowSpecialCondition(SpecialCondition specialCondition, bool isAdded)
    {
            
    }

    private void Die()
    {
        // Handle death logic here
        Debug.Log("player has died.");
        _effectResolver.ResetState();
        _effectResolver.onSpecialConditionChanged -= ShowSpecialCondition;
        Destroy(gameObject);
    }
}