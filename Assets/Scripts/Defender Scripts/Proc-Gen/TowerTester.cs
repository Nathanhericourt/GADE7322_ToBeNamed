using System.Collections.Generic;
using UnityEngine;

public class LuckyTowerRollTester : MonoBehaviour
{
    [SerializeField] private int rolls = 1000;

    [ContextMenu("Run Rolls")]
    private void RunRolls()
    {
        Dictionary<LuckyRarity, List<float>> startDps = new Dictionary<LuckyRarity, List<float>>();
        Dictionary<LuckyRarity, List<float>> maxDps = new Dictionary<LuckyRarity, List<float>>();
        foreach (LuckyRarity r in System.Enum.GetValues(typeof(LuckyRarity)))
        {
            startDps[r] = new List<float>();
            maxDps[r] = new List<float>();
        }

        for (int i = 0; i < rolls; i++)
        {
            LuckyTowerData d = LuckyTowerGenerator.Generate(Random.Range(1, int.MaxValue));
            startDps[d.rarity].Add(d.GetDps(0));
            maxDps[d.rarity].Add(d.GetDps(d.upgradeTraits.Count));
        }

        foreach (LuckyRarity rarity in startDps.Keys)
        {
            List<float> start = startDps[rarity];
            if (start.Count == 0)
            {
                Debug.Log($"{rarity}: none rolled");
                continue;
            }

            float min = float.MaxValue, max = float.MinValue, total = 0f, totalMax = 0f;
            foreach (float v in start)
            {
                if (v < min) min = v;
                if (v > max) max = v;
                total += v;
            }
            foreach (float v in maxDps[rarity])
                totalMax += v;

            Debug.Log($"{rarity}: {start.Count} rolled | start DPS min {min:F1}, avg {total / start.Count:F1}, max {max:F1}" +
                      $" | avg with all upgrade traits {totalMax / start.Count:F1}");
        }
    }
}