using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StaminaBarUI : MonoBehaviour
{
    [SerializeField]
    private Stamina stamina;

    [SerializeField]
    private Image fillImage;

    private void OnEnable()
    {
        if (stamina != null)
        {
            stamina.StaminaChanged +=
                HandleStaminaChanged;
        }
    }

    private void Start()
    {
        if (stamina == null ||
            fillImage == null)
        {
            Debug.LogError(
                $"{nameof(StaminaBarUI)} is missing references.",
                this
            );

            enabled = false;
            return;
        }

        UpdateBar(
            stamina.CurrentStamina,
            stamina.MaxStamina
        );
    }

    private void OnDisable()
    {
        if (stamina != null)
        {
            stamina.StaminaChanged -=
                HandleStaminaChanged;
        }
    }

    private void HandleStaminaChanged(
        float current,
        float maximum)
    {
        UpdateBar(current, maximum);
    }

    private void UpdateBar(
        float current,
        float maximum)
    {
        fillImage.fillAmount =
            maximum > 0f
                ? Mathf.Clamp01(current / maximum)
                : 0f;
    }
}