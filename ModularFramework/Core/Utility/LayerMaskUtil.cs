using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModularFramework.Utility {
	public static class LayerMaskUtil {
		public static int ConvertLayerToMask(int layerIndex) {
			return 1 << layerIndex;
		}

		public static int InvertLayerMask(int layerMask) {
			return ~layerMask;
		}

		public static int CombineLayerMasks(IEnumerable<int> layerMasks)
		{
			int combined = 0;
			foreach(int l in layerMasks) {
				combined |= ConvertLayerToMask(l);
			}
			return combined;
		}

		public static int CombineLayerToMask(IEnumerable<string> layers) {
			return CombineLayerMasks(layers.Select(n=>LayerMask.GetMask(n)));
		}

		public static int MaskExceptLayers(IEnumerable<int> layerMasks) {
			return InvertLayerMask(CombineLayerMasks(layerMasks));
		}

		public static int MaskExceptLayers(IEnumerable<string> layers) {
			return MaskExceptLayers(layers.Select(n=>LayerMask.GetMask(n)));
		}
	}
}