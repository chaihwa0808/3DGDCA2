using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image healthbarSprite;
    private Camera cam1;
    private Camera cam2;

    void Start()
    {
        // Find both cameras
        cam1 = GameObject.FindWithTag("MainCamera1").GetComponent<Camera>();
        cam2 = GameObject.FindWithTag("MainCamera2").GetComponent<Camera>();
    }

    // Update the health bar UI
    public void UpdateHealthBar(float maxHealth, float currentHealth)
    {
        healthbarSprite.fillAmount = currentHealth / maxHealth;
    }

    void Update()
    {
        Camera nearestCam = GetNearestCamera();
        if (nearestCam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - nearestCam.transform.position);
        }
    }

    Camera GetNearestCamera()
    {
        if (cam1 == null && cam2 == null) return null;
        if (cam1 == null) return cam2;
        if (cam2 == null) return cam1;

        float dist1 = Vector3.Distance(transform.position, cam1.transform.position);
        float dist2 = Vector3.Distance(transform.position, cam2.transform.position);

        return (dist1 < dist2) ? cam1 : cam2; // Return the nearest camera
    }
}

