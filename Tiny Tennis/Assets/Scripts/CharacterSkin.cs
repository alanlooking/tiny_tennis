using UnityEngine;

// Описание персонажа: портрет для экрана выбора + клипы всех анимаций
[CreateAssetMenu(fileName = "NewCharacterSkin", menuName = "PingPong/Character Skin")]
public class CharacterSkin : ScriptableObject
{
    [Header("Инфо")]
    public string displayName = "Персонаж";
    public Sprite portrait; // Портрет для кнопки выбора

    [Header("Клипы анимаций (файлы .anim)")]
    public AnimationClip idleClip;
    public AnimationClip walkClip;          // не забыть Loop Time!
    public AnimationClip hitPrimaryClip;
    public AnimationClip hitAlternateClip;
}