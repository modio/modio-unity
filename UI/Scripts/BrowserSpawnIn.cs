using System.Collections;
using UnityEngine;

namespace ModIOBrowser
{
    public class BrowserSpawnIn : MonoBehaviour
    {
        public GameObject browserPrefab;
        GameObject spawnedBrowser;

        bool hasSpawned => spawnedBrowser != null;

        public void SpawnIn()
        {
            if(!hasSpawned)
            {
                spawnedBrowser = Instantiate(browserPrefab);
            }
        
            Browser.Open(null);
        }
    }
}
