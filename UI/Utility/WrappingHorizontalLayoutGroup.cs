using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is a light weight custom layout grid that will only work at runtime. This is used for the
/// tags displayed in the mod details panel. Each tag has a varying width but we need them to wrap
/// and display on the next line when it would otherwise exceed the layout's rect width.
/// Unity's default grid layout group cannot provide this functionality.
/// </summary>
public class WrappingHorizontalLayoutGroup : MonoBehaviour
{
    public float cellHeight;
    public Vector2 padding;
    HashSet<GameObject> elements = new HashSet<GameObject>();
    List<List<GameObject>> rows = new List<List<GameObject>>();
    float MaxWidth => ((RectTransform)transform).sizeDelta.x;
    float CurrentRowHeight => -1f * (cellHeight * (rows.Count - 1) + padding.y * (rows.Count - 1));
    
    public void AddGameObjectToLayout(GameObject gameObject)
    {
        if(elements.Contains(gameObject))
        {
            Debug.LogError("Can't add GO to layout group, it already exists.");
            return;
        }
        elements.Add(gameObject);
        gameObject.transform.SetParent(transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gameObject.transform);
        
        float width = ((RectTransform)gameObject.transform).sizeDelta.x + padding.x;
        var row = CurrentRow();
        float currentRowWidth = RowWidth(row);

        if(currentRowWidth + width > MaxWidth)
        {
            row = AddRow();
            currentRowWidth = 0f;
        }
        gameObject.transform.localPosition = new Vector2(currentRowWidth, CurrentRowHeight);
        row.Add(gameObject);
    }

    public void EmptyLayoutGroup()
    {
        elements.Clear();
        rows.Clear();
    }

    List<GameObject> AddRow()
    {
        rows.Add(new List<GameObject>());
        return rows[rows.Count - 1];
    }
    
    List<GameObject> CurrentRow()
    {
        if(rows.Count == 0)
        {
            return AddRow();
        }
        return rows[rows.Count - 1];
    }

    float RowWidth(List<GameObject> row)
    {
        float width = 0f;
        foreach(var go in row)
        {
            if (go.transform is RectTransform rectTransform)
            {
                width += rectTransform.sizeDelta.x + padding.x;
            }
        }
        return width;
    }
}
