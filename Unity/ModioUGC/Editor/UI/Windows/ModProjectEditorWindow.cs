using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modio.ModioUGC
{
    public class ModProjectEditorWindow : EditorWindow
    {
        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset = default;
        [SerializeField]
        StyleSheet m_light = default;
        [SerializeField]
        StyleSheet m_dark = default;
        
        [MenuItem("Window/UI Toolkit/ModProjectEditorWindow")]
        public static void ShowExample()
        {
            var wnd = GetWindow<ModProjectEditorWindow>();
            wnd.titleContent = new GUIContent("ModProjectEditorWindow");
        }

        List<(string key, string value)> metaDataBacker = new List<(string, string)>();
        ModProjectMetadataTabController _modProjectMetadataTabController;


        public void CreateGUI()
        {
            SetTheme();
            
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement treeAsset = m_VisualTreeAsset.Instantiate();
            treeAsset.style.flexGrow = 1;
            root.Add(treeAsset);

            var monetizationTabController = new TabController(
                root,
                "mod-project-monetization-tab"
            );
            
            var permissionsTabController = new TabController(
                root,
                "mod-project-permissions-tab"
            );
            
            var dependenciesTabController = new TabController(
                root,
                "mod-project-dependencies-tab"
            );
            
            monetizationTabController.Hide();
            permissionsTabController.Hide();
            dependenciesTabController.Hide();

            root.RegisterCallback<ChangeEvent<string>>(OnValueChanged);
            root.RegisterCallback<ChangeEvent<int>>(OnValueChanged);
            root.RegisterCallback<ChangeEvent<float>>(OnValueChanged);
            root.RegisterCallback<ChangeEvent<bool>>(OnValueChanged);

            _modProjectMetadataTabController = new ModProjectMetadataTabController(
                root,
                "mod-project-metadata-tab",
                metaDataBacker
            );
        }

        void SetTheme()
        {
            if(EditorGUIUtility.isProSkin && m_dark)
                rootVisualElement.styleSheets.Add(m_dark);
            else if(m_light)
                rootVisualElement.styleSheets.Add(m_light);
        }

        /// <summary>
        /// Handles the context click event for the mod project fields. If the clicked element is marked as changed, a context menu is shown with an option to revert the change. If the revert option is selected, the changed class is removed from the element and the context click manipulator is removed, effectively reverting the change.
        /// </summary>
        /// <param name="evt"></param>
        void OnContextClick(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is not VisualElement element)
                return;

            if (!element.ClassListContains("changed"))
                return;

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Revert",
                (_) =>
                {
                    element.RemoveFromClassList("changed");
                    var manipulator = (ContextualMenuManipulator)element.userData;
                    element.RemoveManipulator(manipulator);
                }
            );
        }

        
        /// <summary>
        /// Marks the mod project field containing the changed element as changed and adds a context click manipulator to it that allows reverting the change. This is done by traversing up the visual tree until an element with the class "mod-project-field" is found, which is then marked as changed.
        /// </summary>
        /// <param name="evt"></param>
        /// <typeparam name="T"></typeparam>
        void OnValueChanged<T>(ChangeEvent<T> evt)
        {
            if (evt.target is not VisualElement element)
                return;

            VisualElement modProjectField = FindModProjectField(element);

            if (modProjectField == null)
                return;

            if (modProjectField.ClassListContains("changed"))
                return;


            var manipulator = new ContextualMenuManipulator(OnContextClick);
            modProjectField.AddManipulator(manipulator);
            modProjectField.userData = manipulator;
            modProjectField.AddToClassList("changed");

        }

        /// <summary>
        /// Marks the mod project field containing the changed element as changed and adds a context click manipulator to it that allows reverting the change. This is done by traversing up the visual tree until an element with the class "mod-project-field" is found, which is then marked as changed.
        /// If no such element is found, the method returns null.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        static VisualElement FindModProjectField(VisualElement element)
        {
            const string className = "mod-project-field";

            while (element != null)
            {

                if (element.ClassListContains(className))
                    return element;

                element = element.parent;
            }

            return null;
        }
    }
}
