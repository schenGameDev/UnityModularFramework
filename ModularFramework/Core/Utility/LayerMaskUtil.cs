using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModularFramework.Utility {
	public static class LayerMaskUtil {
		public static int ConvertLayerToMask(int layer) {
			return 1 << layer;
		}

		public static int InvertLayerMask(int layermask) {
			return ~layermask;
		}

		public static int CombineLayerToMask(IEnumerable<int> layers) {
			int combined = ConvertLayerToMask(layers.ElementAt(0));
			foreach(int l in layers.Skip(1)) {
				combined |= ConvertLayerToMask(l);
			}
			return combined;
		}

		public static int CombineLayerToMask(IEnumerable<string> layers) {
			return CombineLayerToMask(layers.Select(n=>LayerMask.NameToLayer(n)));
		}

		public static int MaskExceptLayers(IEnumerable<int> layers) {
			return InvertLayerMask(CombineLayerToMask(layers));
		}

		public static int MaskExceptLayers(IEnumerable<string> layers) {
			return MaskExceptLayers(layers.Select(n=>LayerMask.NameToLayer(n)));
		}
	}
}