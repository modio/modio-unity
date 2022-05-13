using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
	internal class ColorSetter : MonoBehaviour
	{
		public ColorSetterType type;
		MultiTargetButton button;

		void SetGraphicColor(Color color)
		{
			Graphic graphic = GetComponent<Graphic>();
			if(graphic != null)
			{
				float alpha = graphic.color.a; // maintain the alpha
				color.a = alpha;
				graphic.color = color;
			}
		}

		void SetSelectableColorBlock(ColorBlock colors)
		{
			Selectable selectable = GetComponent<Selectable>();
			if(selectable != null)
			{
				selectable.colors = colors;
			}
		}

		void SetDropdownColorBlocks()
		{
			
		}
		
		internal void Refresh()
		{
			ColorScheme scheme = GetComponentInParent<ColorScheme>();
			if(scheme != null)
			{
				scheme.RefreshUI();	
			}
		}
		
		internal void Refresh(ColorScheme scheme)
		{
			switch(type)
			{
				case ColorSetterType.Button:
					SetSelectableColorBlock(scheme.GetColorBlock_Button());
					break;
				case ColorSetterType.Dropdown:
					SetSelectableColorBlock(scheme.GetColorBlock_Button());
					break;
				default:
					SetGraphicColor(scheme.GetSchemeColor(type));
					break;
			}
 #if UNITY_EDITOR
			EditorUtility.SetDirty(this);
 #endif
		}
	}
}
