using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class Unit : MonoBehaviour
{
    #region VARIABLES
    //~~    
    [SerializeField, Tooltip("Target = Player")] private Transform targetReference;
    [SerializeField] private float speed = 10;

    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private LayerMask unwalkableLayer;
    [SerializeField] private LayerMask targetLayer;

    private static float halfScaleX, halfScaleY;
    private float targetHalfScaleX, targetHalfScaleY;
    private const float selfOffset = .05f;
    private bool moveUp => bottomVector.y < target.position.y - (targetHalfScaleY + selfOffset);
    private bool moveDown => bottomVector.y > target.position.y + selfOffset - (targetHalfScaleY);
    private bool moveStraight => transform.position.x.Difference(target.position.x) > 1;
    private Vector2 distanceToTarget => transform.Distance(target);
    private Vector2 topVector => transform.Position2D() + Vector2.up * halfScaleY;
    private Vector2 bottomVector => transform.Position2D() + Vector2.down * halfScaleY;
    private Vector2 bottomFrontVector => bottomVector + Vector2.right * halfScaleX * transform.Sign(target);

    private Transform target;

    private const int STACKCOUNTER = 200;
    private int stackCounter = STACKCOUNTER;
    private float startDistance;

    #region Ladder Variables

    private Ladder[] ladders;
    private Ladder targetLadder;

    private Vector2 nearestLadderVector;
    private Vector2 ladderDirection;
    private Vector2 lastGrabLadderDirection;

    private bool onLadderUp, onLadderDown;
    private bool _isClimbingLadder;

    private const float avoidFloorDistance = .1f;
    private const float laderGrabDistance = .2f;
    private const float laderFloorDropDistance = .1f;

    private float _verticalVelocity;

    #endregion

    enum MovementStates { Null = 0, DownFloor = 1, SameFloor = 2, UpFloor = 3 }
    MovementStates movementState = MovementStates.Null;

    static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    //~~
    #endregion

    #region UNITY EVENT FUNCTIONS
    //~~    
    void Start()
    {
        InitializeTransform();

        SetTarget(targetReference);

        InitializeLadder();

        SetMovementState();
    }
    //~~
    #endregion

    #region METHODS
    //~~

    #region Initialize

    private void InitializeTransform()
    {
        halfScaleX = transform.localScale.x / 2;
        halfScaleY = transform.localScale.y / 2;
    }

    private void InitializeLadder()
    {
        ladders = FindObjectsOfType<Ladder>();
        nearestLadderVector = ladders[0].FirstStep;
    }

    #endregion

    async void SetMovementState()
    {
        try
        {
            await Task.Delay(100, destroyCancellationToken);

            stackCounter = STACKCOUNTER;

            CheckTargetGroundHit();

            if (moveUp && movementState != MovementStates.UpFloor)
            {
                movementState = MovementStates.UpFloor;
                print("Movement State : " + movementState);
                MoveToUpFloor();
            }
            else if (moveDown && movementState != MovementStates.DownFloor)
            {
                movementState = MovementStates.DownFloor;
                print("Movement State : " + movementState);
                MoveToDownFloor();
            }
            else if (moveStraight && movementState != MovementStates.SameFloor)
            {
                movementState = MovementStates.SameFloor;
                print("Movement State : " + movementState);
                MoveOnTheSameFloor();
            }
            else
            {
                movementState = MovementStates.Null;
                print("Movement State : " + movementState);
                SetMovementState();
            }
        }
        catch (OperationCanceledException)
        {
            print("Operation Canceled");
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private void SetTarget(Transform reference)
    {
        print("Target : " + reference.name);

        target = reference;
        targetHalfScaleX = target.localScale.x / 2;
        targetHalfScaleY = target.localScale.y / 2;
    }

    bool CheckToStack()
    {
        float distanceToTarget = Vector2.Distance(transform.Position2D(), target.Position2D());
        if (stackCounter == STACKCOUNTER)
        {
            startDistance = distanceToTarget;
        }

        stackCounter--;
        // print("Stack Counter : " + stackCounter);        

        if (stackCounter <= .0f && startDistance.Difference(distanceToTarget) < 1)
        {
            stackCounter = STACKCOUNTER;
            return true;
        }

        else if (stackCounter <= .0f)
        {
            stackCounter = STACKCOUNTER;
            return false;
        }
        return false;
    }

    bool CheckHitToTarget()
    {
        drawRays.Add(new DrawRay(bottomVector, Vector2.right * transform.Sign(target) * 100, Color.red));
        RaycastHit2D hitTarget = Physics2D.Raycast(transform.Position2D() + Vector2.down * halfScaleY, Vector2.right * transform.Sign(target), 100, targetLayer);

        //drawRays.Add(new DrawRay(transform.Position2D() + Vector2.down * halfScaleY, Vector2.left * 100, Color.red));
        //RaycastHit2D hitLeft = Physics2D.Raycast(transform.Position2D() + Vector2.down * halfScaleY, Vector2.left, 100, targetLayer);

        if (hitTarget)
        {
            print("Hit to Target");

            //onLadderUp = false;
            return true;
        }
        return false;
    }

    bool CheckTargetGroundHit()
    {
        Vector2 targetGroundRaycastStart = targetReference.Position2D() + Vector2.down * (targetReference.localScale.y / 2 + .01f);

        RaycastHit2D targetGroundHit = Physics2D.Raycast(targetGroundRaycastStart, Vector2.down, .2f);
        RaycastHit2D targetGroundHitLeft = Physics2D.Raycast(targetGroundRaycastStart + Vector2.left * targetReference.localScale.x / 2, Vector2.down, .2f);
        RaycastHit2D targetGroundHitRight = Physics2D.Raycast(targetGroundRaycastStart + Vector2.right * targetReference.localScale.x / 2, Vector2.down, .2f);

        #region Unwalkable Check

        RaycastHit2D targetUnwalkableHit = Physics2D.Raycast(targetGroundRaycastStart, Vector2.down, .2f, unwalkableLayer);
        RaycastHit2D targetUnwalkableHitLeft = Physics2D.Raycast(targetGroundRaycastStart + Vector2.left * targetReference.localScale.x / 2, Vector2.down, .2f, unwalkableLayer);
        RaycastHit2D targetUnwalkableHitRight = Physics2D.Raycast(targetGroundRaycastStart + Vector2.right * targetReference.localScale.x / 2, Vector2.down, .2f, unwalkableLayer);


        if (targetUnwalkableHit || targetUnwalkableHitLeft || targetUnwalkableHitRight)
        {
            print("Target Unwalkable Hit");

            Transform tempTarget = null;

            if (targetUnwalkableHit) tempTarget = targetUnwalkableHit.transform;
            else if (targetUnwalkableHitLeft) tempTarget = targetUnwalkableHitLeft.transform;
            else if (targetGroundHitRight) tempTarget = targetUnwalkableHitRight.transform;

            SetTarget(tempTarget);
        }
        else if (target != targetReference)
            SetTarget(targetReference);

        #endregion

        Debug.DrawRay(targetGroundRaycastStart, Vector2.down * .2f, Color.cyan);
        Debug.DrawRay(targetGroundRaycastStart + Vector2.left * targetReference.localScale.x / 2, Vector2.down * .2f, Color.cyan);
        Debug.DrawRay(targetGroundRaycastStart + Vector2.right * targetReference.localScale.x / 2, Vector2.down * .2f, Color.cyan);

        return targetGroundHit || targetGroundHitLeft || targetGroundHitRight;
    }

    #region Move Up

    void ClimbTheLadder(Vector2 dir)
    {
        //var climbingVector = ControlLadderMovement(dir);

        Vector2 bottomFrontPosition = bottomVector + dir.OneWay() * halfScaleX;

        RaycastHit2D ladderHit = Physics2D.Raycast(bottomFrontPosition, dir.OneWay(), .07f, walkableLayer);

        if (ladderHit && ladderHit.collider.usedByEffector)
            transform.Translate(Vector2.up * 8 * Time.deltaTime);
        else
            MoveStraight(transform.Position2D() + ladderDirection);
    }

    async void MoveToUpFloor()
    {
        nearestLadderVector = FindNearestLadder();
        Vector2 dir = new Vector2(ladderDirection.x.Sign(), ladderDirection.y.Sign());

        while (movementState == MovementStates.UpFloor && !destroyCancellationToken.IsCancellationRequested)
        {
            print("Move To Up Floor - While");

            if (distanceToTarget.magnitude < 1) break;

            if (!CheckTargetGroundHit()) break;

            // Move to the first step of the ladder
            if (!onLadderUp) MoveToLadder();

            // Climb the ladder
            else ClimbTheLadder(dir);

            if (transform.position.y > targetLadder.LastStepYTop)
            {
                print("Climbing Completed");

                transform.Position2D(transform.Position2D() + ladderDirection * .2f);
                onLadderUp = false;

                if (moveUp)
                {
                    print("One More Up");

                    await Task.Yield();

                    MoveToUpFloor();
                    return;
                }
                else
                {
                    print("Same Floor");
                    break;

                    //transform.position2D(new Vector2(transform.position.x + ladderDirection.x * .2f, target.position.y + .1f));
                }
            }

            //If it hits the target while climbing the ladder
            //if (transform.position.y < targetLadder.LastStepY - 1f && CheckHitToTarget())
            //if (CheckHitToTarget()) break;

            await Task.Yield();
        }

        await Task.Yield();

        SetMovementState();
    }

    #endregion

    #region Move Down

    bool GoDownTheLadder(Vector2 dir)
    {
        print("dir : " + dir);
        Vector2 bottomBack = bottomVector - dir.OneWay() * (halfScaleX + .05f);
        RaycastHit2D ladderHit = Physics2D.Raycast(bottomBack, Vector2.down, .05f, walkableLayer);
        Debug.DrawRay(bottomBack, Vector2.down * .05f, Color.cyan, .3f);

        if (ladderHit)
        {
            print("ladder hit name : " + ladderHit.transform.name);
            MoveStraight(transform.Position2D() + ladderDirection);
        }
        else
        {
            transform.Translate(Vector2.down * 4 * Time.deltaTime);
        }

        if (transform.position.y < targetLadder.LastStepYTop && ladderHit && !ladderHit.collider.usedByEffector)
        {
            print("Ladder Hit = Ground");
            return false;
        }
        print("ladder not hit");
        return true;
    }

    async void MoveToDownFloor()
    {
        nearestLadderVector = FindNearestLadder();

        print(targetLadder.gameObject.name);

        Vector2 dir = new Vector2(ladderDirection.x.Sign(), ladderDirection.y.Sign());

        while (movementState == MovementStates.DownFloor && !destroyCancellationToken.IsCancellationRequested)
        {
            print("Move To Down Floor - While");

            if (distanceToTarget.magnitude < 1)
            {
                print("Min Distance");
                break;
            }

            // Move to the first step of the ladder
            if (!onLadderDown)
            {
                if (!CheckTargetGroundHit()) break;

                MoveToLadder();
            }

            else if (GoDownTheLadder(dir) == false)
            {
                print("Go Down the Ladder Completed");

                onLadderDown = false;

                if (moveDown)
                {
                    print("One More Down");

                    MoveToDownFloor();
                    return;
                }
                else
                {
                    print("Down : Same Floor");
                    break;
                }
            }

            await Task.Yield();
        }

        await Task.Yield();

        SetMovementState();
    }

    #endregion

    #region Move Straight

    async void MoveOnTheSameFloor()
    {
        while (!moveUp && !moveDown && movementState == MovementStates.SameFloor && !destroyCancellationToken.IsCancellationRequested)
        {
            print("Move Straight");

            if (distanceToTarget.x < 1) break;

            Vector2 groundRaycastStart = bottomFrontVector;

            print("pos : " + transform.Position2D() + " | ground ray start : " + groundRaycastStart);

            RaycastHit2D groundHit = Physics2D.Raycast(groundRaycastStart + Vector2.down * selfOffset, Vector2.down, .2f, walkableLayer);

            RaycastHit2D downObstacleHit = Physics2D.Raycast(groundRaycastStart + Vector2.right * transform.Sign(target) * .1f, Vector2.right * transform.Sign(target), .2f);

            RaycastHit2D upObstacleHit = Physics2D.Raycast(groundRaycastStart + Vector2.right * transform.Sign(target) * .1f + Vector2.up * halfScaleY * 2, Vector2.right * transform.Sign(target), .2f);

            Debug.DrawRay(groundRaycastStart + Vector2.down * selfOffset, Vector2.down * .2f, Color.blue);
            Debug.DrawRay(groundRaycastStart, Vector2.right * transform.Sign(target) * .2f, Color.blue);
            Debug.DrawRay(groundRaycastStart + Vector2.up * halfScaleY * 2, Vector2.right * transform.Sign(target) * .2f, Color.blue);

            if ((downObstacleHit && !downObstacleHit.collider.usedByEffector) || (upObstacleHit && !upObstacleHit.collider.usedByEffector))
            {
                if (downObstacleHit) print("Down Obstacle : " + downObstacleHit.collider.gameObject.name);
                if (upObstacleHit) print("Up Obstacle : " + upObstacleHit.collider.gameObject.name);

                break;
            }

            if (groundHit)
            {
                if (!moveStraight) break;
                MoveStraight(target);
                print("Move Straight : Ground Hit");
            }

            else
            {
                print("Void Hit");
                break;
            }
            await Task.Yield();
        }

        SetMovementState();
    }

    void MoveStraight(Transform target, float minDistance = 0)
    {
        if (transform.DistanceX(target) <= minDistance) return;
        transform.Translate(Vector2.right * transform.Direction(target).normalized.x * speed * Time.deltaTime);
    }
    void MoveStraight(Vector2 target, float minDistance = 0)
    {
        if (transform.DistanceX(target) <= minDistance) return;
        transform.Translate(Vector2.right * transform.Direction(target).normalized.x * speed * Time.deltaTime);
    }
    #endregion

    #region Ladder

    Vector2 FindNearestLadder()
    {
        // If the movement is up, it finds the nearest ladder to itself and goes to the first step of the ladder.
        // If the movement is down, it finds the nearest ladder to the target and goes to the first step of the ladder.
        bool observerIsTarget = transform.position.y > target.position.y;
        print("Observer is Target : " + observerIsTarget);

        float minDistanceY = float.MaxValue; //currentLader.y.Difference(transform.position.y);
        float minDistanceX = float.MaxValue;

        foreach (Ladder ladder in ladders)
        {
            // If minY is the same in multiple ladders, the ladder that is not in the direction to be moved is eliminated.
            // Example: minY = 1 and -1 => save the ladder according to the direction of movement.
            if (observerIsTarget && ladder.FirstStepYTop > bottomVector.y)
            {
                print("target : " + targetLadder?.name + " | current : " + ladder.name);
                continue;
            }
            if (!observerIsTarget && ladder.LastStepYBottom < bottomVector.y)
            {
                print("target : " + targetLadder?.name + " | current : " + ladder.name);
                continue;
            }

            Vector2 targetStep = observerIsTarget ? ladder.LastVisibleStep : ladder.FirstStep;
            if (onLadderDown || onLadderUp)
            {
                print("onLadderDown : " + onLadderDown + " | onLadderUp : " + onLadderUp);
                targetStep = observerIsTarget ? ladder.FirstStep : ladder.LastVisibleStep;
                onLadderUp = !observerIsTarget;
                onLadderDown = observerIsTarget;
                print("Target Step : " + (observerIsTarget ? "First" : "Last"));
            }

            print(ladder + " Target Step : " + targetStep);

            if (targetStep.y.Difference(transform.position.y) < minDistanceY)
            {
                minDistanceY = targetStep.y.Difference(transform.position.y);
                minDistanceX = targetStep.x.Difference(transform.position.x);

                nearestLadderVector = observerIsTarget ? ladder.LastVisibleStep : ladder.FirstStep;
                ladderDirection = observerIsTarget ? -ladder.Direction : ladder.Direction;
                targetLadder = ladder;
            }

            else if (targetStep.y.Difference(transform.position.y) == minDistanceY)
            {
                minDistanceY = targetStep.y.Difference(transform.position.y);

                if (targetStep.x.Difference(transform.position.x) <= minDistanceX)
                {
                    minDistanceX = targetStep.x.Difference(transform.position.x);

                    nearestLadderVector = observerIsTarget ? ladder.LastVisibleStep : ladder.FirstStep;
                    ladderDirection = observerIsTarget ? -ladder.Direction : ladder.Direction;
                    targetLadder = ladder;
                }
            }
            //print(ladder.gameObject.name + " Dif : " + transform.position.y.Difference(targetStep.y) + " & minY : " + minDistanceY);
        }

        print("Target Ladder : " + targetLadder.gameObject.name);
        return nearestLadderVector;
    }

    void MoveToLadder()
    {
        if (transform.position.x.Difference(nearestLadderVector.x) < .1f)
        {
            if (ladderDirection.y > 0)
            {
                onLadderUp = true;
                print(targetLadder + " : first step reached");
                return;
            }
            else
            {
                onLadderDown = true;
                print(targetLadder + " : last step reached");
                return;
            }
        }
        if (ladderDirection.y > 0 && transform.position.y > targetLadder.FirstStep.y + 1f)
        {
            onLadderUp = true;
            print("ladderDirection.y > 0 && transform.position.y > targetLadder.FirstStep.y + 1f");
            return;
        }
        else if (ladderDirection.y < 0 && transform.position.y < targetLadder.LastStep.y - 1f)
        {
            onLadderDown = true;
            print("ladderDirection.y < 0 && transform.position.y < targetLadder.LastStep.y - 1f");
            return;
        }
        print("Move To Ladder" + " | onladderDown : " + onLadderDown + " | onladderUp : "
             + onLadderUp);

        MoveStraight(nearestLadderVector);
    }

    #endregion

    #region State Counter

    int movementStateCounter = -1;
    private async Task<int> MovementStateCounter()
    {
        movementStateCounter = 0;
        while (movementStateCounter < 5 && movementStateCounter > -1)
        {
            await Task.Delay(1000, destroyCancellationToken);
            movementStateCounter++;
        }
        if (movementStateCounter >= 4)
        {
            movementStateCounter = -1;
        }
        print("Movement State Counter : " + movementStateCounter);
        return movementStateCounter;
    }
    #endregion
    //~~
    #endregion

    #region EDITOR
    //~~
#if UNITY_EDITOR
    #region Gizmos
    private void OnDrawGizmos()
    {
        //drawRays.ForEach(ray => ray.Draw());
        //    print(drawRays.Count);
        //foreach (DrawRay ray in drawRays) ray.Draw();
    }

    public HashSet<DrawRay> drawRays = new();
    public struct DrawRay
    {
        Vector2 start, dir;
        Color color;
        public DrawRay(Vector2 start, Vector2 dir, Color color)
        {
            this.start = start;
            this.dir = dir;
            this.color = color;
        }
        public void Draw()
        {
            Debug.DrawRay(start, dir, color);
        }
    }
    #endregion
#endif
    //~~
    #endregion
}




#region Old Pathfinding
//~~
/*
IEnumerator FollowPathOld()
{
    if (path.Length > 0)
    {
        targetIndex = 0;
        Vector2 currentWaypoint = path[0];
        while (Vector2.Distance(target.position2D(), transform.position2D()) > 1)
        {
            Vector2 targetPosition = new Vector2(currentWaypoint.x, transform.position.y);
            Vector2 direction = (targetPosition - transform.position2D()).normalized;
            if (transform.position2D() == targetPosition)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            // climb ladders
            if (target.position.y - .1f > transform.position.y)
            {
                if (onLadderDown)
                {
                    //targetPosition.y = target.position.y;
                    Invoke(nameof(OnLadderDownFalse), 1f);
                }
                else
                {
                    nearestLadderVector = FindNearestLadder(nearestLadderVector);
                    if (Vector2.Distance(nearestLadderVector, transform.position2D()) < 1 && !onLadderUp)
                        targetPosition = nearestLadderVector;
                    else
                    {
                        onLadderUp = true;
                        var climbingVector = ControlLadderMovement(direction);
                        if (climbingVector.magnitude > .0f)
                            targetPosition = climbingVector;
                    }
                }

            }

            else if (target.position.y + .1f < transform.position.y)
            {
                /*float climbingStartPointX = transform.position.x + (transform.localScale.x / 2) * Mathf.Sign(direction.x);
                float climbingStartPointY = transform.position.y - ((transform.localScale.y / 2)) + avoidFloorDistance;

                Vector2 climbingStartPoint = Vector2.right * climbingStartPointX + Vector2.up * climbingStartPointY;
                RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, climbingStartPoint + direction, laderGrabDistance, walkableLayer);
                if (hit && hit.collider.usedByEffector)
                {
                    if (downX == 0) downX = direction.x;
                    print("hit && hit.collider.usedByEffector");
                    targetPosition.x = 100 * Mathf.Sign(downX) * speed;
                }

nearestLadderVector = FindNearestLadder(nearestLadderVector);
                if (onLadderDown)
                    targetPosition = nearestLadderVector;

print("ladder pos : " + targetPosition);

}


transform.position2D(Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime));

yield return null;

        }
    }
    /*else
    {
        var direction = Vector2.zero;
        var targetPosition = Vector2.zero;
        int limit = 500;
        while (limit > 0)
        {
            limit--;
            direction = (target.position - transform.position);
            if (targetPosition == Vector2.zero)
                targetPosition = Vector2.right * direction.x + Vector2.up * transform.position2D().y;
            transform.position2D(Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime));

            if (limit < 0 || Vector2.Distance(transform.position2D(), target.position2D()) < .2f) yield break;
            yield return null;
        }
    }
}
*/

/*
IEnumerator FollowPath()
{
    if (path.Length > 0)
    {
        targetIndex = 0;
        Vector2 currentWaypoint = path[0];
        while (Vector2.Distance(target.position2D(), transform.position2D()) > 1)
        {
            Vector2 targetPosition = new Vector2(currentWaypoint.x, transform.position.y);
            Vector2 direction = (targetPosition - transform.position2D()).normalized;
            if (transform.position2D() == targetPosition)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            // climb ladders
            if (target.position.y - .1f > transform.position.y)
            {
                if (onLadderDown)
                {
                    //targetPosition.y = target.position.y;
                    Invoke(nameof(OnLadderDownFalse), 1f);
                }
                else
                {
                    nearestLadderVector = FindNearestLadder(nearestLadderVector);
                    if (Vector2.Distance(nearestLadderVector, transform.position2D()) < 1 && !onLadderUp)
                        targetPosition = nearestLadderVector;
                    else
                    {
                        onLadderUp = true;
                        var climbingVector = ControlLadderMovement(direction);
                        if (climbingVector.magnitude > .0f)
                            targetPosition = climbingVector;
                    }
                }

            }

            else if (target.position.y + .1f < transform.position.y)
            {
                /*float climbingStartPointX = transform.position.x + (transform.localScale.x / 2) * Mathf.Sign(direction.x);
                float climbingStartPointY = transform.position.y - ((transform.localScale.y / 2)) + avoidFloorDistance;

                Vector2 climbingStartPoint = Vector2.right * climbingStartPointX + Vector2.up * climbingStartPointY;
                RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, climbingStartPoint + direction, laderGrabDistance, walkableLayer);
                if (hit && hit.collider.usedByEffector)
                {
                    if (downX == 0) downX = direction.x;
                    print("hit && hit.collider.usedByEffector");
                    targetPosition.x = 100 * Mathf.Sign(downX) * speed;
                }

                nearestLadderVector = FindNearestLadder(nearestLadderVector);
                if (onLadderDown)
                    targetPosition = nearestLadderVector;

                print("ladder pos : " + targetPosition);

            }


            transform.position2D(Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime));

            yield return null;

        }
    }
    /*else
    {
        var direction = Vector2.zero;
        var targetPosition = Vector2.zero;
        int limit = 500;
        while (limit > 0)
        {
            limit--;
            direction = (target.position - transform.position);
            if (targetPosition == Vector2.zero)
                targetPosition = Vector2.right * direction.x + Vector2.up * transform.position2D().y;
            transform.position2D(Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime));

            if (limit < 0 || Vector2.Distance(transform.position2D(), target.position2D()) < .2f) yield break;
            yield return null;
        }
    }
}
*/

/*
  IEnumerator RefreshPath()
    {
        /*
        Vector2 targetPositionOld = target.position2D() + Vector2.up; // ensure != to target.position initially

        while (true)
        {
            if (targetPositionOld != target.position2D())
            {
                targetPositionOld = target.position2D();

                path = Pathfinding.RequestPath(transform.position2D(), target.position2D());
                StopCoroutine("FollowPath");
                StartCoroutine("FollowPath");
            }

            yield return new WaitForSeconds(.25f);
        }

//To prevent error, null is returned
yield return null;
    }
 */

/*
     void OnLadderUpFalse()
    {
        onLadderUp = false;
        onLadderDown = true;
        //transform.position2D(new Vector2(transform.position.x, target.position.y));
    }

    void OnLadderDownFalse()
    {
        onLadderDown = true;
        // transform.position2D(new Vector2(transform.position.x, target.position.y));
    }
 */

//~~    
#endregion

#region Old Ladder Climbing
/*

 private Vector2 ControlLadderMovement(Vector2 movementVector)
    {
        Vector2 climbingVector = Vector2.zero;

        #region Ladder Climbing
        float climbingStartPointX = transform.position.x + (transform.localScale.x / 2) * Mathf.Sign(movementVector.x);
        float climbingStartPointY = transform.position.y - ((transform.localScale.y / 2)) + avoidFloorDistance;

        Vector2 climbingStartPoint = Vector2.right * climbingStartPointX + Vector2.up * climbingStartPointY;

        if (!_isClimbingLadder)
        {
            Debug.DrawLine(climbingStartPoint, climbingStartPoint + movementVector * laderGrabDistance, Color.green);
            RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, climbingStartPoint + movementVector, laderGrabDistance, walkableLayer);
            if (hit && hit.collider.usedByEffector)
                GrabLadder(movementVector);
        }

        else
        {
            RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, lastGrabLadderDirection, laderGrabDistance, walkableLayer);

            if (hit && !hit.collider.usedByEffector)
            {
                DropLadder();
                _verticalVelocity = 4f;
            }
            else if (!hit)
            {
                DropLadder();
                _verticalVelocity = 4f;
            }

            if (Vector2.Dot(movementVector, lastGrabLadderDirection) < 0)
            {
                hit = Physics2D.Raycast(transform.position2D(), Vector2.down, laderFloorDropDistance);
                if (hit) DropLadder();
            }

            if (_isClimbingLadder)
            {
                //movementVector = new Vector2(0, movementVector.y * 10);
                _verticalVelocity = 1f;
            }

            movementVector.x = transform.position.x + movementVector.x * Time.deltaTime;
            movementVector.y = transform.position.y + speed * _verticalVelocity * Time.deltaTime;
            climbingVector = movementVector;
        }
        return climbingVector;
        #endregion
    }

    private void GrabLadder(Vector2 lastGrabLadderDirection)
    {
        _isClimbingLadder = true;
        this.lastGrabLadderDirection = lastGrabLadderDirection;
    }

    private void DropLadder()
    {
        _isClimbingLadder = false;
    }

 */
#endregion