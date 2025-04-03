using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityNavigation = UnityEngine.UI.Navigation;

namespace Modio.Unity.UI.Navigation
{
    public class ModioGridNavigation : Selectable, ILayoutController
    {
        static readonly Queue<Selectable> PrevRow = new Queue<Selectable>();

        [SerializeField] bool _getSelectablesInChildrensChildren;

        [SerializeField] GameObject _fallbackSelectionToIfNoValidChildren;

        bool _selectChildImmediately;
        bool _needsDelayedNavigationCorrection;
        GameObject _lastSelectedGameObject;

        static readonly List<Selectable> ReusedSelectables = new List<Selectable>();

        static readonly Vector3[] PrevCorners = new Vector3[4];
        static readonly Vector3[] TransCorners = new Vector3[4];

        protected override void OnEnable()
        {
            base.OnEnable();

            _lastSelectedGameObject = null;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            _needsDelayedNavigationCorrection = true;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            //Override the transition to default to None. We're usually putting this on a group, and it will never stay selected itself
            transition = Transition.None;
            navigation = new UnityNavigation { mode = UnityNavigation.Mode.Explicit };
        }
#endif

        public void SetLayoutHorizontal() { }

        public void SetLayoutVertical()
        {
            _needsDelayedNavigationCorrection = true;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (_needsDelayedNavigationCorrection)
            {
                RecalculateNavigation();
                _needsDelayedNavigationCorrection = false;
            }

            // Do not perform child selection if selection has already been lost
            if (_selectChildImmediately && EventSystem.current.currentSelectedGameObject != gameObject)
                _selectChildImmediately = false;

            if (_selectChildImmediately)
            {
                _selectChildImmediately = false;

                var targetPosition = new Vector3(
                    -10000,
                    10000,
                    0
                ); // picks the most top left pos if no current selection

                if (_lastSelectedGameObject != null) targetPosition = _lastSelectedGameObject.transform.position;

                float closestDist = float.MaxValue;
                Selectable closestSelectable = null;

                foreach (Transform child in transform)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    if (_getSelectablesInChildrensChildren)
                    {
                        child.GetComponentsInChildren(ReusedSelectables);
                    }
                    else
                    {
                        ReusedSelectables.Clear();
                        var mainSelectable = child.GetComponent<Selectable>();
                        if (mainSelectable != null) ReusedSelectables.Add(mainSelectable);
                    }

                    foreach (Selectable selectable in ReusedSelectables)
                    {
                        if (selectable == this ||
                            !selectable.interactable ||
                            selectable.navigation.mode == UnityNavigation.Mode.None)
                            continue;

                        RectTransform rectTransform = selectable.transform as RectTransform;
                        float sqrDist = float.MaxValue;

                        if (rectTransform != null)
                        {
                            rectTransform.GetWorldCorners(TransCorners);

                            foreach (Vector3 corner in TransCorners)
                            {
                                var cornerSqrDist = (corner - targetPosition).sqrMagnitude;
                                sqrDist = Mathf.Min(sqrDist, cornerSqrDist);
                            }
                        }
                        else
                        {
                            sqrDist = (selectable.transform.position - targetPosition).sqrMagnitude;
                        }

                        if (sqrDist > closestDist) continue;

                        closestDist = sqrDist;
                        closestSelectable = selectable;
                    }
                }

                if (closestSelectable != null)
                    EventSystem.current.SetSelectedGameObject(closestSelectable.gameObject);
                else if (_fallbackSelectionToIfNoValidChildren != null)
                    EventSystem.current.SetSelectedGameObject(_fallbackSelectionToIfNoValidChildren);
                else
                    EventSystem.current.SetSelectedGameObject(_lastSelectedGameObject);
            }

            var currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection != null && currentSelection.activeInHierarchy)
            {
                _lastSelectedGameObject = currentSelection;
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            _selectChildImmediately = true;
        }

