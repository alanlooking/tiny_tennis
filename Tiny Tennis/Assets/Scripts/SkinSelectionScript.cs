using UnityEngine;
using UnityEngine.UI;

public class SkinSelectionScreen : MonoBehaviour
{
    [Header("Скины (порядок в массиве = порядок кнопок)")]
    [SerializeField] private CharacterSkin[] availableSkins;

    [Header("UI")]
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private GameObject panel; // Панель экрана выбора

    [Header("Кому применяем скин")]
    [SerializeField] private Animator playerAnimator;

    private bool chosen = false;

    private void Start()
    {
        for (int i = 0; i < skinButtons.Length && i < availableSkins.Length; i++)
        {
            CharacterSkin skin = availableSkins[i];
            Button button = skinButtons[i];

            // Портрет на кнопку берём из ассета скина
            if (skin.portrait != null)
            {
                button.image.sprite = skin.portrait;
            }

            int index = i; // локальная копия для замыкания
            button.onClick.AddListener(() => ChooseSkin(index));
        }

        panel.SetActive(true);
    }

    private void ChooseSkin(int skinIndex)
    {
        if (chosen) return; // защита от двойного клика
        Debug.Log($"[Выбор] Клик по кнопке: скин {skinIndex} — {availableSkins[skinIndex].displayName}");

        CharacterSkin skin = availableSkins[skinIndex];
        if (!ApplySkin(playerAnimator, skin)) return; // ошибка в лог, экран остаётся открытым

        chosen = true;
        panel.SetActive(false);

        // Матч стартует только теперь
        GameManager.Instance.StartNewMatch();
    }

    // Подменяет клипы в контроллере игрока на клипы выбранного персонажа.
    // Стейты, переходы и параметры остаются от базового контроллера.
    private bool ApplySkin(Animator animator, CharacterSkin skin)
    {
        if (animator == null || skin == null)
        {
            Debug.LogError("[SkinSelection] Animator или скин не назначены");
            return false;
        }

        if (skin.idleClip == null || skin.walkClip == null ||
            skin.hitPrimaryClip == null || skin.hitAlternateClip == null)
        {
            Debug.LogError($"[SkinSelection] У скина '{skin.displayName}' заполнены не все клипы");
            return false;
        }

        AnimatorOverrideController overrideController =
            new AnimatorOverrideController(animator.runtimeAnimatorController);

        // В кавычках — имена клипов, которые лежат в ТЕКУЩЕМ контроллере игрока.
        // Справа — клипы из ассета скина (могут называться как угодно).
        overrideController["idle_north"] = skin.idleClip;
        overrideController["walking_north"] = skin.walkClip;
        overrideController["first_punch"] = skin.hitPrimaryClip;
        overrideController["second_punch_north"] = skin.hitAlternateClip;
        Debug.Log($"[Выбор] Применяю скин '{skin.displayName}': Idle={skin.idleClip.name}, Walk={skin.walkClip.name}");
        animator.runtimeAnimatorController = overrideController;
        return true;
    }
}