using System.Collections.Generic;
using System.IO;
using ModIO.Implementation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Plugins.mod.io.Runtime.Utility
{
    public class TestSceneLoader : MonoBehaviour
    {
        [SerializeField]
        RectTransform _container;
        [SerializeField]
        Button _prefab;
        readonly List<Button> _buttonList = new List<Button>();

        void Start()
        {
            if (ModIOCommandLineArgs.TryGet("scene-index", out string sceneIndex) && int.TryParse(sceneIndex, out int index))
            {
                if (index >= 0 && index < SceneManager.sceneCountInBuildSettings)
                    SceneManager.LoadScene(index, LoadSceneMode.Single);
            }
            else if (ModIOCommandLineArgs.TryGet("scene-name", out string sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            CreateSceneButtons();
        }

        void CreateSceneButtons()
        {
            _prefab.gameObject.SetActive(false);
            for (int i = SceneManager.sceneCountInBuildSettings - 1; i >= 0; i--)
            {
                if (SceneManager.GetActiveScene().buildIndex == i)
                    continue;

                var button = CreateSceneButton(i);
                button.transform.SetAsFirstSibling();
            }
        }

        Button CreateSceneButton(int buildIndex)
        {
            var button = Instantiate(_prefab, _container);
            var text = button.GetComponentInChildren<Text>();
            if (text)
                text.text = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));

            button.onClick.AddListener(
                () =>
                {
                    DisableButtons();
                    SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
                }
            );
            button.gameObject.SetActive(true);
            _buttonList.Add(button);
            return button;
        }

        void DisableButtons()
        {
            foreach (var button in _buttonList)
            {
                if (button != null) button.enabled = false;
            }
        }
    }
}
