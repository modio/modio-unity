using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Modio.ModioUGC
{
    public class ModProjectMetadataTabController : TabController
    {


        public ModProjectMetadataTabController(
            VisualElement root,
            string selector,
            List<(string key, string value)> backer
        ) : this(root.Q<Tab>(selector), backer) { }


        public ModProjectMetadataTabController(Tab tab, List<(string key, string value)> backer) : base(tab)
        {

            var metaDataList = tab.Q<ListView>("mod-project-metadata-tab");
            var metaDataBlob = tab.Q<TextField>("mod-project-metadata-blob");

            metaDataList.itemsSource = backer;

            metaDataList.makeItem = () =>
            {
                TemplateContainer element = metaDataList.itemTemplate.Instantiate();
                element.style.flexDirection = FlexDirection.Row;
                return element;
            };

            metaDataList.bindItem = (element, i) =>
            {
                var (key, value) = backer[i];
                var keyField = element.Q<TextField>("metadata-key");
                var valueField = element.Q<TextField>("metadata-value");

                keyField.UnregisterValueChangedCallback(KeyChanged);
                valueField.UnregisterValueChangedCallback(ValueChanged);
                keyField.userData = i;
                valueField.userData = i;
                keyField.value = (key);
                valueField.value = (value);

                void KeyChanged(ChangeEvent<string> evt)
                {
                    //use the index stored in userData to update the correct entry in the backer list
                    var index = (int)((TextField)evt.target).userData;
                    backer[index] = (evt.newValue, backer[index].value);
                }

                keyField.RegisterValueChangedCallback(KeyChanged);

                void ValueChanged(ChangeEvent<string> evt)
                {
                    //use the index stored in userData to update the correct entry in the backer list
                    var index = (int)((TextField)evt.target).userData;
                    backer[index] = (backer[index].key, evt.newValue);
                }

                valueField.RegisterValueChangedCallback(ValueChanged);
            };

            metaDataList.RegisterCallback<ChangeEvent<string>>(
                _ =>
                {
                    if (metaDataBlob.userData is true && metaDataList.userData is true)
                    {
                        metaDataList.userData = false;
                        return;
                    }

                    metaDataList.userData = true;

                    metaDataBlob.value = string.Join("\n", backer.Select(kv => $"{kv.key}:{kv.value}"));

                }
            );

            metaDataList.itemsAdded += _ =>
            {
                if (metaDataBlob.userData is true && metaDataList.userData is true)
                {
                    metaDataList.userData = false;
                    return;
                }

                metaDataList.userData = true;
                metaDataBlob.value = string.Join("\n", backer.Select(kv => $"{kv.key}:{kv.value}"));
            };

            metaDataList.itemsRemoved += _ =>
            {
                if (metaDataBlob.userData is true && metaDataList.userData is true)
                {
                    metaDataList.userData = false;
                    return;
                }

                metaDataList.userData = true;
                metaDataBlob.value = string.Join("\n", backer.Select(kv => $"{kv.key}:{kv.value}"));
            };

            metaDataBlob.RegisterCallback<ChangeEvent<string>>(
                evt =>
                {

                    if (metaDataBlob.userData is true && metaDataList.userData is true)
                    {
                        metaDataList.userData = false;
                        return;
                    }

                    metaDataBlob.userData = true;

                    backer.Clear();

                    string[] lines = evt.newValue.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(new[] { ':' }, 2);

                        if (parts.Length == 2)
                            backer.Add((parts[0], parts[1]));
                    }

                    metaDataList.Rebuild();
                }
            );
        }
    }
}
