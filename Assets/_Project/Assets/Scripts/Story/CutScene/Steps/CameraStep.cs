using System.Collections;
using UnityEngine;

public class CameraStep : CutSceneStep
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 destination;
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool waitForCompletion = true;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Reset() => targetCamera = Camera.main;

    public override IEnumerator Execute()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) yield break;

        Vector3 from = cam.transform.position;
        Vector3 to = new Vector3(destination.x, destination.y, from.z);

        if (waitForCompletion)
            yield return StartCoroutine(MoveCamera(cam, from, to));
        else
            StartCoroutine(MoveCamera(cam, from, to));
    }

    private IEnumerator MoveCamera(Camera cam, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            cam.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        cam.transform.position = to;
    }
}
