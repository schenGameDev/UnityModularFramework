using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("NPC/NPC", 0), DisallowMultipleComponent]
public class Npc : Character,IDamageable
{
    public int maxHealth = 500;
    
    [Header("Runtime")]
    public bool isStunned;
    public bool isFallen;
    public Transform tauntedBy;
    public int health;

    public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
    {
        
    }

    public Transform Transform => transform;
    public DamageTarget DamageTarget => DamageTarget.NPC;
    

    private void Start()
    {
        health = maxHealth;
    }
    
    
    
    public void FaceTarget(Vector3 targetPosition)
    {
        var faceDirection = targetPosition - transform.position;
        faceDirection.y = 0;
        transform.forward = faceDirection.normalized;
    }
    
    List<IEffect<IDamageable>> _activeEffects = new();

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

    public void TakeDamage(int damageAmount, DamageType damageType)
    {
        if (damageType == DamageType.Physical)
        {
            health -= damageAmount;
            if (health <= 0)
            {
                Die();
            }
        }
        
    }

    private void Die()
    {
        Debug.Log("NPC has died.");

        foreach (var effect in _activeEffects)
        {
            effect.OnCompleted -= RemoveEffect;
            effect.Cancel();
        }
        
        Destroy(gameObject);
    }
}