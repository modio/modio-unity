using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;

internal class ModDetailsTagListItem : ListItem
{
    [SerializeField] TMP_Text text;
    
    public override void Setup(string title)
    {
        base.Setup(title);
        text.text = title;
        gameObject.SetActive(true);
    }
}
