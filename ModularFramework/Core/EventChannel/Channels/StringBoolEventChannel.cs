using ModularFramework;
using UnityEngine;

[CreateAssetMenu(menuName = "Event Channel/String,Bool Event Channel", fileName = "Channel")]
public class StringBoolEventChannel : EventChannel<(string,bool)> {}