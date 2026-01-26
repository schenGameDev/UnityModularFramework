using UnityEngine;

public interface IDamageable
{
    public void TakeDamage(int damageAmount, DamageType damageType);
    
    public void TakeEffect(IEffect<IDamageable> effect);
    
    public void TakeSpecialCondition(SpecialCondition specialCondition);
    
    public void RemoveSpecialCondition(SpecialCondition specialCondition);
    
    public void AimedAtBy( bool isAiming, Transform attacker, string details = null);
    
    public Transform Transform { get; }
    
    public DamageTarget DamageTarget { get; }
}