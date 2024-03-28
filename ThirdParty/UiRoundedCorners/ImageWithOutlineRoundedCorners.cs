using UnityEngine;
using UnityEngine.UI;

namespace Nobi.UiRoundedCorners {
    [ExecuteInEditMode]								//Required to check the OnEnable function
    [DisallowMultipleComponent]                     //You can only have one of these in every object.
    [RequireComponent(typeof(RectTransform))]
	public class ImageWithOutlineRoundedCorners : MonoBehaviour {
		private static readonly int Props = Shader.PropertyToID("_WidthHeightRadius");
		private static readonly int Thick = Shader.PropertyToID("_Thickness");

        public float radius = 40f;
        public float thickness = 5f;
        private Material material;
        private const string SHADER_PATH = "UI/RoundedCorners/OutlineWithRoundedCorners";

		[HideInInspector, SerializeField] private MaskableGraphic image;

		private void OnValidate() {
			Validate();
			Refresh();
		}

		private void OnDestroy() {
            image.material = null;      //This makes so that when the component is removed, the UI material returns to null

            DestroyHelper.Destroy(material);
			image = null;
			material = null;
		}

		private void OnEnable() {
            //You can only add either ImageWithRoundedCorners or ImageWithIndependentRoundedCorners
            //It will replace the other component when added into the object.
            var other = GetComponent<ImageWithIndependentRoundedCorners>();
            if (other != null)
            {
                radius = other.r.x;					//When it does, transfer the radius value to this script
                DestroyHelper.Destroy(other);
            }

            Validate();
			Refresh();
		}

		private void OnRectTransformDimensionsChange() {
			if (enabled && material != null) {
				Refresh();
			}
		}

		public void Validate()
        {
            var shader = Shader.Find(SHADER_PATH);
            if (shader == null)
            {
                Debug.LogWarning($"Cannot find the specified shader! {SHADER_PATH}");
                return;
            }

			if (material == null)
            {
				material = new Material(shader);
			}

			if (image == null) {
				TryGetComponent(out image);
			}

			if (image != null) {
				image.material = material;
			}
		}

		public void Refresh() {
            var shader = Shader.Find(SHADER_PATH);
            if(shader == null)
                return;

			var rect = ((RectTransform)transform).rect;

            //Multiply radius value by 2 to make the radius value appear consistent with ImageWithIndependentRoundedCorners script.
            //Right now, the ImageWithIndependentRoundedCorners appears to have double the radius than this.
            material.SetVector(Props, new Vector4(rect.width, rect.height, radius * 2, 0));
            material.SetFloat(Thick, thickness);
        }
	}
}
