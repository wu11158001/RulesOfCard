using UnityEngine;
using DG.Tweening;

public partial class EntryView : MonoBehaviour
{
    private const long IconDuration = 1000;

    private void Start()
    {
        InitializeDocument();

        AnimateLoop();
    }

    /// <summary>
    /// 循環動畫
    /// </summary>
    private void AnimateLoop()
    {
        // Icon
        IconImg.schedule.Execute(() => {
            IconImg.ToggleInClassList("IconImg_Up");
        }).Every(IconDuration);

        // 文字
        DOTween.To(() => 0f, x => {
            int dotCount = Mathf.FloorToInt(x) % 4;
            TipLable.text = "載入中" + new string('.', dotCount);
        }, 4f, 2.0f)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);
    }
}