using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("飘字 Prefab 引用")]
    public GameObject damageTextPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnDamageText(Vector3 position, float damage, DamageType damageType = DamageType.Normal)
    {
        if (damageTextPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 1.8f + new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));

        GameObject popText = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        DamageText script = popText.GetComponent<DamageText>();

        if (script != null)
        {
            script.Setup(damage, damageType);
        }
    }
}