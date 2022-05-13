using System;
using System.Collections.Generic;
using ModIO;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
	/// <summary>
	/// This class is intended to be inherited by container classes for list item type UI objects
	/// such as mod profiles, mod collections, mod search results, tags, etc
	/// </summary>
	internal class ListItem : MonoBehaviour
	{
		static Dictionary<Type, List<ListItem>> ListItems = new Dictionary<Type, List<ListItem>>();

		static ListItem LastCreatedListItem;

		public ViewportRestraint viewportRestraint;

		public Selectable selectable;

		public bool isPlaceholder;

		public List<ColorSetter> colorSetters = new List<ColorSetter>();
		public List<MultiTargetButton> buttons = new List<MultiTargetButton>();
		public List<MultiTargetDropdown> dropdowns = new List<MultiTargetDropdown>();
		public List<MultiTargetToggle> toggles = new List<MultiTargetToggle>();

		public ColorScheme scheme;

		void Awake()
		{
			LastCreatedListItem = this;
		}

		void Reset()
		{
			GetColorSchemeComponents();
		}

		[ContextMenu("Get Color Setters")]
		public void GetColorSchemeComponents()
		{
			colorSetters = new List<ColorSetter>(GetComponentsInChildren<ColorSetter>());
			buttons = new List<MultiTargetButton>(GetComponentsInChildren<MultiTargetButton>());
			dropdowns = new List<MultiTargetDropdown>(GetComponentsInChildren<MultiTargetDropdown>());
			toggles = new List<MultiTargetToggle>(GetComponentsInChildren<MultiTargetToggle>());
		}

		public void SetColorScheme(ColorScheme scheme)
		{
			this.scheme = scheme;
			foreach(var setter in colorSetters)
			{
				setter.Refresh(scheme);
			}
			foreach(var button in buttons)
			{
				button.scheme = scheme;
			}
			foreach(var dropdown in dropdowns)
			{
				dropdown.scheme = scheme;
			}
			foreach(var toggle in toggles)
			{
				toggle.scheme = scheme;
			}
		}

		/// <summary>
		/// Sets the viewport restraint for this item. eg when this list item becomes selected it
		/// will automatically check if it is within the screen space, if not it will trigger a
		/// transition to bring it into the screen space.
		/// </summary>
		/// <param name="content">the content to scroll/move when needed</param>
		/// <param name="viewport">the viewport to check (uses screen by default)</param>
		public virtual void SetViewportRestraint(RectTransform content, RectTransform viewport)
		{
			if(viewportRestraint == null)
			{
				viewportRestraint = gameObject.AddComponent<ViewportRestraint>();
				viewportRestraint.Container = content;
				viewportRestraint.Viewport = viewport;
			}
		}

		public virtual void Select() { }
		public virtual void PlaceholderSetup() { isPlaceholder = true; }
		public virtual void Setup() { isPlaceholder = false; }
		public virtual void Setup(string title) { isPlaceholder = false; }
		public virtual void Setup(string tagName, string tagCategory) { isPlaceholder = false; }
		public virtual void Setup(ModProfile profile) { isPlaceholder = false; }
		public virtual void Setup(SubscribedMod profile) { isPlaceholder = false; }
		public virtual void Setup(InstalledMod profile) { isPlaceholder = false; }
		public virtual void Setup(ModProfile profile, bool subscriptionStatus, string progressStatus) { isPlaceholder = false; }
		public virtual void Setup(Action onClick) { isPlaceholder = false; }
		public virtual void Setup(string title, Action onClick) { isPlaceholder = false; }

		public void RedrawRectTransform()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
		}

		/// <summary>
		/// Use this to either instantiate a new list item or check for an existing one that isn't
		/// already in use that we can recycle.
		/// </summary>
		/// <param name="prefab">the prefab to instantiate</param>
		/// <param name="parent">the parent to assign this list item to</param>
		/// <param name="scheme">the color scheme pattern to force onto this list item</param>
		/// <param name="getPlaceholders">whether or not to get placeholder items instead</param>
		/// <typeparam name="T">the class type of the list item we're using. eg CollectionModListItem.cs</typeparam>
		/// <returns></returns>
		public static ListItem GetListItem<T>(GameObject prefab, Transform parent, ColorScheme scheme, bool getPlaceholders = false)
		{
			Type type = typeof(T);

			// make sure we have an instance reference for this type of prefab/type
			if(!ListItems.ContainsKey(type))
			{
				ListItems.Add(type, new List<ListItem>());
			}

			// Try to find an unused list item of this type to recycle
			foreach(ListItem li in ListItems[type])
			{
				if(!li.gameObject.activeSelf || (li.isPlaceholder && !getPlaceholders))
				{
					li.SetColorScheme(scheme);
					li.transform.SetParent(parent);
					return li;
				}
			}

			// if no list item was found create a new one
			Instantiate(prefab, parent);
			ListItems[type].Add(LastCreatedListItem);

			LastCreatedListItem.SetColorScheme(scheme);
			return LastCreatedListItem;
		}

		/// <summary>
		/// Use this to hide all of the list items that exist of a specific type.
		/// Eg CollectionModListItem
		/// </summary>
		/// <param name="placeholdersOnly">whether or not to only remove unused placeholders</param>
		/// <typeparam name="T">the type of list item</typeparam>
		public static void HideListItems<T>(bool placeholdersOnly = false)
		{
			Type type = typeof(T);

			if(ListItems.ContainsKey(type))
			{
				foreach(ListItem li in ListItems[type])
				{
					if(placeholdersOnly && !li.isPlaceholder)
					{
						continue;
					}
					li.gameObject.SetActive(false);
				}
			}
		}
	}
}
