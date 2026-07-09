using System.Collections;
using UnityEngine;

public class AnimationStep : CutSceneStep
{
    public enum ParamType { Trigger, Bool, Int, Float }

    [SerializeField] private Animator animator;
    [SerializeField] private ParamType paramType = ParamType.Trigger;
    [SerializeField] private string paramName;
    [SerializeField] private bool boolValue;
    [SerializeField] private int intValue;
    [SerializeField] private float floatValue;
    [SerializeField] private float waitAfter;

    public override IEnumerator Execute()
    {
        if (animator == null) yield break;

        switch (paramType)
        {
            case ParamType.Trigger: animator.SetTrigger(paramName);             break;
            case ParamType.Bool:    animator.SetBool(paramName, boolValue);     break;
            case ParamType.Int:     animator.SetInteger(paramName, intValue);   break;
            case ParamType.Float:   animator.SetFloat(paramName, floatValue);   break;
        }

        if (waitAfter > 0f)
            yield return new WaitForSeconds(waitAfter);
    }
}
