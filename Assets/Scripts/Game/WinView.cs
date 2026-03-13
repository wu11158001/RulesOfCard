using UnityEngine;
using System;
using System.Collections;

public partial class WinView : MonoBehaviour
{
    public void SetData(Action newGameAction)
    {
        InitializeDocument();

        NewGameBtn.clicked += () =>
        {
            newGameAction?.Invoke();
            Destroy(gameObject);
        };

        StartCoroutine(IShowAni_Up());
    }

    /// <summary>
    /// 顯示動畫
    /// </summary>
    private IEnumerator IShowAni_Up()
    {
        yield return null;
        FrameBox.AddToClassList("ShowAni-Up");
    }
}