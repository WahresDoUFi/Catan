using UnityEngine;

public class ModelColorManager : MonoBehaviour
{
    [SerializeField] private Material[] materials;
    [SerializeField] private Renderer[] renderers;

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
}