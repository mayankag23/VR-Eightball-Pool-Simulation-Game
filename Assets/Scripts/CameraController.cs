using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float downAngle;
    [SerializeField] private float power;
    [SerializeField] private GameObject cueStick;
    [SerializeField] private float maxDrawDistance;
    [SerializeField] private TextMeshProUGUI powerText;

    [Header("VR")]
    // Assign your XR Origin in the inspector! This is required to place the VR player correctly.
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform rightHandController;
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] public bool isVRMode = false;
    //[SerializeField] private Vector3 stickOffset;
    [SerializeField] private Vector3 stickOffset = new Vector3(0, 0, 0.3f);

    private float horizontalInput;
    private bool isTakingShot = false;
    private float savedMousePosition = 0f;

    private Transform cueball;
    private GameManager gameManager;
    private Camera cam;

    private TrackedPoseDriver trackedPoseDriver;

    private void OnEnable()
    {
        if (triggerAction != null && triggerAction.action != null)
            triggerAction.action.Enable();
    }

    private void OnDisable()
    {
        if (triggerAction != null && triggerAction.action != null)
            triggerAction.action.Disable();
    }

    private void Start()
    {
        cam = Camera.main;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        trackedPoseDriver = GetComponent<TrackedPoseDriver>();

        if (isVRMode && xrOrigin != null)
        {
            xrOrigin.localScale = Vector3.one * 100f;
        }
        if (trackedPoseDriver != null)
            trackedPoseDriver.enabled = isVRMode;

        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("Ball"))
        {
            Ball b = ball.GetComponent<Ball>();
            if (b != null && b.IsCueBall())
            {
                cueball = ball.transform;
                break;
            }
        }

        ResetCamera();
    }

    private void Update()
    {
      
        // Draws a blue line for the camera's forward direction
        Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);

        // Draws a red line for the right hand controller's forward direction
        if (rightHandController != null)
        {
            Debug.DrawRay(rightHandController.position, rightHandController.forward * 5f, Color.red);
        }
        

        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isVRMode = !isVRMode;
            if (trackedPoseDriver != null)
                trackedPoseDriver.enabled = isVRMode;

            Debug.Log("VR Mode: " + isVRMode);
            ResetCamera(); // Re-align player when switching modes
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ResetCamera();
        }

        // Stick logic
        if (isVRMode && rightHandController != null && cueStick != null)
        {
            //cueStick.transform.position = rightHandController.position;
            //cueStick.transform.rotation = rightHandController.rotation;
            rightHandController.position = cueStick.transform.position;
            rightHandController.rotation = cueStick.transform.rotation;
        }

        // PC Rotation logic
        if (cueball != null && !isTakingShot && !isVRMode && Mouse.current != null)
        {
            horizontalInput = Mouse.current.delta.ReadValue().x * rotationSpeed * 0.02f;
            transform.RotateAround(cueball.position, Vector3.up, horizontalInput);
        }

        Shoot();
    }

    public void ResetCamera()
    {
        if (cueball == null || cueStick == null)
            return;

        isTakingShot = false;
        savedMousePosition = 0f;
        cueStick.SetActive(true);

        // Calculate exactly where the camera SHOULD be in world space
        Vector3 targetWorldPos = cueball.position + offset;
        Quaternion targetWorldRot = Quaternion.LookRotation(cueball.position - targetWorldPos);
        targetWorldRot = Quaternion.Euler(downAngle, targetWorldRot.eulerAngles.y, 0f);

        if (!isVRMode)
        {
            // PC MODE: Move the camera directly.
            transform.position = targetWorldPos;
            transform.rotation = targetWorldRot;
        }
        else
        {
            // VR MODE: Move the XR Origin, NOT the camera.
            // The TrackedPoseDriver controls the camera now. We move the "room" (XR Origin) 
            // so that the headset inside the room ends up exactly at the targetWorldPos.
            if (xrOrigin != null)
            {
                // Align rotation first so localPosition calculations are accurate
                float yRotationOffset = targetWorldRot.eulerAngles.y - transform.localEulerAngles.y;
                xrOrigin.rotation = Quaternion.Euler(0, yRotationOffset, 0);

                // Teleport the XR Origin based on the camera's local offset
                xrOrigin.position = targetWorldPos - transform.localPosition;
                //xrOrigin.position = targetWorldPos;
            }
        }

        if (powerText != null)
            powerText.text = "Power: 0%";
    }

    private void Shoot()
    {
        if (!isVRMode)
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame && !isTakingShot)
            {
                isTakingShot = true;
                savedMousePosition = 0f;
            }
            else if (isTakingShot)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    float mouseY = Mouse.current.delta.ReadValue().y;
                    //savedMousePosition = Mathf.Clamp(savedMousePosition + mouseY, maxDrawDistance, 0f);
                    float sensitivity = 0.04f; // adjust this
                    savedMousePosition = Mathf.Clamp(savedMousePosition + mouseY * sensitivity, maxDrawDistance, 0f);

                    float powerValue = Mathf.Abs(savedMousePosition) / Mathf.Abs(maxDrawDistance) * 100f;
                    if (powerText != null)
                        powerText.text = "Power: " + Mathf.RoundToInt(powerValue) + "%";
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    ShootBall(Mathf.Abs(savedMousePosition));
                }
            }
        }
        else
        {
           
            if (triggerAction == null || triggerAction.action == null) return;

            float triggerValue = triggerAction.action.ReadValue<float>();

            if (triggerValue > 0.1f && !isTakingShot)
            {
                isTakingShot = true;
            }

            if (isTakingShot)
            {
                // We only update the power IF the trigger is actively being pressed.
                if (triggerValue >= 0.1f)
                {
                    savedMousePosition = Mathf.Lerp(0f, maxDrawDistance, triggerValue);

                    float powerValue = Mathf.Abs(savedMousePosition) / Mathf.Abs(maxDrawDistance) * 100f;
                    if (powerText != null)
                        powerText.text = "Power: " + Mathf.RoundToInt(powerValue) + "%";
                }
                // If the trigger is released, we shoot BEFORE wiping out the savedMousePosition!
                else
                {
                    ShootBall(Mathf.Abs(savedMousePosition));
                    isTakingShot = false;
                }
            }
        }
    }

    private void ShootBall(float forceMultiplier)
    {
        if (cueball == null) return;

        Vector3 hitDirection;

        // Allow the right hand controller to dictate the shot direction in VR
        if (isVRMode && rightHandController != null)
        {
            hitDirection = rightHandController.forward;
        }
        else
        {
            hitDirection = transform.forward;
        }

        // Keep the ball on the table plane
        hitDirection = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;

        Rigidbody rb = cueball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(hitDirection * power * forceMultiplier, ForceMode.Impulse);
        }

        if (cueStick != null)
            cueStick.SetActive(false);

        if (gameManager != null)
        {
            gameManager.BeginShot();
            gameManager.SwitchCameras();
        }

        isTakingShot = false;
    }
}



