using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class GridManager : Singleton<GridManager>
{
    public int gridSizeX;
    public int gridSizeY;
    public GameObject playerPrefab;
    public GameObject ballPrefab;
    public GameObject tilePrefab;
    public Sprite[] sprites;  
    public Color[] colorIndex;
    public List<Ball> ballsToDestroy = new List<Ball>();
    public List<Ball> balls = new List<Ball>();
    public Vector3 orginSpawnPoint;
    public Tile[,] tileGrid;
    public Ball[,] ballGrid;
    public  int width;
    public int height;
    public int score;
   
    void Start()
    {
        IntPlayer();
        IntGrid();
        Camera.main.transform.position = new Vector3(transform.position.x + gridSizeX / 2, (transform.position.y + gridSizeY / 2), -10);
        Camera.main.orthographicSize = gridSizeX + (Screen.height / Screen.width > 1.77 ? 1.5f : 0);
    }

    public void IntPlayer()
    {
        width = gridSizeX;
        height = gridSizeY;
        tileGrid = new Tile[width, height];
        ballGrid = new Ball[width, height];
        GameObject playerObject = Instantiate(playerPrefab, orginSpawnPoint + new Vector3(-1, -1, 0), Quaternion.identity);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newTileGO = Instantiate(tilePrefab, orginSpawnPoint + new Vector3(x, y, 0), Quaternion.identity);
                newTileGO.transform.parent = transform;
                Tile newTile = newTileGO.GetComponent<Tile>();
                tileGrid[x, y] = newTile;
                newTile.x = x;
                newTile.y = y;

            }
        }
    }

    public void IntGrid()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                int color = Random.Range(0, sprites.Length);
                Ball cellBall = Instantiate(ballPrefab, tileGrid[x, y].transform).GetComponent<Ball>(); 
                cellBall.x = x;
                cellBall.y = y;
                cellBall.color = color;
                ballGrid[x, y] = cellBall;
                balls.Add(cellBall);
                //colorBalls.Add(cellBall.GetComponent<SpriteRenderer>());
            }
        }
    }

    public void CheckBalls(Ball ball)
    {
        FindBallsToDestroy(ball);  
    }

    public void FindBallsToDestroy(Ball ball)
    {
        ballsToDestroy.Add(ball);
        //colorBallsToDestroy.Add(ball.color);

        foreach (Ball neighbor in ball.neighbors)
        {
            if (neighbor.color == ball.color & !ballsToDestroy.Contains(neighbor))
            FindBallsToDestroy(neighbor); 
        }
    }

    public void DestroyBalls()
    {
        foreach (Ball ball in ballsToDestroy)
        {
            if (ball == null)
            {
                Debug.Log("ball is null");
                return; }

            else
            {
                score++;
                ball.Destroy(); }

            balls.Remove(ball);
           // colorBalls.Remove(ball.GetComponent<SpriteRenderer>());
        }
    }

    public async void winLevel()
    {  if (balls.Count == 0)
        {
            Debug.Log("You Win");
            SoundManager.Play(AudioClips.victory);
            await Task.Delay(2300); 
            UIManager.Instance.setTransition(Views.leaderboard);
            UIManager.Instance.topPanel.SetActive(false);
            gameObject.SetActive(false);
            PlayerManager.Instance.gameObject.SetActive(false); }
    }
}
