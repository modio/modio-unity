#if UNITY_EDITOR

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ModIOBrowser
{
    [CanEditMultipleObjects] 
    [CustomEditor(typeof(MonoBehaviour), true)] 
    public class MonoBehaviourCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); 

            foreach(var method in target.GetType().GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance))
            {                
                var attributes = method.GetCustomAttributes(typeof(ExposeMethodInEditorAttribute), true);
                if(attributes.Length > 0)
                {
                    string buttonName = method.Name[0].ToString();
                    foreach(var item in method.Name.Substring(1))
                    {
                        if(char.IsUpper(item))
                        {
                            buttonName += " " + item.ToString();
                        }
                        else
                            buttonName += item.ToString();
                    }

                    if(GUILayout.Button(buttonName))
                    {
                        ((MonoBehaviour)target).Invoke(method.Name, 0f);
                    }
                }
            }            
        }
    }
}

#endif
