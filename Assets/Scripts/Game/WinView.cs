using UnityEngine;
using System;

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

        FrameBox.AddToClassList("ShowAni-Up");
    }
}