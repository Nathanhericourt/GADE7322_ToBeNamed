using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image fillImage;

    private Transform target;
    private float heightOffset;

    private Camera mainCamera;

    private Enemy enemy;
    private Defender defender;

    public void SetTarget(Transform newTarget, float newHeightOffset)
    {
        target = newTarget;
        heightOffset = newHeightOffset;

        enemy = target.GetComponent<Enemy>();
        defender = target.GetComponent<Defender>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Keep the health bar above the character
        transform.position = target.position + Vector3.up * heightOffset;

        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }

        UpdateHealth();
    }

    private void UpdateHealth()
    {
        if (fillImage == null)
            return;

        float healthPercent = 1f;

        if (enemy != null)
        {
            healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
        }
        else if (defender != null)
        {
            healthPercent = (float)defender.CurrentHealth / defender.MaxHealth;
        }

        fillImage.fillAmount = Mathf.Clamp01(healthPercent);
    }
}