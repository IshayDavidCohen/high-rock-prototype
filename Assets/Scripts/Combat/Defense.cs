using UnityEngine;


[DisallowMultipleComponent]
public sealed class Defense : MonoBehaviour
{
    private const float ArmorScalingConstant = 100f;

    [Header("Defense")]
    [SerializeField, Min(0f)]
    private float armor = 0f;

    [SerializeField, Range(0f, 100f)]
    private float magicResistancePercent = 0f;
    
    public float Armor => armor;

    public float MagicResistancePercent => magicResistancePercent;
    public float PhysicalDamageReduction => armor / (armor + ArmorScalingConstant);

    public float ResolveDamage(float incomingDamage, DamageType damageType)
    {
        if (incomingDamage <= 0f)
            return 0f;

        return damageType switch
        {
            DamageType.Physical =>
                ResolvePhysicalDamage(incomingDamage),

            DamageType.Magical =>
                ResolveMagicalDamage(incomingDamage),

            _ => incomingDamage
        };
    }

    private float ResolvePhysicalDamage(float incomingDamage)
    {
        return incomingDamage * (1f - PhysicalDamageReduction);
    }

    private float ResolveMagicalDamage(float incomingDamage)
    {
        float resistance = magicResistancePercent / 100f;
        return incomingDamage * (1f - resistance);
    }
}
