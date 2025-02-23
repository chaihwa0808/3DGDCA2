using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    // --- COMPONENTS ---
    private CharacterController controller;
    private Animator animator;
    private Enemy enemy;

    [Header(">> Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 360f;

    [Header(">> Jumping Settings")]
    public float jumpSpeed = 5f;
    public float jumpHorizontalSpeed = 3f;
    public float jumpButtonGracePeriod = 0.2f;

    private float ySpeed;
    private float originalStepOffset;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;

    private bool isJumping;
    private bool isFalling;
    private bool isLanding;
    private Vector3 lastVelocity;
    private float targetHeight;

    [Header(">> Stamina Settings")]
    public float maxStamina = 20f;
    public float stamina = 100f;
    public float staminaDepletionRate = 5f;  // Stamina lost per second when moving
    public float attackStaminaCost = 15f;    // Stamina lost per attack
    public float jumpStaminaCost = 10f;
    public float staminaRegenRate = 3f;      // Stamina regen per second when idle
    public Slider staminaBar;

    [Header(">> Player Markers")]
    public Transform feetMarker;
    public Transform headMarker;
    private float originalHeight;
    private Vector3 originalCenter;


    [Header(">> Combat Settings")]
    public float attackRange = 3f;
    public int damageDealt = 1;

    [Header(">> Player Configuration")]
    public Player player;
    public enum Player { P1, P2 };


    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        originalStepOffset = controller.stepOffset;

        // Store controller original height and center
        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    void Update()
    {
        // --- INPUT HANDLING ---
        float h = Input.GetAxis("Horizontal " + player.ToString());
        float v = Input.GetAxis("Vertical " + player.ToString());
        bool isCrouching = Input.GetButton("Crouch " + player.ToString());

        if (Input.GetButtonDown("Attack " + player.ToString()))
        {
            animator.SetTrigger("Attack");
        }

        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isInAir", true);
        animator.SetFloat("MoveX", h);
        animator.SetFloat("MoveY", v);

        float speed = isCrouching ? moveSpeed * 0.5f : moveSpeed;
        if (isLanding) speed *= 0.8f;

        // Slow down if stamina is low
        if (stamina <= 3f)
        {
            speed *= 0.5f; // Reduce speed by 50% when stamina is critically low
        }

        Vector3 move = transform.TransformDirection(new Vector3(h, 0, v).normalized) * speed;

        // --- STAMINA DEPLETION ---
        float moveMagnitude = new Vector3(h, 0, v).magnitude;

        if (moveMagnitude > 0.1f && controller.isGrounded)
        {
            stamina -= staminaDepletionRate * moveMagnitude * Time.deltaTime;
        }
        else
        {
            stamina += staminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        UpdateStaminaBar();

        // --- PLAYER ROTATION ---
        float r = Input.GetAxis("Mouse X " + player.ToString());
        transform.Rotate(0, r * rotationSpeed * Time.deltaTime, 0);

        // --- JUMP & FALL HANDLING ---
        if (controller.isGrounded)
        {
            if (isFalling)
            {
                StartCoroutine(HandleLanding());
                return;
            }

            lastGroundedTime = Time.time;
            isJumping = false;
            isFalling = false;
            isLanding = false;

            animator.SetBool("isGrounded", true);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);

            ySpeed = -0.5f; // Prevent instant falling

            if (jumpButtonPressedTime.HasValue && Time.time - jumpButtonPressedTime <= jumpButtonGracePeriod)
            {
                Jump();
            }
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;

            if (isJumping && ySpeed < 0)
            {
                isJumping = false;
                isFalling = true;
                animator.SetBool("isJumping", false);
                animator.SetBool("isFalling", true);
            }
            else if (!isJumping && ySpeed < -2 && !isFalling)
            {
                isFalling = true;
                animator.SetBool("isFalling", true);
            }
        }

        if (!controller.isGrounded)
        {
            lastVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        }

        if (Input.GetButtonDown("Jump " + player.ToString()))
        {
            jumpButtonPressedTime = Time.time;
        }

        Vector3 velocity = move * (controller.isGrounded ? 1 : jumpHorizontalSpeed);
        if (isLanding) velocity += lastVelocity * 0.5f;

        velocity.y = ySpeed;
        controller.Move(velocity * Time.deltaTime);

        UpdateControllerSize();
    }

    void Jump()
    {
        if (stamina < jumpStaminaCost) return; // Prevent jumping if stamina is too low

        stamina -= jumpStaminaCost; // Deduct stamina
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        UpdateStaminaBar(); // Update UI

        ySpeed = jumpSpeed;
        isJumping = true;
        isFalling = false;
        isLanding = false;

        animator.SetBool("isJumping", true);
        animator.SetBool("isFalling", false);
        animator.SetBool("isGrounded", false);

        jumpButtonPressedTime = null;
        lastGroundedTime = null;
    }

    private IEnumerator HandleLanding()
    {
        isFalling = false;
        isLanding = true;

        animator.SetBool("isFalling", false);
        animator.SetTrigger("Landing");

        float originalSpeed = moveSpeed;
        moveSpeed *= 0.5f;

        yield return new WaitForSeconds(0.3f); // Adjust timing to match animation

        isLanding = false;
        moveSpeed = originalSpeed;
    }

    void UpdateControllerSize()
    {
        if (animator.GetBool("isCrouching"))
        {
            targetHeight = 1.3f;
        }
        else if (animator.GetBool("isFalling") || animator.GetBool("isJumping"))
        {
            targetHeight = 1.5f;
        }
        else
        {
            targetHeight = originalHeight;
        }

        SmoothHeightTransition();
    }

     void SmoothHeightTransition()
    {
        controller.height = targetHeight;

        // Adjust center based on height difference
        float heightDifference = (controller.height - originalHeight) / 2;
        controller.center = originalCenter + new Vector3(0, heightDifference, 0);

        if (animator.GetBool("isJumping") || animator.GetBool("isFalling"))
        {
            controller.center += new Vector3(0, 0.4f, 0);
        }
    }

    public void DealDamageToEnemy()
    {
        if (stamina < attackStaminaCost) return; // Prevent attacking if stamina is too low

        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Vector3 playerPos = transform.position;

        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
        {
            float distance = Vector3.SqrMagnitude(enemy.transform.position - playerPos);

            if (distance < closestDistance && distance <= attackRange * attackRange)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            stamina -= attackStaminaCost;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);
            UpdateStaminaBar(); // Update UI

            closestEnemy.TakeDamage(damageDealt);
        }
    }

    void UpdateStaminaBar()
    {
        staminaBar.value = stamina / maxStamina;

        // Color transition: Green (High) , Orange (Mid) , Red (Low)
        Color staminaColor;

        if (stamina > maxStamina * 0.5f) // Above 50% = Green
            staminaColor = Color.yellow;
        else if (stamina > 3f) // Between 3 and 50% = Orange
            staminaColor = new Color(1f, 0.5f, 0f); // RGB for orange
        else // 0 to 3 = Red
            staminaColor = Color.red;

        staminaBar.fillRect.GetComponent<Image>().color = staminaColor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 1, 0), attackRange);
    }
}
