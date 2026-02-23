using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("NPC/Equipment", 0), DisallowMultipleComponent]
    public class Equipment : Npc
    {
        public bool isDeployed = true;
        public bool isPickedUp = false;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void PickUp(bool isPickedUp = true)
        {
            this.isPickedUp = isPickedUp;
            if (isPickedUp) Deploy(false);
        }

        private void Deploy(bool isDeployed = true)
        {
            this.isDeployed = isDeployed;
            _rigidbody.isKinematic = isDeployed;

        }
    }
}