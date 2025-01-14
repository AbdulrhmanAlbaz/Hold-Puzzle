using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public enum ballColors
{
    red,blue,green,orange,purple
}

public class PlayerManager : Singleton<PlayerManager>
{
    bool isMoving;
    public bool inMoveAndPop ;
    [SerializeField] private bool isInTouch, shouldPop;
    private float startTime, duration;
    public int moveCounter;
    [SerializeField] private ballColors ballColors;
    public Vector3 startPos, targetPos;
    public Direction shootDirection;
    [SerializeField] private float speed = 8;
    public GridManager manager => GridManager.Instance;
    public Vector3 lastPos;
    public GameObject trail;

    Direction direction;
    SpriteRenderer spriteRenderer;
    int color;

    int splittedScreen = Screen.width / 3;

    void Start()
    {
        inMoveAndPop = false;

        ballColors = ballColors.red;

        moveCounter = 0;
        trail.gameObject.SetActive(false);
        direction = Direction.up;
        color = Random.Range(0, manager.sprites.Length);
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = manager.sprites[color];

        trail.GetComponent<TrailRenderer>().endColor = GridManager.Instance.colorIndex[color]; trail.GetComponent<TrailRenderer>().startColor = GridManager.Instance.colorIndex[color];
    }

    void Update() => PlayerController();

    public void PlayerController()
    {
        if (transform.position.x == -1 && transform.position.y < manager.gridSizeY) { direction = Direction.up; }
        else if (transform.position.x < manager.gridSizeX && transform.position.y == manager.gridSizeY) { direction = Direction.right; }
        else if (transform.position.x == manager.gridSizeX && transform.position.y > -1) { direction = Direction.down; }
        else { direction = Direction.left; }

        if (Time.time - startTime < duration)
        {
            if (Input.GetMouseButtonUp(0) && !inMoveAndPop)
                isInTouch = false; 
        }

        else if (Input.GetMouseButton(0))
        {
            isInTouch = true;
            if (!isMoving) MovePlayer();
        }
    } 

    private void MoveAndPop()
    {
        if (transform.position.x == -1 && transform.position.y == -1) { isMoving = false; return; }
        else if (transform.position.x == -1 && transform.position.y == manager.gridSizeY) { isMoving = false; return; }
        else if (transform.position.x == manager.gridSizeX && transform.position.y == -1) { isMoving = false; return; }
        else if (transform.position.x == manager.gridSizeX && transform.position.y == manager.gridSizeY) { isMoving = false; return; }

        if (inMoveAndPop == false) inMoveAndPop = true;
        moveCounter++;

        lastPos = new Vector3(transform.position.x,transform.position.y,transform.position.z);
        shootDirection = direction == Direction.up ? Direction.right : direction == Direction.right ? Direction.down : direction == Direction.left ? Direction.up : Direction.left;
        ShootPlayer();
    }

    private void MovePlayer()
    {
        isMoving = true;
        trail.gameObject.SetActive(false);
        
        Vector3 moveDirection = direction == Direction.up ? Vector3.up : direction == Direction.right ? Vector3.right : direction == Direction.down ? Vector3.down : Vector3.left;

        startPos = transform.position;
        targetPos = startPos + moveDirection;
        float distance = Vector3.Magnitude(targetPos - startPos);

        duration = distance / speed;
        startTime = Time.time;

        transform.DOMove(targetPos, distance / speed).SetEase(Ease.Linear).OnComplete(() =>
        {
            if (!Input.GetMouseButton(0)) MoveAndPop();
            else isMoving = false;
        });
    }

