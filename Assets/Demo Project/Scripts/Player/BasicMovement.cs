using UnityEngine;
using Input = UnityEngine.Input;

public class BasicMovement : MonoBehaviour
{
    public float speed = 10f;
    public float jumpForce = 10f;
    Vector2 movementVector;
    Vector2 verticalVector;

    // climbing
    private bool _isClimbingLadder;
    private const float avoidFloorDistance = .1f;
    private const float laderGrabDistance = .2f;
    private const float laderFloorDropDistance = .1f;
    private Vector2 lastGrabLadderDirection;
    float _verticalVelocity;
    [SerializeField] LayerMask walkableLayer;

    void Update()
    {
        movementVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space))
            GetComponent<Rigidbody2D>().AddForce(Vector2.up * jumpForce);

        ControlLadderMovement();

        if (Input.GetKey(KeyCode.W) && Mathf.Abs(movementVector.x) > 0)
        {
            movementVector.x = transform.position.x + movementVector.x * speed * Time.deltaTime;
            movementVector.y = (_isClimbingLadder) ? transform.position.y + (speed) * _verticalVelocity * Time.deltaTime : transform.position.y;

            transform.Position2D(movementVector);
        }
        else if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
        {
            movementVector.x = transform.position.x + movementVector.x * speed * Time.deltaTime;
            movementVector.y = transform.position.y;

            transform.Position2D(movementVector);
        }
    }

    private void ControlLadderMovement()
    {
        #region Ladder Climbing
        float climbingStartPointX = transform.position.x + (transform.localScale.x / 2) * Mathf.Sign(movementVector.x);
        float climbingStartPointY = transform.position.y - ((transform.localScale.y / 2)) + avoidFloorDistance;

        Vector2 climbingStartPoint = Vector2.right * climbingStartPointX + Vector2.up * climbingStartPointY;

        if (!_isClimbingLadder)
        {
            Debug.DrawLine(climbingStartPoint, climbingStartPoint + movementVector * laderGrabDistance, Color.red);
            RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, climbingStartPoint + movementVector, laderGrabDistance, walkableLayer);
            if (hit && hit.collider.usedByEffector)
                GrabLadder(movementVector);
            //GetComponent<Rigidbody2D>().gravityScale = 0;
        }

        else
        {
            RaycastHit2D hit = Physics2D.Raycast(climbingStartPoint, lastGrabLadderDirection, laderGrabDistance, walkableLayer);

            if (hit && !hit.collider.usedByEffector)
            {
                DropLadder();
                _verticalVelocity = 1f;
            }
            else if (!hit)
            {
                DropLadder();
                _verticalVelocity = 1f;
            }

            if (Vector2.Dot(movementVector, lastGrabLadderDirection) < 0)
            {
                //GetComponent<Rigidbody2D>().gravityScale = 1;

                hit = Physics2D.Raycast(transform.Position2D(), Vector2.down, laderFloorDropDistance);
                if (hit) DropLadder();
            }

            if (_isClimbingLadder)
            {
                //movementVector = new Vector2(0, movementVector.y * 10);
                _verticalVelocity = 1f;
            }
        }
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
        // GetComponent<Rigidbody2D>().gravityScale = 1;

    }
}
