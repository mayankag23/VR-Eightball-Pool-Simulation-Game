using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private int ballNumber = 0; // 0 = cue ball, 1-15 = object balls
    private bool isCueBall = false;
    private bool isEightBall = false;

    private Rigidbody rb;
    private Renderer ballRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        ballRenderer = GetComponent<Renderer>();
        if (ballRenderer == null)
        {
            ballRenderer = GetComponentInChildren<Renderer>();
        }
}

    private void Start()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            if (transform.position.y > 0.1f)
                return;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }
    }

    public int GetBallNumber()
    {
        return ballNumber;
    }

    public bool IsCueBall()
    {
        return isCueBall;
    }

    public bool IsEightBall()
    {
        return isEightBall || ballNumber == 8;
    }

    public void SetBallNumber(int number)
    {
        ballNumber = number;
        isCueBall = false;
        isEightBall = number == 8;
    }

    public void MakeCueBall()
    {
        isCueBall = true;
        isEightBall = false;
        ballNumber = 0;
    }

    public void MakeEightBall()
    {
        isEightBall = true;
        isCueBall = false;
        ballNumber = 8;
    }

    public void ApplyTexture(Texture2D texture)
    {
        Debug.Log("Applying texture to ball: " + gameObject.name);

        if (ballRenderer == null)
            ballRenderer = GetComponentInChildren<Renderer>();

        if (ballRenderer == null || texture == null)
            return;

        Material mat = ballRenderer.material;

        //  FORCE assign 
        mat.mainTexture = texture;

        // ALSO try URP explicitly
        mat.SetTexture("_BaseMap", texture);

        // Reset color tint
        mat.color = Color.white;
    }
}

