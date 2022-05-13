using System.Collections;
using UnityEngine;

namespace ModIOBrowser
{
    public class BrowserSpawnIn : MonoBehaviour
    {
        public GameObject browserPrefab;

        bool hasSpawned = false;

        public void SpawnIn()
        {
            if(!hasSpawned)
            {
                Instantiate(browserPrefab);
                hasSpawned = true;
            }
        
            Browser.Open(null);
        }
    }
}
