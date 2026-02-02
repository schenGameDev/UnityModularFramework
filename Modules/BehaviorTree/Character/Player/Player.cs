using System.Collections.Generic;
using ModularFramework.Utility;
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
    
    private List<IEffect<IDamageable>> _activeEffects = new();

    public DamageTarget DamageTarget => DamageTarget.Player;
    public Transform Transform => transform;
    
    private void OnEnable()
    {
        SingletonRegistry<Player>.Replace(this);
    }

    private void OnDisable()
    {
        SingletonRegistry<Player>.Clear();
    }
    
    private void OnDestroy()
    {
        SingletonRegistry<Player>.Clear();
    }
    
    private void Start()
    {
        health = maxHealth;
    }
    
    private void OnHealthChanged()
    {
        canvas.SetActive(true);
        healthBar.fillAmount = health / maxHealth;
    }

    public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
    {
        highlightMarker.SetActive(isAiming);
    }
    
    
    public void TakeDamage(int amount, DamageType damageType)
    {
        if(damageType == DamageType.Physical) TakePhysicalDamage(amount);
    }

    private void TakePhysicalDamage(int amount)
    {
        health -= amount;
        OnHealthChanged();
        Debug.Log($"player has taken {amount} damage.");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Handle death logic here
        Debug.Log("player has died.");
        _activeEffects.ForEach(e => e.Cancel());
        Destroy(gameObject);
    }

    public void TakeEffect(IEffect<IDamageable> effect)
    {
        effect.OnCompleted += RemoveEffect;
        _activeEffects.Add(effect);
        effect.Apply(this);
    }
    
    void RemoveEffect(IEffect<IDamageable> effect)
    {
        effect.OnCompleted -= RemoveEffect;
    }

    public void TakeSpecialCondition(SpecialCondition specialCondition)
    {
        Debug.Log("Taking special condition: " + specialCondition);
    }

    public void RemoveSpecialCondition(SpecialCondition specialCondition)
    {
        Debug.Log("Removing special condition: " + specialCondition);
    }
}