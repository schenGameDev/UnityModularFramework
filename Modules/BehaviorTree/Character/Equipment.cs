using ModularFramework;
using UnityEngine;

[AddComponentMenu("NPC/Equipment", 0), DisallowMultipleComponent]
public class Equipment : Npc
{
    public bool isDeployed = true;
    public bool isPickedUp = false;
    
    private Rigidbody _rigidbody;
    
    protected virtual void OnEnable()
    {
        Registry<Equipment>.TryAdd(this);
    }

    protected virtual void OnDisable()
    {
        Registry<Equipment>.Remove(this);
    }
    
    protected virtual void OnDestroy()
    {
        Registry<Equipment>.Remove(this);
    }
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void PickUp(bool isPickedUp = true)
    {
        this.isPickedUp = isPickedUp;
        if(isPickedUp) Deploy(false);
    }

    private void Deploy(bool isDeployed = true)
    {
        this.isDeployed = isDeployed;
        _rigidbody.isKinematic = isDeployed;
        
    }
}