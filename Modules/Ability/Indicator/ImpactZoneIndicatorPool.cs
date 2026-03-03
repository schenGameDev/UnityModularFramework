using UnityEngine;
using UnityEngine.Pool;

namespace ModularFramework.Modules.Ability
{
    public class ImpactZoneIndicatorPool : MonoBehaviour
    {
        [SerializeField] private ImpactZoneIndicator prefab;
        
        private void Awake()
        {
            PrefabPool<ImpactZoneIndicator>.Register(prefab, CreatePool);
        }
        
        private ObjectPool<ImpactZoneIndicator> CreatePool(ImpactZoneIndicator indicatorPrefab)
        {
            return new ObjectPool<ImpactZoneIndicator>(
                createFunc: () =>
                {
                    var indicator = Instantiate(indicatorPrefab,Vector3.zero, Quaternion.identity);
                    return indicator;
                },
                actionOnRelease: indicator => indicator.Hide(),
                actionOnDestroy: indicator => Destroy(indicator.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
    }
}