using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using static ThirdPersonController;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    // --- COMPONENTS ---
    private CharacterController controller;
    public Animator animator;
    private Enemy enemy;

    [Header(">> Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 360f;

    [Header(">> Jumping Settings")]
    public float jumpSpeed = 5f;
    public float jumpHorizontalSpeed = 3f;
    public float jumpButtonGracePeriod = 0.2f;
    public bool canMoveInAir = false;

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
    public float attackStaminaCost = 3f;    // Stamina lost per attack
    public float jumpStaminaCost = 5f;
    public float staminaRegenRate = 3f;      // Stamina regen per second when idle
    public Slider staminaBar;


    [Header(">> Mushroom Settings")]
    public int mushroomCount = 0;
    public int maxMushrooms = 3;
    public GameObject mushroomInHand;
    private bool isMushroomSelected = false;
    public GameObject thrownMushroomPrefab;
    public Transform throwOrigin;
    public float throwForce = 10f;
    public float upwardForce = 5f;
    public TextMeshProUGUI mushroomText;
    public string otherPlayer;
    private bool isStunned = false;


    [Header(">> Player Markers")]
    public Transform feetMarker;
    public Transform headMarker;
    private float originalHeight;
    private Vector3 originalCenter;

    [Header(">> Combat Settings")]
    public float attackRange = 3f;
    public int attackDamage = 2;
    public int killCount = 0;
    public TextMeshProUGUI killCountText;
    private List<Enemy> allEnemies;

    [Header(">> Knockback, Checkpoint, Respawn")]
    private Vector3 knockbackDirection;
    private bool isKnockedBack = false;
    private float knockbackDuration = 0.1f; // Duration of knockback
    private float knockbackSpeed = 5f;      // Speed of knockback
    private Vector3 lastCheckpointPosition;

    [Header(">> Player Configuration")]
    public Player player;
    public int playerID;
    public enum Player { P1, P2 };
    public GameObject characterModel;
    public SkinnedMeshRenderer characterRenderer;

    public AudioClip punchSound;
    public AudioClip throwSound;

    private AudioSource audioSource;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        originalStepOffset = controller.stepOffset;
        lastCheckpointPosition = transform.position;

        // Store controller original height and center
        originalHeight = controller.height;
        originalCenter = controller.center;

        characterRenderer = characterModel.GetComponent<SkinnedMeshRenderer>();
    }

    void Update()
    {
        UpdateMushroomUI();
        UpdateControllerSize();

        // --- INPUT HANDLING ---
        float h = Input.GetAxis("Horizontal " + player.ToString());
        float v = Input.GetAxis("Vertical " + player.ToString());

        animator.SetFloat("MoveX", h);
        animator.SetFloat("MoveY", v);
        animator.SetBool("isInAir", true);

        // Crouching 
        bool isCrouching = Input.GetButton("Crouch " + player.ToString()); // joystick b 0
        animator.SetBool("isCrouching", isCrouching);

        // Attack 
        if (Input.GetButtonDown("Attack " + player.ToString()) && stamina > attackStaminaCost) // joystick b 1
        {
            animator.SetTrigger("Attack");
        }

        // Jump 
        if (Input.GetButtonDown("Jump " + player.ToString())) // joystick b 3
        {
            jumpButtonPressedTime = Time.time;
        }

        // Mushroom selection 
        if (Input.GetButtonDown("MushroomSelection " + player.ToString()))
        {
            MushroomSelectionToggle();
        }

        // Mushroom throw
        if (Input.GetButtonDown("Throw " + player.ToString()))
        {
            if (mushroomCount >= 3 && isMushroomSelected && otherPlayer != null)
            {
                animator.SetTrigger("Throw");
                PlayThrowSound();
            }
            else
            {
                Debug.Log("not enough mushrooms");
            }
        }

        // If crouching or landing, lower speed
        float speed = isCrouching ? moveSpeed * 0.5f : moveSpeed;
        if (isLanding) speed *= 0.8f;

        // Slow down if stamina is low
        if (stamina <= 3f)
        {
            speed *= 0.5f; // Reduce speed by 50% when stamina is critically low
        }

        // If on ground, calculate movement
        Vector3 move = Vector3.zero;
        if (controller.isGrounded || canMoveInAir)
        {
            move = transform.TransformDirection(new Vector3(h, 0, v).normalized) * speed;
        }

        // Check if player is in stunned state
        if (InTakeHitAnimaton())
        {
            // Stop movement while stunned
            controller.Move(Vector3.zero);
            return;
        }

        // --- STAMINA DEPLETION ---
        float moveMagnitude = new Vector3(h, 0, v).magnitude;

        if (moveMagnitude > 0.1f && controller.isGrounded)
        {
            stamina -= staminaDepletionRate * moveMagnitude * Time.deltaTime;
        }
        else
        {
            // Recover stamina in idle state
            stamina += staminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        UpdateStaminaBar();

        // --- ROTATION ---
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

            // Only allow movement in air if the player was moving before jumping
            canMoveInAir = (h != 0 || v != 0);

            animator.SetBool("isGrounded", true);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);

            ySpeed = -0.5f;

            if (jumpButtonPressedTime.HasValue && Time.time - jumpButtonPressedTime <= jumpButtonGracePeriod)
            {
                Jump();
            }
        }
        else
        {
            // Apply gravity 
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

        // Get horizontal velocity for when player is off-ground
        if (!controller.isGrounded)
        {
            lastVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        }

        // Set speed based on isGrounded status
        // Smooth out landing process by adding velocity 
        Vector3 velocity = move * (controller.isGrounded ? 1 : jumpHorizontalSpeed);
        if (isLanding) velocity += lastVelocity * 0.5f;

        // Update gravity 
        // Applies final velocity to player
        velocity.y = ySpeed;
        controller.Move(velocity * Time.deltaTime);

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
        canMoveInAir = false; // Reset when landing

        animator.SetBool("isFalling", false);
        animator.SetTrigger("Landing");

        float originalSpeed = moveSpeed;
        moveSpeed *= 0.5f;

        yield return new WaitForSeconds(0.3f); // Adjust timing to match animation

        isLanding = false;
        moveSpeed = originalSpeed;
    }

    // --- HITTING OBSTACLE ---
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the player is hitting an obstacle
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            // Use the player's facing direction for knockback, but move backwards (opposite of forward)
            Vector3 knockbackDirection = -transform.forward;  // Inverse of the player's forward direction
            knockbackDirection.y = 0; // Ensure knockback stays on the XZ plane

            float knockbackStrength = 3f; // The strength of the knockback
            float knockbackDuration = 0.5f; // Duration of knockback

            Knockback(knockbackDirection, knockbackStrength, knockbackDuration);
        }
    }


    public void Knockback(Vector3 knockbackDirection, float knockbackStrength, float knockbackDuration)
    {
        if (isKnockedBack) return;
        StartCoroutine(HandleKnockback(knockbackDirection, knockbackStrength, knockbackDuration));
    }

    private IEnumerator HandleKnockback(Vector3 knockbackDirection, float knockbackStrength, float knockbackDuration)
    {
        if (!controller.enabled) yield break;

        isKnockedBack = true;

        // Store the original Y position
        float originalY = transform.position.y;

        // Apply knockback over time
        float timer = 0;
        while (timer < knockbackDuration)
        {
            // Calculate the knockback movement
            Vector3 moveDirection = knockbackDirection * knockbackStrength * Time.deltaTime;

            // Move the player
            controller.Move(moveDirection);

            timer += Time.deltaTime;
            yield return null;
        }

        // After knockback, respawn player at the last checkpoint
        RespawnAtCheckpoint();

        isKnockedBack = false;
    }


    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        lastCheckpointPosition = checkpointPosition;
    }

    // --- RESPAWN ---
    public void RespawnAtCheckpoint()
    {
        // Disable any movement or physics
        controller.enabled = false;
        StopCoroutine(nameof(HandleKnockback));

        transform.position = lastCheckpointPosition + new Vector3 (0,1,0);
        controller.enabled = true;
        moveSpeed = 7f;
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

            closestEnemy.TakeDamage(attackDamage, this);
        }
    }



    // --- STAMINA BAR ----
    void UpdateStaminaBar()
    {
        staminaBar.value = stamina / maxStamina;

        // Color transition: Green (High) , Orange (Mid) , Red (Low)
        Color staminaColor;

        if (stamina > maxStamina * 0.5f) 
            staminaColor = Color.yellow;
        else if (stamina > 3f)
            staminaColor = new Color(1f, 0.5f, 0f); 
        else 
            staminaColor = Color.red;

        staminaBar.fillRect.GetComponent<Image>().color = staminaColor;
    }

    // --- CHARACTER CONTROLLER SIZE ----
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

        // Adjust center based on height difference
        float heightDifference = (controller.height - originalHeight) / 2;
        controller.center = originalCenter + new Vector3(0, heightDifference, 0);

        if (animator.GetBool("isJumping") || animator.GetBool("isFalling"))
        {
            controller.center += new Vector3(0, 0.4f, 0);
        }
    }

    // --- MUSHROOM SETTINGS ---
    // Press g or right trigger to toggle mushroom
    void MushroomSelectionToggle()
    {
        if (isMushroomSelected)
        {
            isMushroomSelected = false;
            mushroomInHand.SetActive(false);
        }
        else if (mushroomCount >= maxMushrooms)
        {
            isMushroomSelected = true;
            mushroomInHand.SetActive(true);
        }
    }

    public void ThrowMushroom()
    {
        // Find other player and throw in direction
        GameObject otherPlayerObject = GameObject.Find(otherPlayer);
        float throwRange = 30f; 
        float distance = Vector3.Distance(transform.position, otherPlayerObject.transform.position);
        
        // rotation towards other player 
        Vector3 direction = otherPlayerObject.transform.position - transform.position;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = 5f;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // return if mushroom not equipped, less than min. count
        // or target player is out of range
        if (!isMushroomSelected || !mushroomInHand.activeSelf || mushroomCount < maxMushrooms || distance > throwRange)
        {
            return;
        }

        if (otherPlayerObject == null)
        {
            Debug.Log("Other player not found.");
            return;
        }

        if (distance > throwRange)
        {
            Debug.Log("Other player is out of range. Cannot throw.");
            return;
        }

        // Create the mushroom and start the arc movement
        GameObject thrownMushroom = Instantiate(thrownMushroomPrefab, throwOrigin.position, Quaternion.identity);
        StartCoroutine(MoveMushroomInArc(thrownMushroom, otherPlayerObject.transform));

        // Reset mushroom selection
        // deduct mushroom
        isMushroomSelected = false;
        mushroomInHand.SetActive(false);
        mushroomCount -= 3;
    }

    private IEnumerator MoveMushroomInArc(GameObject mushroom, Transform targetTransform)
    {
        // Throw duration
        float duration = 0.75f; 
        float elapsedTime = 0f;

        Vector3 startPosition = mushroom.transform.position;

        if (mushroom != null)
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Get updated target position
                Vector3 updatedPosition = targetTransform.position;
                Vector3 midPoint = (startPosition + updatedPosition) / 2 + Vector3.up * 2f;

                // Interpolating between start, mid, and target
                Vector3 currentPosition = Vector3.Lerp(Vector3.Lerp(startPosition, midPoint, t), Vector3.Lerp(midPoint, updatedPosition, t), t);
                mushroom.transform.position = currentPosition;

                yield return null;
            }
        }

        // Ensure mushroom reaches the exact final position
        mushroom.transform.position = targetTransform.position;

        ThirdPersonController targetPlayer = targetTransform.GetComponent<ThirdPersonController>();
        Animator targetAnimator = targetPlayer.GetComponent<Animator>();

        // Target to play take hit after mushroom reaches dest.
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("TakeHit");
        }

        // Slow target player 
        if (targetPlayer != null)
        {
            targetPlayer.ApplySlowEffect(5f);
            SetMushroomInvisible(mushroom);
        }

        // Destroy mushroom after applying effect
        Destroy(mushroom);
    }

    // Track if character currently taking hit
    bool InTakeHitAnimaton()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("Take hit");
    }

    // Disable mushroom renderer
    void SetMushroomInvisible(GameObject mushroom)
    {
        foreach (MeshRenderer mesh in mushroom.GetComponentsInChildren<MeshRenderer>())
        {
            mesh.enabled = false;
        }
    }

    // Coroutine to slow other player
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

    // --- UI UPDATES ---
    public void CollectMushroom()
    {
        mushroomCount++;
        UpdateMushroomUI();
    }

    void UpdateMushroomUI()
    {
        mushroomText.text = "Mushrooms: " + mushroomCount;
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

    // --- SFX ---
    void PlayPunchSound()
    {
        audioSource.PlayOneShot(punchSound);
    }

    void PlayThrowSound()
    {
        audioSource.PlayOneShot(throwSound);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 1, 0), attackRange);
    }
}