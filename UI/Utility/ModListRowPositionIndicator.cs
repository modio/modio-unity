using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
    public class ModListRowPositionIndicator : MonoBehaviour, IMoveHandler
    {
        public void OnMove(AxisEventData eventData)
        {
            ModListRow.currentSelectedPosition = transform.position;
        }
    }
}
