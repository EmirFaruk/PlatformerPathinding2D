using UnityEngine;

public class Ladder : MonoBehaviour
{
    #region VARIABLES
    public Vector2 FirstStep => firstStep;
    private Vector2 firstStep;
    public Vector2 LastStep => lastStep;
    private Vector2 lastStep;

    public Vector2 LastVisibleStep => lastVisibleStep;
    private Vector2 lastVisibleStep;

    public Vector2 Direction => direction;
    private Vector2 direction;

    public float LastStepYTop => lastStepYTop;
    private float lastStepYTop;
    public float LastStepYBottom => lastStepYBottom;
    private float lastStepYBottom;

    public float FirstStepYTop => firstStepYTop;
    private float firstStepYTop;
    public float FirstStepYBottom => firstStepYBottom;
    private float firstStepYBottom;

    private void Awake()
    {
        var first = transform.GetChild(0);
        firstStep = first.Position2D();
        firstStepYTop = first.position.y + first.localScale.y / 2;
        firstStepYBottom = first.position.y - first.localScale.y / 2;

        var dir = firstStep - transform.GetChild(1).Position2D();
        firstStep.x += Mathf.Sign(dir.x) * .5f;
        firstStep.y += Mathf.Sign(dir.y) * .5f;

        Transform last = transform.GetChild(transform.childCount - 1);
        lastStep = last.Position2D();
        lastStepYTop = last.position.y + last.localScale.y / 2;
        lastStepYBottom = last.position.y - last.localScale.y / 2;

        dir = lastStep - transform.GetChild(transform.childCount - 2).Position2D();
        lastStep.x += Mathf.Sign(dir.x) * .5f;
        lastStep.y += Mathf.Sign(dir.y) * .5f;

        direction = (lastStep - firstStep).normalized;

        last = transform.GetChild(transform.childCount - 2);
        lastVisibleStep = last.Position2D();
    }
    #endregion


}
