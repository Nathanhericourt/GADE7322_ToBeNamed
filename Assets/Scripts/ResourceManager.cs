using UnityEngine;

//Player Resource tracker
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    [Tooltip("The amount of resources the player starts with.")]
    public int startingResources = 100;

    [Header("Passive Income")]
    [Tooltip("Resources gained automatically every (per set tick.")]
    public int incomePerTick = 5;

    [Tooltip("Seconds between each auto income tick")]
    public float incomeInterval = 3f;

    [Tooltip("Current reasorce total the player has.")]
    public int currentResource;
    private float incomeTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentResource = startingResources;
    }

    private void Update()
    {
        incomeTimer += Time.deltaTime;
        if (incomeTimer >= incomeInterval)
        {
            incomeTimer = 0f;
            Add(incomePerTick);
        }
    }

    //Adds resources
    public void Add(int amount)
    {
        currentResource += amount;
    }

    public bool CanAfford(int cost)
    {
        return currentResource >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (!CanAfford(cost))
            return false;
        
        currentResource -= cost;
        return true;
    }
}