    private void ShootPlayer()
    {
        inMoveAndPop = false;
        trail.gameObject.SetActive(true);
        isMoving = true;
        startPos = transform.position;
       
        Vector3 shootDirVector = shootDirection == Direction.up ? Vector3.up : shootDirection == Direction.right ? Vector3.right : shootDirection == Direction.down ? Vector3.down : Vector3.left;
        var targetBall = GetTargetBall();

        if (targetBall == null)
        { 
            SoundManager.Play(AudioClips.move);
            transform.DOMove(startPos + shootDirVector * (manager.gridSizeX + 1), manager.gridSizeX / speed).SetEase(Ease.Linear).OnComplete(() =>
            {
                isMoving = false;
                trail.gameObject.SetActive(false);

            });

            return;
        }

        if (targetBall.color != color)
        {  
            float distanceTarget = Vector3.Magnitude(targetBall.transform.position - startPos);
            trail.transform.DOScale(0.5f, 0.2f).SetEase(Ease.InBounce);

            AnimateSquish(targetBall, distanceTarget, shootDirection);
            AnimateBouncingBall(targetBall);
            return;
        }

        Ball distantBall = targetBall;
        manager.ballsToDestroy.Clear();
        manager.CheckBalls(targetBall);


        while (targetBall != null)
        {
            targetBall = GetTargetBall();
            if (targetBall == null) break;

            if (targetBall.color != color) break;
            manager.CheckBalls(targetBall);
        }

        foreach (Ball ball in manager.ballsToDestroy)
        {
            switch (shootDirection)
            {
                case Direction.up:
                    if (ball.x == distantBall.x & ball.y> distantBall.y) distantBall = ball;
                    break;

                case Direction.right:
                    if (ball.x > distantBall.x & ball.y == distantBall.y) distantBall = ball;
                    break;

                case Direction.down:
                    if (ball.x == distantBall.x & ball.y < distantBall.y) distantBall = ball;
                    break;

                case Direction.left:
                    if (ball.x < distantBall.x & ball.y == distantBall.y) distantBall = ball;
                    break;
            }
        }

        manager.DestroyBalls();

        SoundManager.Play(AudioClips.button);
        manager.winLevel();
        trail.gameObject.SetActive(true);
        float distance = Vector3.Magnitude(distantBall.transform.position - startPos);

        transform.DOMove(distantBall.transform.position, distance / speed).SetEase(Ease.Linear).OnComplete(() =>
        {
            if (BallShouldMoveForward(distantBall))
            {
                int dist = shootDirection == Direction.left ? manager.gridSizeX - distantBall.x : shootDirection == Direction.right ? distantBall.x : shootDirection == Direction.down ? manager.gridSizeY - distantBall.y : distantBall.y;

                    transform.DOMove(startPos + shootDirVector * (manager.gridSizeX + 1), manager.gridSizeX / (speed * dist)).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        trail.gameObject.SetActive(false);
                        manager.DestroyBalls();
                        isMoving = false;
                    });
            }

            else
            {
                transform.DOMove(startPos, distance / speed).SetEase(Ease.Linear).OnComplete(() =>
                {
                    trail.gameObject.SetActive(false);
                    isMoving = false;
                    changeColor();
                });
            }
        }); 
    }

    private bool BallShouldMoveForward(Ball ball)
    {
      
        switch (shootDirection)
        {
            case Direction.right:

                for (int i = ball.x + 1; i < manager.gridSizeX; i++)
                {
                    Ball b = manager.ballGrid[i, (int)transform.position.y];
                    if (b == null || b.color == color) continue;

                    return false;
                }
                break;

            case Direction.left:

                for (int i = ball.x - 1; i >= 0; i--)
                {
                    Ball b = manager.ballGrid[i, (int)transform.position.y];
                    if (b == null || b.color == color) continue;
 
                    return false;
                }
                break;

            case Direction.up:

                for (int i = ball.y + 1; i < manager.gridSizeY; i++)
                {
                    Ball b = manager.ballGrid[(int)transform.position.x, i];
                    if (b == null || b.color == color) continue;
                   
                    return false;
                }
                break;

            case Direction.down:

                for (int i = ball.y - 1; i >= 0; i--)
                {
                    Ball b = manager.ballGrid[(int)transform.position.x, i];
                    if (b == null || b.color == color) continue;
                   
                    return false;
                }
                break;
        }
        return true;
    }

    private Ball GetTargetBall()
    {       
        switch (shootDirection)
        {
            case Direction.up:
             
                for (int y = 0; y < manager.gridSizeY; y++)
                {
                    Ball ball = manager.ballGrid[(int)transform.position.x, y];
                    if (ball != null && !manager.ballsToDestroy.Contains(ball))
                        
                        return ball;  
                }
     
                break;

            case Direction.right:         
                for (int x = 0; x < manager.gridSizeX; x++)
                {
                    Ball ball = manager.ballGrid[x, (int)transform.position.y];
                    if (ball != null && !manager.ballsToDestroy.Contains(ball))
                       
                    return ball;
                }
                break;

            case Direction.down:             
                for (int y = manager.gridSizeY - 1; y >= 0; y--)
                {
                    Ball ball = manager.ballGrid[(int)transform.position.x, y];
                    if (ball != null && !manager.ballsToDestroy.Contains(ball))
                       
                    return ball;
                  
                }
                break;

            case Direction.left:           
                for (int x = manager.gridSizeX - 1; x >= 0; x--)
                {
                    Ball ball = manager.ballGrid[x, (int)transform.position.y];
                    if (ball != null && !manager.ballsToDestroy.Contains(ball))
                        
                    return ball;
                }
                break;
        }
        return null;
    }


    public void FindBallsToDestroy(Ball ball)
    {
        manager.ballsToDestroy.Add(ball);

        foreach (Ball neighbor in ball.neighbors)
        {
            if (neighbor.color == ball.color & !manager.ballsToDestroy.Contains(neighbor))
            {
                FindBallsToDestroy(neighbor);
            }
        }
    }

    private async void changeColor()
    {
        color = Random.Range(0, 5);

        switch(color)
        {
            case 0:
                ballColors = ballColors.red;
                color = 0;
                break;

            case 1:
                ballColors = ballColors.green;
                color = 1;
                break;

            case 2:
                ballColors = ballColors.orange;
                color = 2;
                break;

            case 3:
                ballColors = ballColors.purple;
                color = 3;
                break;

            case 4:
                ballColors = ballColors.blue;
                color = 4;
                break;
        }
   
        await Task.Delay(100);
        spriteRenderer.sprite = manager.sprites[color];

        trail.GetComponent<TrailRenderer>().endColor = GridManager.Instance.colorIndex[color]; trail.GetComponent<TrailRenderer>().startColor = GridManager.Instance.colorIndex[color];
    }

    public void AnimateBouncingBall(Ball ball)
    {
        int ballDist = shootDirection == Direction.left ? manager.gridSizeX - ball.x: shootDirection == Direction.right ? ball.x : shootDirection == Direction.down ? manager.gridSizeY - ball.y : ball.y;

        ball.transform.DOScale(0.8f, ballDist / speed).SetDelay(ballDist / speed).SetEase(Ease.InBounce).OnComplete(() =>
        {
            ball.transform.DOScale(1f, 0.1f).SetEase(Ease.OutBounce);
        });
    }

    public void AnimateSquish(Ball targetBall, float distanceTarget, Direction direction)
    {
        Vector3 target = targetBall.color == color ? targetBall.transform.position : targetBall.transform.position + (shootDirection == Direction.left ? Vector3.right *0.75f : shootDirection == Direction.right ? Vector3.left * 0.75f : shootDirection == Direction.down ? Vector3.up * 0.75f : Vector3.down * 0.75f);

        transform.DOMove(target, distanceTarget / speed).SetEase(Ease.Linear).OnComplete(() =>
        {
            SoundManager.Play(AudioClips.noAvailableMove);
            transform.DOMove(startPos, distanceTarget / speed).SetEase(Ease.Linear).SetDelay(0.1f).OnComplete(() =>
            {
                transform.DOScale(1f, 0.2f).SetEase(Ease.OutBounce);
                isMoving = false;
                
                changeColor();
            });

            if (direction == Direction.up || direction == Direction.down)
            {
                transform.DOScaleY(0.8f, 1 / speed).SetEase(Ease.InBounce).OnComplete(() =>
                {
                    transform.DOScaleY(1f, 1 / speed).SetEase(Ease.InBounce);
                });

            }
            else
            {
                transform.DOScaleX(0.8f, 1 / speed).SetEase(Ease.InBounce).OnComplete(() =>
                {
                    transform.DOScaleX(1f, 1 / speed).SetEase(Ease.InBounce);
                });
            }
        });

        if (direction == Direction.up || direction == Direction.down)
        {
            transform.DOScaleX(0.8f, distanceTarget / speed).SetEase(Ease.InBounce);
            transform.DOScaleY(1.29f, distanceTarget / speed).SetEase(Ease.InBounce);
        }
        else
        {
            transform.DOScaleX(1.29f, distanceTarget / speed).SetEase(Ease.InBounce);
            transform.DOScaleY(0.8f, distanceTarget / speed).SetEase(Ease.InBounce);
        }
    }

}
