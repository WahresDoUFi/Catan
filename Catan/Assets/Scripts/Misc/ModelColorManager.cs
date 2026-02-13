using UnityEngine;

namespace Misc
{
    public class ModelColorManager : MonoBehaviour
    {
        [SerializeField] private Material[] materials;
        [SerializeField] private Renderer[] renderers;

        [Header("Only for Streets")]
        [SerializeField] private Renderer plaza1Renderer;
        [SerializeField] private Renderer plaza2Renderer;

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void SetColor(Color color)
        {
            foreach (var r in renderers)
            {
                if (!r) continue;

                var sharedMats = r.sharedMaterials;

                for (int i = 0; i < sharedMats.Length; i++)
                {
                    var mat = sharedMats[i];
                    if (mat == null) continue;

                    if (System.Array.IndexOf(materials, mat) == -1)
                        continue;

                    r.GetPropertyBlock(_propertyBlock, i);
                    _propertyBlock.SetColor("_BaseColor", color);
                    r.SetPropertyBlock(_propertyBlock, i);
                }
            }
        }

        public void MixColor(Color additionalColor, bool plaza1)
        {
            var r = plaza1 ? plaza1Renderer : plaza2Renderer;
            Color baseColor = r.materials[1].color;
            Color other = additionalColor;
            Color mixed = Color.Lerp(baseColor, other, 0.5f);

            var sharedMats = r.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                var mat = sharedMats[i];
                if (mat == null) continue;

                if (System.Array.IndexOf(materials, mat) == -1)
                    continue;

                r.GetPropertyBlock(_propertyBlock, i);
                _propertyBlock.SetColor("_BaseColor", mixed);
                r.SetPropertyBlock(_propertyBlock, i);
            }
        }
    }
}