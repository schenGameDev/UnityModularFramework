using ModularFramework;
using ModularFramework.Commons;
using UnityEngine;

[CreateAssetMenu(menuName = "Event Channel/Ink Var Event Channel", fileName = "InkVarChannel")]
public class InkVarEventChannel : EventChannel<(string,Keeper)> { }