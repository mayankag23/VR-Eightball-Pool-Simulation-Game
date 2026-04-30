using System.Collections.Generic;
using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform cueBallPosition;
    [SerializeField] private Transform headBallPosition;

    [Header("Textures")]
    [SerializeField] private Texture2D cueBallTexture;
    [SerializeField] private Texture2D[] objectBallTextures = new Texture2D[15];

    private void Awake()
    {
        if (GameObject.FindGameObjectsWithTag("Ball").Length > 0)
           return;

            PlaceAllBalls();
    }

    private void PlaceAllBalls()
    {
        PlaceCueBall();
        PlaceRandomBalls();
    }

    private void PlaceCueBall()
    {
        GameObject ball = Instantiate(ballPrefab, cueBallPosition.position, Quaternion.identity);
        Ball b = ball.GetComponent<Ball>();
        b.MakeCueBall();
        b.ApplyTexture(cueBallTexture);
    }

    private void PlaceRandomBalls()
    {
        float scale = ballPrefab.transform.localScale.x;
        float radius = ballPrefab.GetComponent<SphereCollider>().radius * scale;
        float diameter = radius * 2f;
        float spacing = diameter * 1.05f;

        Vector3 origin = headBallPosition.position;

        List<int> ballNumbers = new List<int>();
        for (int i = 1; i <= 15; i++)
        {
            if (i != 8)
                ballNumbers.Add(i);
        }

        for (int i = 0; i < ballNumbers.Count; i++)
        {
            int r = Random.Range(i, ballNumbers.Count);
            (ballNumbers[i], ballNumbers[r]) = (ballNumbers[r], ballNumbers[i]);
        }

        int index = 0;

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                float x = (col - row * 0.5f) * spacing;
                float z = -row * spacing * 0.866f;

                Vector3 rotatedOffset = new Vector3(-z, 0, x);
                Vector3 spawnPos = origin + rotatedOffset;

                GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
                Ball b = ball.GetComponent<Ball>();

                if (row == 2 && col == 1)
                {
                    b.MakeEightBall();
                    if (objectBallTextures.Length > 7 && objectBallTextures[7] != null)
                        b.ApplyTexture(objectBallTextures[7]);
                }
                else
                {
                    int ballNumber = ballNumbers[index];
                    b.SetBallNumber(ballNumber);

                    int textureIndex = ballNumber - 1;
                    if (textureIndex >= 0 && textureIndex < objectBallTextures.Length)
                        b.ApplyTexture(objectBallTextures[textureIndex]);

                    index++;
                }

                Rigidbody rb = ball.GetComponent<Rigidbody>();
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }
    }
}
