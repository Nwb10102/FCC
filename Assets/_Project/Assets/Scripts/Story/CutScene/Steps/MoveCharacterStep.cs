using System.Collections;
using UnityEngine;

public class MoveCharacterStep : CutSceneStep
{
    [SerializeField] private Transform character;
    [SerializeField] private Vector2 destination;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool waitForCompletion = true;

    public override IEnumerator Execute()
    {
        if (character == null) yield break;

        Vector3 dest = new Vector3(destination.x, destination.y, character.position.z);

        if (waitForCompletion)
        {
            while (Vector2.Distance(character.position, destination) > 0.01f)
            {
                character.position = Vector3.MoveTowards(character.position, dest, speed * Time.deltaTime);
                yield return null;
            }
            character.position = dest;
        }
        else
        {
            StartCoroutine(MoveAsync(character, dest));
        }
    }

    private IEnumerator MoveAsync(Transform t, Vector3 dest)
    {
        while (Vector2.Distance(t.position, dest) > 0.01f)
        {
            t.position = Vector3.MoveTowards(t.position, dest, speed * Time.deltaTime);
            yield return null;
        }
        t.position = dest;
    }
}
