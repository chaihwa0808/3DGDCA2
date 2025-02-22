using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    CharacterController controller;
    Animator animator;

    public float moveSpeed = 7f;
    public float rotationSpeed = 360f;
    public float jumpSpeed = 5f; // Increase this to jump higher
    public float jumpHorizontalSpeed = 3f;
    public float jumpButtonGracePeriod = 0.2f;

    private float ySpeed;
    private float originalStepOffset;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;
    private bool isJumping;
    private bool isGrounded;
    private bool isLanding;

    public enum Player { P1, P2 };
    public Player player;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        originalStepOffset = controller.stepOffset;

        // Initialize Animator parameters to prevent unwanted animations at start
        animator.SetBool("isGrounded", true);
        animator.SetBool("isJumping", false);
        animator.SetBool("isFalling", false);
        animator.SetBool("isLanding", false);
        animator.SetBool("isCrouching", false);

        // Ensure proper starting state
        isGrounded = true;
        isJumping = false;
        isLanding = false;
        ySpeed = -0.5f; // Small downward force to prevent floating on start
    }

    void Update()
    {
        // Get movement input
        float h = Input.GetAxis("Horizontal " + player.ToString());
        float v = Input.GetAxis("Vertical " + player.ToString());
        bool isCrouching = Input.GetKey(KeyCode.C) || Input.GetButton("Crouch");

        animator.SetBool("isCrouching", isCrouching);
        animator.SetFloat("MoveX", h);
        animator.SetFloat("MoveY", v);

        float speed = isCrouching ? moveSpeed * 0.5f : moveSpeed;
        Vector3 move = transform.TransformDirection(new Vector3(h, 0, v).normalized) * speed;

        // Player Rotation
        float r = Input.GetAxis("Mouse X " + player.ToString());
        transform.Rotate(0, r * rotationSpeed * Time.deltaTime, 0);

        // Handle Gravity & Landing
        if (controller.isGrounded)
        {
            // remember when player was last on the ground
            lastGroundedTime = Time.time;
            isGrounded = true;
            isJumping = false;
            animator.SetBool("isGrounded", true);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            ySpeed = -0.5f; // Prevent instant falling
        }
        else
        {
            // Apply gravity if not grounded
            isGrounded = false;
            ySpeed += Physics.gravity.y * Time.deltaTime; 
        }

        // Jumping Logic
        if (Input.GetButtonDown("Jump"))
        {
            jumpButtonPressedTime = Time.time;
        }

        // Was grounded recently
        if (Time.time - lastGroundedTime <= jumpButtonGracePeriod) 
        {
            controller.stepOffset = originalStepOffset;
            ySpeed = -0.5f; // Reset slight gravity impact
            animator.SetBool("isJumping", false);
            animator.SetBool("isLanding", false);
            animator.SetBool("isGrounded", true);

            if (Time.time - jumpButtonPressedTime <= jumpButtonGracePeriod)
            {
                // Apply upward force
                ySpeed = jumpSpeed; 
                animator.SetBool("isJumping", true);
                isJumping = true;
                isGrounded = false;
                isLanding = false;

                jumpButtonPressedTime = null;
                lastGroundedTime = null;
            }
        }
        else
        {
            controller.stepOffset = 0;
            animator.SetBool("isGrounded", false);
            isGrounded = false;

            // Falling Logic (Jumping > Falling)
            if (!isGrounded && ySpeed < -2)
            {
                isJumping = false;
                animator.SetBool("isJumping", false);
                animator.SetBool("isFalling", true);
            }
        }

        // Movement > Falling (Unexpected Drop)
        if (!isGrounded && !isJumping)
        {
            animator.SetBool("isFalling", true);
        }

        // Apply movement (Handle horizontal air movement)
        Vector3 velocity = move * (isGrounded ? 1 : jumpHorizontalSpeed);
        velocity.y = ySpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator ResetLanding()
    {
        yield return new WaitForSeconds(0.2f);
        animator.SetBool("isLanding", false);
        isLanding = false; // Landing > Movement transition
    }
}
