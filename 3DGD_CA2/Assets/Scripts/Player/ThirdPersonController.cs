using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    // --- COMPONENTS ---
    private CharacterController controller;
    private AudioSource audioSource;
    public Animator animator;

    // --- PLAYER CONFIGURATION ---
    public Player player;
    public int playerID;
    public GameObject characterModel;
    public SkinnedMeshRenderer characterRenderer;
    public string otherPlayer;

    // --- MOVEMENT SETTINGS ---
    [Header(">> Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 360f;
    private Vector3 lastMovementDirection = Vector3.zero;

    // --- JUMPING SETTINGS ---
    [Header(">> Jumping Settings")]
    public float jumpSpeed = 5f;
    public float jumpHorizontalSpeed = 3f;
    public float jumpButtonGracePeriod = 0.2f;
    public bool canMoveInAir = false;

    // --- STAMINA SETTINGS ---
    [Header(">> Stamina Settings")]
    public float maxStamina = 20f;
    public float stamina = 100f;
    public float staminaDepletionRate = 5f;
    public float attackStaminaCost = 15f;
    public float jumpStaminaCost = 10f;
    public float staminaRegenRate = 3f;
    public Slider staminaBar;

    // --- MUSHROOM SETTINGS ---
    [Header(">> Mushroom Settings")]
    public int mushroomCount = 0;
    public int maxMushrooms = 3;
    public GameObject mushroomInHand;
    public GameObject thrownMushroomPrefab;
    public Transform throwOrigin;
    public TextMeshProUGUI mushroomText;
    public bool isMushroomSelected;

    // --- COMBAT SETTINGS ---
    [Header(">> Combat Settings")]
    public float attackRange = 3f;
    public int damageDealt = 2;
    public int killCount = 0;
    public TextMeshProUGUI killCountText;

    // --- KNOCKBACK, CHECKPOINT, RESPWN ---
    [Header(">> Knockback, Checkpoint, Respawn")]
    private Vector3 lastCheckpointPosition;
    private bool isKnockedBack = false;

    // --- JUMPING AND FALLING ---
    private float ySpeed;
    private float originalStepOffset;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;
    private bool isJumping;
    private bool isFalling;
    private bool isLanding;
    private Vector3 lastVelocity;
    private float targetHeight;

    // --- PLAYER MARKERS ---
    [Header(">> Player Markers")]
    public Transform feetMarker;
    public Transform headMarker;
    private float originalHeight;
    private Vector3 originalCenter;

    // --- SFX ---
    [Header(">> SFX")]
    public AudioClip punchSound;
    public AudioClip throwSound;

    void Start()
    {
        InitializeComponents();
        StoreOriginalValues();
    }

    void Update()
    {
        UpdateMushroomUI();
        HandleInput();
        HandleMovement();
        HandleJumpAndFall();
        UpdateControllerSize();
    }

    // --- INITIALIZATION ---
    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    private void StoreOriginalValues()
    {
        originalStepOffset = controller.stepOffset;
        lastCheckpointPosition = transform.position;
        originalHeight = controller.height;
        originalCenter = controller.center;
        characterRenderer = characterModel.GetComponent<SkinnedMeshRenderer>();
    }

    // --- INPUT HANDLING ---
    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal " + player.ToString());
        float v = Input.GetAxis("Vertical " + player.ToString());
        bool isCrouching = Input.GetButton("Crouch " + player.ToString());

        if (Input.GetButtonDown("Attack " + player.ToString()))
        {
            animator.SetTrigger("Attack");
        }

        animator.SetBool("isCrouching", isCrouching);
        animator.SetFloat("MoveX", h);
        animator.SetFloat("MoveY", v);

        HandleStamina(h, v);
        HandleRotation();
        HandleJumpInput();
        HandleMushroomInput();
    }

    private void HandleStamina(float h, float v)
    {
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
    }

    private void HandleRotation()
    {
        float r = Input.GetAxis("Mouse X " + player.ToString());
        transform.Rotate(0, r * rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump " + player.ToString()))
        {
            jumpButtonPressedTime = Time.time;
        }
    }

    private void HandleMushroomInput()
    {
        if (Input.GetButtonDown("MushroomSelection " + player.ToString()))
        {
            MushroomSelectionToggle();
        }

        if (Input.GetButtonDown("Throw " + player.ToString()))
        {
            if (mushroomCount >= 3 && mushroomInHand.activeSelf && otherPlayer != null)
            {
                animator.SetTrigger("Throw");
            }
            else
            {
                Debug.Log("Not enough mushrooms");
            }
        }
    }

    // --- MOVEMENT HANDLING ---
    private void HandleMovement()
    {
        Vector3 move = Vector3.zero;
        float speed = animator.GetBool("isCrouching") ? moveSpeed * 0.5f : moveSpeed;
        if (isLanding) speed *= 0.8f;

        if (controller.isGrounded || canMoveInAir)
        {
            float h = Input.GetAxis("Horizontal " + player.ToString());
            float v = Input.GetAxis("Vertical " + player.ToString());
            move = transform.TransformDirection(new Vector3(h, 0, v).normalized) * speed;
        }

        if (InTakeHitAnimaton())
        {
            controller.Move(Vector3.zero);
            return;
        }

        Vector3 velocity = move * (controller.isGrounded ? 1 : jumpHorizontalSpeed);
        velocity.y = ySpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    // --- JUMPING AND FALLING HANDLING ---
    private void HandleJumpAndFall()
    {
        if (controller.isGrounded)
        {
            HandleGroundedState();
        }
        else
        {
            HandleAirState();
        }
    }

    private void HandleGroundedState()
    {
        if (isFalling)
        {
            StartCoroutine(HandleLanding());
            return;
        }

        ySpeed = 0f;
        lastGroundedTime = Time.time;
        isJumping = false;
        isFalling = false;
        isLanding = false;

        canMoveInAir = (Input.GetAxis("Horizontal " + player.ToString()) != 0 ||
                        Input.GetAxis("Vertical " + player.ToString()) != 0);

        animator.SetBool("isGrounded", true);
        animator.SetBool("isJumping", false);
        animator.SetBool("isFalling", false);

        ySpeed = -0.5f; // Prevent instant falling

        if (jumpButtonPressedTime.HasValue && Time.time - jumpButtonPressedTime <= jumpButtonGracePeriod)
        {
            Jump();
        }
    }

    private void HandleAirState()
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

        if (!controller.isGrounded)
        {
            lastVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        }
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

    // --- COLLISION HANDLING ---
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            HandleObstacleCollision();
        }
    }

    private void HandleObstacleCollision()
    {
        if (lastMovementDirection == Vector3.zero)
        {
            lastMovementDirection = -transform.forward; // Default to moving backward if no input
        }

        Vector3 knockbackDirection = -lastMovementDirection.normalized;
        knockbackDirection.y = 0;

        float knockbackStrength = 3f; // The strength of the knockback
        float blinkDuration = 0.5f;  // How long the player should blink

        KnockbackAndBlink(knockbackDirection, knockbackStrength, blinkDuration);
    }

    // --- CHECKPOINT AND RESPWN ---
    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        lastCheckpointPosition = checkpointPosition;
    }

    public void RespawnAtCheckpoint()
    {
        ySpeed = 0f;
        controller.enabled = false;  // Temporarily disable the controller
        StopAllCoroutines();

        transform.position = lastCheckpointPosition + new Vector3(0, -0.4f, 0);
        controller.enabled = true;
    }

    public void KnockbackAndBlink(Vector3 knockbackDirection, float knockbackStrength, float blinkDuration)
    {
        StartCoroutine(HandleKnockbackAndBlink(knockbackDirection, knockbackStrength, blinkDuration));
    }

    private IEnumerator HandleKnockbackAndBlink(Vector3 knockbackDirection, float knockbackStrength, float blinkDuration)
    {
        isKnockedBack = true;

        float timer = 0;
        float knockbackDuration = 0.5f; // Duration of knockback

        while (timer < knockbackDuration)
        {
            Vector3 moveDirection = knockbackDirection * knockbackStrength * Time.deltaTime;
            controller.Move(moveDirection);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(BlinkEffect(blinkDuration));

        isKnockedBack = false;
        RespawnAtCheckpoint();
    }

    private IEnumerator BlinkEffect(float duration)
    {
        float blinkTime = 0;
        float toggleInterval = 0.3f;
        bool isVisible = true;

        while (blinkTime < duration)
        {
            if (blinkTime % toggleInterval < Time.deltaTime)
            {
                isVisible = !isVisible; // Toggle visibility
                SetPlayerVisibility(isVisible);
            }

            blinkTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        SetPlayerVisibility(true); // Ensure the character is visible again
    }

    private void SetPlayerVisibility(bool visible)
    {
        characterRenderer.enabled = visible;
    }

    // --- LANDING HANDLING ---
    private IEnumerator HandleLanding()
    {
        isFalling = false;
        isLanding = true;
        canMoveInAir = false; // Reset when landing

        animator.SetBool("isFalling", false);
        animator.SetTrigger("Landing");

        float originalSpeed = moveSpeed;
        moveSpeed *= 0.5f;

        yield return new WaitForSeconds(0.3f); // Adjust timing to match animation

        isLanding = false;
        moveSpeed = originalSpeed;
    }

    // --- CONTROLLER SIZE ADJUSTMENT ---
    void UpdateControllerSize()
    {
        if (animator.GetBool("isCrouching"))
        {
            targetHeight = 1.2f;
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

        float heightDifference = (controller.height - originalHeight) / 2;
        controller.center = originalCenter + new Vector3(0, heightDifference, 0);

        if (animator.GetBool("isJumping") || animator.GetBool("isFalling"))
        {
            controller.center += new Vector3(0, 0.4f, 0);
        }
    }

    // --- COMBAT LOGIC ---
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

            closestEnemy.TakeDamage(damageDealt, this);
        }
    }

    // --- STAMINA UI UPDATE ---
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

    // --- MUSHROOM LOGIC ---
    void MushroomSelectionToggle()
    {
        isMushroomSelected = !isMushroomSelected;
        mushroomInHand.SetActive(isMushroomSelected && mushroomCount >= maxMushrooms);
    }

    public void ThrowMushroom()
    {
        GameObject otherPlayerObject = GameObject.Find(otherPlayer);
        float throwRange = 30f; // Adjust as needed
        float distance = Vector3.Distance(transform.position, otherPlayerObject.transform.position);

        if (!isMushroomSelected || !mushroomInHand.activeSelf || mushroomCount < maxMushrooms || distance > throwRange)
        {
            Debug.Log("Cannot throw: No mushroom selected or not enough mushrooms.");
            return;
        }

        if (otherPlayerObject == null)
        {
            Debug.Log("Other player not found.");
            return;
        }

        // Create the mushroom and start the arc movement
        GameObject thrownMushroom = Instantiate(thrownMushroomPrefab, throwOrigin.position, Quaternion.identity);
        StartCoroutine(MoveMushroomInArc(thrownMushroom, otherPlayerObject.transform));

        // Reset mushroom selection
        isMushroomSelected = false;
        mushroomInHand.SetActive(false);
        mushroomCount = 0;
    }

    public void CollectMushroom()
    {
        if (mushroomCount < maxMushrooms)
        {
            mushroomCount++;
            UpdateMushroomUI();
        }
    }

    void UpdateMushroomUI()
    {
        mushroomText.text = "Mushrooms: " + mushroomCount + " / 3";
    }

    private IEnumerator MoveMushroomInArc(GameObject mushroom, Transform targetTransform)
    {
        float duration = 0.75f; // Time taken for the throw
        float elapsedTime = 0f;

        Vector3 startPosition = mushroom.transform.position;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Get updated target position
            Vector3 updatedPosition = targetTransform.position;
            Vector3 midPoint = (startPosition + updatedPosition) / 2 + Vector3.up * 2f; // Adjust arc height

            // Quadratic Bezier Curve: Interpolating between start, mid, and target
            Vector3 currentPosition = Vector3.Lerp(Vector3.Lerp(startPosition, midPoint, t), Vector3.Lerp(midPoint, updatedPosition, t), t);
            mushroom.transform.position = currentPosition;

            yield return null;
        }

        // Ensure mushroom reaches the exact final position
        mushroom.transform.position = targetTransform.position;

        ThirdPersonController targetPlayer = targetTransform.GetComponent<ThirdPersonController>();
        Animator targetAnimator = targetPlayer.GetComponent<Animator>();
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("TakeHit");
        }

        if (targetPlayer != null)
        {
            targetPlayer.ApplySlowEffect(5f);
            SetMushroomInvisible(mushroom);
        }

        // Destroy mushroom after applying effect
        Destroy(mushroom);
    }

    void SetMushroomInvisible(GameObject mushroom)
    {
        mushroomInHand.SetActive(false);
    }

    public void ApplySlowEffect(float duration)
    {
        StartCoroutine(SlowEffectCoroutine(duration));
    }

    private IEnumerator SlowEffectCoroutine(float duration)
    {
        Debug.Log("SlowEffectStarted");
        float originalSpeed = moveSpeed;
        moveSpeed *= 0.2f;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed; // Restore speed
    }

    public void IncreaseKillCount()
    {
        killCount++;
        UpdateKillCountUI();
    }

    void UpdateKillCountUI()
    {
        if (killCountText != null)
        {
            killCountText.text = "Enemy Count: " + killCount;
        }
    }

    // --- GIZMOS ---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 1, 0), attackRange);
    }

    // --- ANIMATION CHECK ---
    bool InTakeHitAnimaton()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("Take hit");
    }

    // --- SFX PLAYBACK ---
    void PlayPunchSound()
    {
        audioSource.PlayOneShot(punchSound);
    }

    void PlayThrowSound()
    {
        audioSource.PlayOneShot(throwSound);
    }

    public enum Player { P1, P2 };
}