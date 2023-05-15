using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
  public class MultiTargetToggle : Toggle
  {
    public ColorScheme scheme;
    public List<Target> extraTargets = new List<Target>();
    
    /// <summary>
    /// This is the object, eg Checkmark, inside the toggle that indicates if it is on or off.
    /// We cannot directly override the behavior that sets the checkmark graphic so instead we
    /// manage an additional object on our own that we can turn on and off whenever we do a
    /// state transition
    /// </summary>
    public GameObject targetIsOnIndicator;
    
    /// <summary>
    /// Same as above, but for when the toggle has been disabled
    /// </summary>
    public GameObject targetIsDisabledIndicator;

    /// <summary>
    /// There is a slight edge case with the 'targetIsOnIndicator' field. It wont get affected on
    /// toggle until DoStateTransition. So we have a method here to manually call it to be run
    /// after we toggle it
    /// </summary>
    public void DoStateTransition()
    {
      DoStateTransition(currentSelectionState, true);
    }
    
    protected override void DoStateTransition(SelectionState state, bool instant)
    {
      base.DoStateTransition(state, instant);

      if(targetIsOnIndicator != null)
      {
          targetIsOnIndicator.SetActive(isOn);
      }
      if(targetIsDisabledIndicator != null)
      {
          targetIsDisabledIndicator.SetActive(state == SelectionState.Disabled);
      }
      
      foreach(var target in extraTargets)
      {
        Color color;
        Sprite newSprite;
        string triggername;
        switch(state)
        {
          case Selectable.SelectionState.Normal:
            color = target.colors.normalColor;
            newSprite = (Sprite)null;
            triggername = target.animationTriggers.normalTrigger;
            break;
          case Selectable.SelectionState.Highlighted:
            color = target.colors.highlightedColor;
            newSprite = target.spriteState.highlightedSprite;
            triggername = target.animationTriggers.highlightedTrigger;
            break;
#if UNITY_2019_4_OR_NEWER
          case Selectable.SelectionState.Selected:
            color = target.colors.highlightedColor;
            newSprite = target.spriteState.highlightedSprite;
            triggername = target.animationTriggers.highlightedTrigger;
            break;
#endif
          case Selectable.SelectionState.Pressed:
            color = target.colors.pressedColor;
            newSprite = target.spriteState.pressedSprite;
            triggername = target.animationTriggers.pressedTrigger;
            break;
          case Selectable.SelectionState.Disabled:
            color = target.colors.disabledColor;
            newSprite = target.spriteState.disabledSprite;
            triggername = target.animationTriggers.disabledTrigger;
            break;
          default:
            color = Color.black;
            newSprite = (Sprite)null;
            triggername = string.Empty;
            break;
        }
        if(!this.gameObject.activeInHierarchy)
          return;
        switch(target.transition)
        {
          case MultiTargetTransition.ColorTint:
            StartColorTween(target.target, color * target.colors.colorMultiplier, target.colors.fadeDuration, instant);
            break;
          case MultiTargetTransition.SpriteSwap:
            DoSpriteSwap(target.target, newSprite);
            break;
          case MultiTargetTransition.Animation:
            TriggerAnimation(target.animator, target.animationTriggers, triggername);
            break;
          case MultiTargetTransition.DisableEnable:
            ToggleActiveState(target, state);
            break;
          case MultiTargetTransition.ColorScheme:
            UseSchemeColorTint(target, state, scheme, instant);
            break;
        }
      }
    }
    
    void UseSchemeColorTint(Target target, Selectable.SelectionState state, ColorScheme scheme, bool instant)
    {
      if(scheme == null)
      {
        return;
      }
      Color color = default;
      switch(state)
      {
        case SelectionState.Normal:
          color = scheme.GetSchemeColor(target.colorSchemeBlock.Normal);
          break;
        case SelectionState.Highlighted:
          color = scheme.GetSchemeColor(target.colorSchemeBlock.Highlighted);
          break;
#if UNITY_2019_4_OR_NEWER
        case SelectionState.Selected:
          color = scheme.GetSchemeColor(target.colorSchemeBlock.Highlighted);
          break;
#endif
        case SelectionState.Pressed:
          color = scheme.GetSchemeColor(target.colorSchemeBlock.Pressed);
          break;
        case SelectionState.Disabled:
          color = scheme.GetSchemeColor(target.colorSchemeBlock.Disabled);
          break;
      }
      StartColorTween(target.target, color * target.colorSchemeBlock.ColorMultiplier, target.colorSchemeBlock.FadeDuration, instant);
    }

    void ToggleActiveState(Target target, Selectable.SelectionState state)
    {
      switch(state)
      {
        case SelectionState.Normal:
          target.target.gameObject.SetActive(target.enableOnNormal);
          break;
        case SelectionState.Highlighted:
          target.target.gameObject.SetActive(target.enableOnHighlight);
          break;
#if UNITY_2019_4_OR_NEWER
        case SelectionState.Selected:
          target.target.gameObject.SetActive(target.enableOnHighlight);
          break;
#endif
        case SelectionState.Pressed:
          target.target.gameObject.SetActive(target.enableOnPressed);
          break;
        case SelectionState.Disabled:
          target.target.gameObject.SetActive(target.enableOnDisabled);
          break;
      }
    }

    void StartColorTween(Graphic target, Color targetColor, float fadeDuration, bool instant)
    {
      if((UnityEngine.Object)target == (UnityEngine.Object)null)
        return;
      target.CrossFadeColor(targetColor, !instant ? fadeDuration : 0.0f, true, true);
    }

    void DoSpriteSwap(Graphic target, Sprite newSprite)
    {
      if(target == null)
        return;
      if(target is Image image)
      {
        image.overrideSprite = newSprite;
      }
    }

    void TriggerAnimation(Animator targetAnimator, AnimationTriggers trigger, string triggerName)
    {
      if(this.transition != Selectable.Transition.Animation || (UnityEngine.Object)targetAnimator == (UnityEngine.Object)null || !this.animator.isActiveAndEnabled || !targetAnimator.hasBoundPlayables || string.IsNullOrEmpty(triggerName))
        return;
      this.animator.ResetTrigger(trigger.normalTrigger);
      this.animator.ResetTrigger(trigger.pressedTrigger);
      this.animator.ResetTrigger(trigger.highlightedTrigger);
      this.animator.ResetTrigger(trigger.disabledTrigger);
      this.animator.SetTrigger(triggerName);
    }

    // public override void OnPointerClick(PointerEventData eventData)
    // {
    //   if(eventData.button != PointerEventData.InputButton.Left)
    //     return;
    //   Press();
    // }
    //
    // public override void OnSubmit(BaseEventData eventData)
    // {
    //   Press();
    //   if(!this.IsActive() || !this.IsInteractable())
    //     return;
    //   DoStateTransition(SelectionState.Pressed, false);
    // }

    // void Press()
    // {
    //   if(!this.IsActive() || !this.IsInteractable())
    //     return;
    //   UISystemProfilerApi.AddMarker("Button.onClick", (UnityEngine.Object)this);
    //   onClick.Invoke();
    // }
  }
}
