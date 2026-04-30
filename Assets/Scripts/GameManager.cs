
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private enum CurrentPlayer
    {
        Player1,
        Player2
    }

    [Header("Rules")]
    [SerializeField] private int maxFouls = 3;
    [SerializeField] private float shotTimer = 3f;
    [SerializeField] private float movementThreshold = 0.05f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI player1BallsText;
    [SerializeField] private TextMeshProUGUI player2BallsText;
    [SerializeField] private TextMeshProUGUI player1FoulText;
    [SerializeField] private TextMeshProUGUI player2FoulText;
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject restartButton;

    [Header("Cue Ball Reset")]
    [SerializeField] private Transform cueBallStartPosition;

    [Header("Cameras")]
    [SerializeField] private Camera cueStickCamera;
    [SerializeField] private Camera overheadCamera;

    [Header("XR Settings")]
    [SerializeField] private GameObject xrOrigin;

    private CurrentPlayer currentPlayer;
    private int player1BallsRemaining = 7;
    private int player2BallsRemaining = 7;
    private int player1Fouls = 0;
    private int player2Fouls = 0;

    //private bool isWinningShotForPlayer1 = false;
    //private bool isWinningShotForPlayer2 = false;
    private bool isWaitingForBallMovementToStop = false;
    private bool willSwapPlayers = false;
    private bool isGameOver = false;
    private bool ballPocketed = false;
    private bool foulRecordedThisShot = false;
    //private bool wrongBallPocketed = false;

    private float currentTimer;
    private Camera currentCamera;
    private Transform cueBall;

    private void Start()
    {
        currentPlayer = CurrentPlayer.Player1;
        currentTimer = shotTimer;
        currentCamera = cueStickCamera;

        if (cueStickCamera != null) cueStickCamera.enabled = true;
        if (overheadCamera != null) overheadCamera.enabled = false;

        if (restartButton != null) restartButton.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);

        currentTurnText.text = "Current Turn: Player 1";
        UpdateUI();
        FindCueBall();
    }

    private void FindCueBall()
    {
        cueBall = null;

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            Ball b = ball.GetComponent<Ball>();
            if (b != null && b.IsCueBall())
            {
                cueBall = ball.transform;
                break;
            }
        }
    }

    private void UpdateUI()
    {
        if (player1BallsText != null)
            player1BallsText.text = "Player 1 Balls Remaining: " + player1BallsRemaining;

        if (player2BallsText != null)
            player2BallsText.text = "Player 2 Balls Remaining: " + player2BallsRemaining;

        if (player1FoulText != null)
            player1FoulText.text = "Player 1 Fouls: " + player1Fouls + " / " + maxFouls;

        if (player2FoulText != null)
            player2FoulText.text = "Player 2 Fouls: " + player2Fouls + " / " + maxFouls;
    }

    public void BeginShot()
    {
        ballPocketed = false;
        foulRecordedThisShot = false;
    }

    private void Update()
    {

        if (isGameOver)
            return;

        if (isWaitingForBallMovementToStop)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer > 0f)
                return;

            bool allStopped = true;

            GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
            foreach (GameObject ball in balls)
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb != null && rb.linearVelocity.magnitude >= movementThreshold)
                {
                    allStopped = false;
                    break;
                }
            }

            if (allStopped)
            {
                isWaitingForBallMovementToStop = false;

                if (willSwapPlayers || !ballPocketed)
                {
                    NextPlayerTurn();
                }
                else
                {
                    SwitchCameras();
                }

                currentTimer = shotTimer;
                ballPocketed = false;
            }
        }



    }

    public void SwitchCameras()
    {
        if (isGameOver)
            return;

        if (currentCamera == cueStickCamera)
        {
            if (cueStickCamera != null) cueStickCamera.enabled = false;
            if (overheadCamera != null) overheadCamera.enabled = true;

            currentCamera = overheadCamera;
            isWaitingForBallMovementToStop = true;
        }
        else
        {
            if (cueStickCamera != null) cueStickCamera.enabled = true;
            if (overheadCamera != null) overheadCamera.enabled = false;

            currentCamera = cueStickCamera;

            CameraController controller = currentCamera.GetComponent<CameraController>();
            if (controller != null)
                controller.ResetCamera();

            foulRecordedThisShot = false;
        }
    }

    public void RestartTheGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(2f);

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }
    private void RegisterFoulForCurrentPlayer(string foulMessage)
    {
        if (isGameOver || foulRecordedThisShot)
            return;

        foulRecordedThisShot = true;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = foulMessage;
            StartCoroutine(HideMessage());
        }

        if (currentPlayer == CurrentPlayer.Player1)
        {
            player1Fouls++;
            if (player1Fouls >= maxFouls)
            {
                Lose("Player 1 lost the game after 3 fouls.");
                return;
            }
        }
        else
        {
            player2Fouls++;
            if (player2Fouls >= maxFouls)
            {
                Lose("Player 2 lost the game after 3 fouls.");
                return;
            }
        }

        UpdateUI();
    }

    private void ResetCueBallToStart()
    {
        if (cueBall == null)
            FindCueBall();

        if (cueBall == null || cueBallStartPosition == null)
            return;

        cueBall.position = cueBallStartPosition.position;
        cueBall.rotation = cueBallStartPosition.rotation;

        Rigidbody rb = cueBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            rb.WakeUp();
        }
    }

    private bool IsPlayer1Ball(Ball ball)
    {
        int number = ball.GetBallNumber();
        return number >= 1 && number <= 7;
    }

    private bool IsPlayer2Ball(Ball ball)
    {
        int number = ball.GetBallNumber();
        return number >= 9 && number <= 15;
    }

    private bool CheckBall(Ball ball)
    {
        if (ball == null)
            return true;

        if (ball.IsCueBall())
        {
            RegisterFoulForCurrentPlayer("Cue ball pocketed.");
            ResetCueBallToStart();
            ballPocketed = true;
            willSwapPlayers = true;
            return false;
        }

        if (ball.IsEightBall())
        {
            if (currentPlayer == CurrentPlayer.Player1)
            {
                if (player1BallsRemaining == 0)
                    Win("Player 1");
                else
                    Lose("Player 1 hit the 8-ball too early.");
            }
            else
            {
                if (player2BallsRemaining == 0)
                    Win("Player 2");
                else
                    Lose("Player 2 hit the 8-ball too early.");
            }

            return true;
        }
         
        if (currentPlayer == CurrentPlayer.Player1)
        {
            if (IsPlayer1Ball(ball))
            {
                player1BallsRemaining = Mathf.Max(0, player1BallsRemaining - 1);
                //if (player1BallsRemaining == 0)
                //    isWinningShotForPlayer1 = true;

                UpdateUI();
            }
            else if (IsPlayer2Ball(ball))
            {
                RegisterFoulForCurrentPlayer("Player 1 pocketed a Player 2 ball.");
                player2BallsRemaining = Mathf.Max(0, player2BallsRemaining - 1);
                UpdateUI();
                willSwapPlayers = true;
            }
        }
        else
        {
            if (IsPlayer2Ball(ball))
            {
                player2BallsRemaining = Mathf.Max(0, player2BallsRemaining - 1);
                //if (player2BallsRemaining == 0)
                //    isWinningShotForPlayer2 = true;

                UpdateUI();
            }
            else if (IsPlayer1Ball(ball))
            {
                RegisterFoulForCurrentPlayer("Player 2 pocketed a Player 1 ball.");
                player1BallsRemaining = Mathf.Max(0, player1BallsRemaining - 1);
                UpdateUI();
                willSwapPlayers = true;
            }
        }

        return true;
    }

    private void Lose(string message)
    {
        isGameOver = true;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = message;
        }

        if (restartButton != null)
            restartButton.SetActive(true);
    }

    private void Win(string player)
    {
        isGameOver = true;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = player + " has won!";
        }

        if (restartButton != null)
            restartButton.SetActive(true);
    }

    private void NextPlayerTurn()
    {
        if (currentPlayer == CurrentPlayer.Player1)
        {
            currentPlayer = CurrentPlayer.Player2;
            if (currentTurnText != null)
                currentTurnText.text = "Current Turn: Player 2";
        }
        else
        {
            currentPlayer = CurrentPlayer.Player1;
            if (currentTurnText != null)
                currentTurnText.text = "Current Turn: Player 1";
        }


        willSwapPlayers = false;
        foulRecordedThisShot = false;
        SwitchCameras();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isGameOver)
            return;

        if (other.CompareTag("Ball"))
        {
            Ball ball = other.GetComponent<Ball>();
            if (ball == null)
                return;

            ballPocketed = true;

            if (CheckBall(ball))
            {
                if (!ball.IsCueBall())
                    Destroy(other.gameObject);
            }
        }
    }
}