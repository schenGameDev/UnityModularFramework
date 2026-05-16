using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [RequireComponent(typeof(Marker))]
    public class LineCard : TextPrinter
    {
        public string PrefabKey { get; set; } = "default";
        
    }
}