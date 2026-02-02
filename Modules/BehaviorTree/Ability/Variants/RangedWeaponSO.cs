using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_SO", menuName = "Game Module/Ability/Ranged Weapon", order = 1)]
public class RangedWeaponSO : ProjectileAbilitySO
{
    [Min(0)] public float fireInterval;
    [Min(0)] public float reloadTime;
    [Min(0)] public int magazineSize;
}