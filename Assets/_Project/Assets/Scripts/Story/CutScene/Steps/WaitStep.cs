using System.Collections;
using UnityEngine;

public class WaitStep : CutSceneStep
{
    [SerializeField] private float duration = 1f;

    public override IEnumerator Execute()
    {
        yield return new WaitForSeconds(duration);
    }
}
