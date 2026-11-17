using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image fillImage;

    [Header("Colours")]
    [Tooltip("Enemy health bars are always this colour")]
    [SerializeField] private Color enemyColor = new Color(0.9f, 0.1f, 0.1f);

    [Tooltip("Defender bars are this colour at full health...")]
    [SerializeField] private Color defenderFullColor = new Color(0.2f, 0.9f, 0.2f);

    [Tooltip("...and fade to this colour as they lose health")]
    [SerializeField] private Color defenderLowColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("Size")]
    [Tooltip("How much the bar grows for every unit the camera is away, so it stays readable when zoomed out. Raise it for bigger bars.")]
    [SerializeField] private float distanceScale = 0.0006f;

    private Transform target;
    private float heightOffset;
    private float baseScale;

    private Camera mainCamera;

    private Enemy enemy;
    private Defender defender;
    private Tower tower;

    private void Awake()
    {
        // The size the prefab was made at is the smallest the bar will ever be
        baseScale = transform.localScale.x;
    }

    public void SetTarget(Transform newTarget, float newHeightOffset)
    {
        target = newTarget;
        heightOffset = newHeightOffset;

        enemy = target.GetComponent<Enemy>();
        defender = target.GetComponent<Defender>();
        tower = target.GetComponent<Tower>();

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

            // Grow with distance so the bar is still easy to see when the camera is zoomed out
            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            float scale = Mathf.Max(baseScale, distance * distanceScale);
            transform.localScale = Vector3.one * scale;
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
            fillImage.color = enemyColor;
        }
        else if (defender != null)
        {
            healthPercent = (float)defender.CurrentHealth / defender.MaxHealth;
            fillImage.color = Color.Lerp(defenderLowColor, defenderFullColor, healthPercent);
        }
        else if (tower != null)
        {
            healthPercent = (float)tower.CurrentHealth / tower.MaxHealth;
            fillImage.color = Color.Lerp(defenderLowColor, defenderFullColor, healthPercent);
        }

        fillImage.fillAmount = Mathf.Clamp01(healthPercent);
    }
}