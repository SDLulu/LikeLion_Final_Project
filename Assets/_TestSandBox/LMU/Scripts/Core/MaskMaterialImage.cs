namespace MaskTransitions
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Rendering;

    public class MaskMaterialImage : Image
    {
        private Material _cachedMaterial;

        public override Material materialForRendering
        {
            get
            {
                if (_cachedMaterial == null)
                {
                    _cachedMaterial = new Material(base.materialForRendering);
                    _cachedMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
                }
                return _cachedMaterial;
            }
        }

        protected override void OnDestroy()
        {
            if (_cachedMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_cachedMaterial);
                }
                else
                {
                    DestroyImmediate(_cachedMaterial);
                }
            }
            base.OnDestroy();
        }

    }
}

