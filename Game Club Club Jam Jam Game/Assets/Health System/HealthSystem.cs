using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private bool scaleWithDifficulty;
    [Header("Health")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;
    [SerializeField] private SliderIndicator healthIndicator;
    [SerializeField] private ScreenTint screenTint;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private float damageTintFadeSpeed = 0.25f;
    [SerializeField] private float healTintFadeSpeed = 0.35f;

    [Header("Armor")]
    [SerializeField] private float armor;
    [SerializeField] private float maxArmor;
    [SerializeField] private SliderIndicator armorIndicator;
    [Tooltip("Each armor point reduces health damage by this factor.")]
    [SerializeField] private float armorDamageReductionFactor = 0.01f;
    [Tooltip("Determines how many armor points to remove with damage. 1=>100% of damage, 2=>50% of damage, 0.5=>200% of damage, etc")]
    public float armorDurability = 1f;

    [Header("Camera Shake")]
    [SerializeField] private bool shakeCameraOnDamage;
    [Tooltip("Multiplier with damage amount for shake magnitude.")]
    [SerializeField] private float damageShakeIntensity = 0.1f;
    [SerializeField] private float damageShakeSpeed = 1.5f;
    [SerializeField] private float damageShakeDuration = 0.1f;
    [Tooltip("Interval in seconds that damage can be applied.")]
    [SerializeField] private float damageCooldown = 0.25f;
    [Tooltip("If false, damage between frames accumulates and is applied at the end of cooldown.")]
    [SerializeField] private bool immuneDuringCooldown = false;

    private float _damageCooldown;
    private float _accumulatedDamage;

    public UnityEvent OnHurt;
    public UnityEvent OnDeath;
    public UnityEvent OnArmorBreak;

    private void Start()
    {
        if (scaleWithDifficulty)
        {
            health *= SceneLoader.Instance.DifficultyScale;
            maxHealth *= SceneLoader.Instance.DifficultyScale;
            armor *= SceneLoader.Instance.DifficultyScale;
            maxArmor *= SceneLoader.Instance.DifficultyScale;
        }
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }

    private void Update()
    {
        if (_damageCooldown > 0f)
        {
            _damageCooldown -= Time.deltaTime;
            if (_damageCooldown <= 0f && !immuneDuringCooldown)
            {
                AddHealth(_accumulatedDamage);
                _accumulatedDamage = 0f;
            }
        }
    }

    public float GetHealth()
    {
        return health;
    }

    public void SetHealth(float health)
    {
        if (health <= 0f && this.health > 0f)
            OnDeath?.Invoke();
        this.health = Mathf.Clamp(health, 0f, maxHealth);
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public void AddHealth(float amount)
    {
        if (amount == 0f)
            return;
        if (amount < 0f)
        {
            if (_damageCooldown > 0f)
            {
                if (!immuneDuringCooldown)
                    _accumulatedDamage += amount;
                return;
            }
            _damageCooldown = damageCooldown;
                
            float newAmount = amount * Mathf.Max(1f - armorDamageReductionFactor * armor, 0f);
            AddArmor(amount); // Apply damage to armor
            amount = newAmount;

            if (amount >= 0f)
                return;
            if (shakeCameraOnDamage)
            {
                CameraControl.Instance.StartCameraShake(-amount * damageShakeIntensity, damageShakeSpeed, damageShakeDuration);
            }
            if (screenTint != null)
                screenTint.StartTint(damageColor, damageTintFadeSpeed, 0f);
            OnHurt?.Invoke();
        }
        else if (screenTint != null)
        {
            screenTint.StartTint(healColor, healTintFadeSpeed, 0f);
        }
        float newHealth = health + amount;
        if (newHealth > maxHealth)
        {
            health = maxHealth;
        }
        else if (newHealth <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();
        }
        else
        {
            health = newHealth;
        }
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public void AddMaxHealth(float amount)
    {
        float newMaxHealth = maxHealth + amount;
        if (newMaxHealth <= 0f)
        {
            maxHealth = 0f;
        }
        else
        {
            maxHealth = newMaxHealth;
        }
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public float GetArmor()
    {
        return armor;
    }

    public void SetArmor(float armor)
    {
        this.armor = Mathf.Clamp(armor, 0f, maxArmor);
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }

    public void AddArmor(float amount)
    {
        if (amount < 0f)
        {
            amount *= 1f / armorDurability;
        }
        float newArmor = armor + amount;
        armor += amount;
        if (newArmor > maxArmor)
        {
            armor = maxArmor;
        }
        else if (newArmor <= 0f)
        {
            armor = 0f;
            OnArmorBreak?.Invoke();
        }
        else
        {
            armor = newArmor;
        }
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }

    public void OnPlayerDeath()
    {
        _accumulatedDamage = 0f;
        PlayerControl.Instance.KillPlayer(1f);
    }
}
