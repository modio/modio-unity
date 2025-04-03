using Modio.Unity.UI.Components;
using Modio.Unity.UI.Components.ModProperties;
using Modio.Unity.UI.Components.SearchProperties;
using Modio.Unity.UI.Components.UserProperties;
using Modio.Unity.UI.Editor.Common;
using UnityEditor;
using UnityEditorInternal;

namespace Modio.Unity.UI.Editor.Components
{
    public class ModioUIPropertiesBaseEditor : UnityEditor.Editor
    {
        static readonly string[] Exclude = { "m_Script", "_properties" };

        protected ReorderableList Properties;

        protected ReorderableList GetReorderableReferenceArray<T>() => ReorderableReferenceArray.New<T>(serializedObject.FindProperty("_properties"));

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, Exclude);

            EditorGUILayout.Space();

            Properties.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }

    public class ModioUIPropertiesBaseEditor<TProperty> : ModioUIPropertiesBaseEditor
    {
        void OnEnable() => Properties = GetReorderableReferenceArray<TProperty>();
    }

    [CustomEditor(typeof(ModioUIModProperties), true)]
    public class ModioUIModPropertiesEditor : ModioUIPropertiesBaseEditor<IModProperty> { }

    [CustomEditor(typeof(ModioUIUserProperties), true)]
    public class ModioUIUserPropertiesEditor : ModioUIPropertiesBaseEditor<IUserProperty> { }
    [CustomEditor(typeof(ModioUISearchProperties), true)]
    public class ModioUISearchPropertiesEditor : ModioUIPropertiesBaseEditor<ISearchProperty> { }
}
