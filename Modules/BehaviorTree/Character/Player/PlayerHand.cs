using System.Linq;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player hand to manage weapons and hold items
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Player))]
public class PlayerHand : MonoBehaviour
{
    [Header("Config")]
    [SerializeField,Required] private RangedWeaponSO[] weaponSlots;
    [SerializeField] private Vector3 fireOffset = new Vector3(0, 0, 1.5f);
    [SerializeField] private EventChannel<bool> fireChannel, interactChannel;
    [SerializeField] private EventChannel item1Channel, item2Channel, item3Channel;
    
    [Header("UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private Image reloadBar;
    
    private static readonly RangeFilter PICKUP_RANGE_FILTER = new RangeFilter
    {
        rangeType = RangeFilter.RangeType.CYLINDER,
        minMaxRange = new Vector2(0, 2f),
        minMaxHeight = new Vector2(-1f, 1f),
        viewAngle = 60,
    };

    private Player _player;
    private Equipment _pickedUpEquip;
    private RangedWeaponSO _weapon; // current holding
    
    [Header("Runtime")]
    private int _currentWeaponIndex = 0;
    public int currentAmmo = 0;
    private float _lastFireTime = 0f;
    private float _reloadTime = 0f;
    private bool _isAiming = false;
    private LayerMask _collisionMask;
    private Autowire<CameraManagerSO> _cameraManager = new();

    private void Awake()
    {
        _player = GetComponent<Player>();
    }


    private void Start()
    {
        ChangeWeapon0();
        _collisionMask = SingletonRegistry<ProjectileManagerSO>.Instance.collisionMask;
        weaponSlots.ForEach(w => w.RegisterProjectile());
    }

    private void Update()
    {
        // if(!isServer) return; 
        if (_pickedUpEquip != null)
        {
            _pickedUpEquip.transform.position = transform.position + transform.forward;
        }

        if (currentAmmo <= 0)
        {
            _reloadTime += Time.deltaTime;
            if (_reloadTime >= _weapon.reloadTime)
            {
                currentAmmo = _weapon.magazineSize;
                _reloadTime = 0f;
                // Debug.Log($"{name} has reloaded.");
            }

            OnReloadTimeChanged();
        }
    }

    private void OnEnable()
    {
        LinkEventChannels();
    }

    private void OnDisable()
    {
        UnlinkEventChannels();
    }

    private void ChangeWeapon(int index)
    {
        
        if(index == _currentWeaponIndex) return;
        if(index < 0 || index >= weaponSlots.Length) return;
        _currentWeaponIndex = index;
        _weapon = weaponSlots[index];
        currentAmmo = _weapon.magazineSize;
        _weapon.projectileSpawnOffset = fireOffset;
        ResetReloadTime();
    }

    private void ChangeWeapon0() => ChangeWeapon(0);
    private void ChangeWeapon1() => ChangeWeapon(1);
    private void ChangeWeapon2() => ChangeWeapon(2);
    
    private void ResetReloadTime()
    {
        _reloadTime = 0f;
    }
    
    private void PickUpEquipment(bool pressed)
    {
        if(!pressed) return;
        if (_pickedUpEquip)
        {
            _pickedUpEquip.PickUp(false);
            _pickedUpEquip = null;
            return;
        }
        
        var equip = Registry<Equipment>.Filter(((ITransformTargetFilter)PICKUP_RANGE_FILTER).GetStrategy<Equipment>(transform)).FirstOrDefault();
        if (equip)
        {
            _pickedUpEquip = equip;
            _pickedUpEquip.transform.position = transform.position + transform.forward * 2 + Vector3.up;
            _pickedUpEquip.transform.rotation = Quaternion.identity;
            _pickedUpEquip.PickUp();
        }
    }
    
    private void Attack(bool pressed)
    {
        // todo: hold to fire continuously
        if (_weapon.projectilePrefab.aimType == Projectile.AimType.Direction)
        {
            if(pressed) FireAtDirection(transform.forward);
        }
        else if(InputSystemSO.InputDevice == InputSystemSO.InputDeviceType.GAMEPAD)
        {
            _cameraManager.Get().UseGamepadAsPointer(pressed);
            if (!pressed)
            {
                FireAtTarget(_cameraManager.Get().GetPointerWorldPosition(_collisionMask));
            }
        }
        else
        {
            if(pressed) FireAtTarget(_cameraManager.Get().GetPointerWorldPosition(_collisionMask));
        }
    }

    private void FireAtDirection(Vector3 direction)
    {
        if(currentAmmo <= 0) return;
        if (_weapon.fireInterval > 0f && Time.time - _lastFireTime < _weapon.fireInterval)
        {
            return;
        }
        
        direction.y = 0;
        _weapon.DryFire(transform, null, direction, null);
        currentAmmo--;
        _lastFireTime = Time.time;
    }
    
    private void FireAtTarget(Vector3 targetPosition)
    {
        if(currentAmmo <= 0) return;
        
        _weapon.DryFire(transform, targetPosition, null, null);
        currentAmmo--;
    }
    
    public void LinkEventChannels()
    {
        fireChannel?.AddListener(Attack);
        item1Channel?.AddListener(ChangeWeapon0);
        item2Channel?.AddListener(ChangeWeapon1);
        item3Channel?.AddListener(ChangeWeapon2);
        interactChannel?.AddListener(PickUpEquipment);
    }

    private void UnlinkEventChannels()
    {
        fireChannel?.RemoveListener(Attack);
        item1Channel?.RemoveListener(ChangeWeapon0);
        item2Channel?.RemoveListener(ChangeWeapon1);
        item3Channel?.RemoveListener(ChangeWeapon2);
        interactChannel?.RemoveListener(PickUpEquipment);
    }
    
    private void OnReloadTimeChanged()
    {
        canvas.SetActive(true);
        reloadBar.fillAmount = _reloadTime / _weapon.reloadTime;
    }
}