        void RecalculateNavigation()
        {
            Selectable prev = null;
            Selectable lastOnPrevRow = null;
            bool isFirstRow = true;
            int countOnCurrentRow = 0;
            PrevRow.Clear();

            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                if (_getSelectablesInChildrensChildren)
                {
                    child.GetComponentsInChildren(ReusedSelectables);
                }
                else
                {
                    ReusedSelectables.Clear();
                    var mainSelectable = child.GetComponent<Selectable>();
                    if (mainSelectable != null) ReusedSelectables.Add(mainSelectable);
                }

                foreach (Selectable selectable in ReusedSelectables)
                {
                    if (selectable == this ||
                        !selectable.interactable ||
                        selectable.navigation.mode == UnityNavigation.Mode.None)
                        continue;

                    var nav = selectable.navigation;
                    nav.mode = UnityNavigation.Mode.Explicit;

                    bool isToTheRight = false;

                    if (prev != null)
                    {
                        if (prev.transform is RectTransform prevRectTransform &&
                            selectable.transform is RectTransform rectTransform)
                        {
                            isToTheRight = IsToTheRight(prevRectTransform, rectTransform);
                        }
                        else
                        {
                            isToTheRight = prev.transform.position.x + 1f < selectable.transform.position.x;
                        }

                        isFirstRow &= isToTheRight;
                    }

                    if (isToTheRight)
                    {
                        countOnCurrentRow++;
                        nav.selectOnLeft = prev;

                        if (prev != null)
                        {
                            UnityNavigation prevNavigation = prev.navigation;
                            prevNavigation.selectOnRight = selectable;
                            prev.navigation = prevNavigation;
                        }
                    }
                    else
                    {
                        //make sure we don't have more than one row cached in the prev buffer
                        while (PrevRow.Count > countOnCurrentRow && PrevRow.Count > 0)
                        {
                            var trailingElement = PrevRow.Dequeue();

                            UnityNavigation prevNavigation = trailingElement.navigation;
                            prevNavigation.selectOnDown = prev;
                            trailingElement.navigation = prevNavigation;
                        }

                        countOnCurrentRow = 1;
                        nav.selectOnLeft = GetNeighbourInDir(MoveDirection.Left);

                        if (prev != null)
                        {
                            UnityNavigation prevNavigation = prev.navigation;
                            prevNavigation.selectOnRight = GetNeighbourInDir(MoveDirection.Right);
                            prev.navigation = prevNavigation;
                        }

                        lastOnPrevRow = prev;
                    }

                    if (isFirstRow)
                    {
                        nav.selectOnUp = GetNeighbourInDir(MoveDirection.Up);
                    }
                    else
                    {
                        Selectable elementAbove = lastOnPrevRow;

                        //Dequeue the previous element, but only if it's not from the current row
                        if (PrevRow.Count >= countOnCurrentRow)
                        {
                            elementAbove = PrevRow.Dequeue();

                            UnityNavigation aboveNavigation = elementAbove.navigation;
                            aboveNavigation.selectOnDown = selectable;
                            elementAbove.navigation = aboveNavigation;
                        }

                        nav.selectOnUp = elementAbove;
                    }

                    selectable.navigation = nav;

                    prev = selectable;
                    PrevRow.Enqueue(prev);
                }
            }

            //Get how many are on the previous (complete) row, rather than split between the last two
            int countOnPreviousRow = PrevRow.Count - countOnCurrentRow;

            foreach (Selectable bottomElement in PrevRow)
            {
                UnityNavigation prevNavigation = bottomElement.navigation;

                if (countOnPreviousRow-- > 0)
                {
                    prevNavigation.selectOnDown = prev;
                }
                else
                {
                    prevNavigation.selectOnDown = GetNeighbourInDir(MoveDirection.Down);
                }

                bottomElement.navigation = prevNavigation;
            }

            if (prev != null)
            {
                UnityNavigation prevNavigation = prev.navigation;
                prevNavigation.selectOnRight = GetNeighbourInDir(MoveDirection.Right);
                prev.navigation = prevNavigation;
            }
        }

        public void NeedsNavigationCorrection() => _needsDelayedNavigationCorrection = true;

        /// <summary>
        /// Checks if all corners of the given rectTransform are right of all corners of the previous transform
        /// </summary>
        static bool IsToTheRight(RectTransform prevRectTransform, RectTransform rectTransform)
        {
            prevRectTransform.GetWorldCorners(PrevCorners);
            rectTransform.GetWorldCorners(TransCorners);

            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3 prevCorner in PrevCorners)
            {
                max = Vector3.Max(prevCorner, max);
            }

            foreach (Vector3 corner in TransCorners)
            {
                if (corner.x < max.x) return false;
            }

            return true;
        }

        Selectable GetNeighbourInDir(MoveDirection moveDirection)
        {
            Selectable neighbour = this;
            int attempts = 0;

            do
            {
                neighbour = moveDirection switch
                {
                    MoveDirection.Left => neighbour.navigation.selectOnLeft,
                    MoveDirection.Up => neighbour.navigation.selectOnUp,
                    MoveDirection.Right => neighbour.navigation.selectOnRight,
                    MoveDirection.Down => neighbour.navigation.selectOnDown,
                    _ => throw new ArgumentOutOfRangeException(nameof(moveDirection), moveDirection, null),
                };
            }
            while (neighbour != null && !neighbour.isActiveAndEnabled && attempts++ < 10);

            if (neighbour == this) return null;

            return neighbour;
        }
    }
}
