using System;
using UnityEngine;

namespace ModularFramework
{
    [CreateAssetMenu(fileName = "AssetBucket_SO", menuName = "Bucket/Asset Bucket")]
    public class AssetBucket : ScriptableObject
    {
        [SerializeField] private AssetIdentity[] items;

        public void ForEach(Action<AssetIdentity> action) {
            Array.ForEach(items, action);
        }
    }
}