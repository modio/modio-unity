using System;
using Modio.Unity.UI.Search;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyUser : ISearchProperty
    {
        [SerializeField] ModioUIUser _user;

        public void OnSearchUpdate(ModioUISearch search)
        {
            var users = search.LastSearchFilter.GetUsers();
            _user.SetUser(users.Count > 0 ? users[0] : null);
        }
    }
}
