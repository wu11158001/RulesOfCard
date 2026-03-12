using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectModeView : MonoBehaviour
{
    [SerializeField] PokerCollection PokerCollection;

    private Button[] CardStyles;

    private int StyleIndex = 0;

    private void Start()
    {
        InitializeDocument();

        // 卡片皮膚按鈕
        CardStyles = new Button[]
        {
            StyleBtn_0, StyleBtn_1, StyleBtn_2, StyleBtn_3
        };

        // 一次發一張牌
        OneCardBtn.clicked += () =>
        {
            StartGame(1);
        };

        // 一次發三張牌
        ThreeCardBtn.clicked += () =>
        {
            StartGame(3);
        };

        // 卡片皮膚ToggleGroup
        var cardToggleGroup = Root?.Q<ToggleButtonGroup>("CardToggleGroup");
        cardToggleGroup.RegisterValueChangedCallback(evt =>
        {
            int[] activeButtons = cardToggleGroup.value.GetActiveOptions(new int[64]).ToArray();
            ulong mask = 0UL;

            foreach (int index in activeButtons)
            {
                if (index is < 0 or >= 64)
                    continue;

                mask |= (1UL << index);

                bool isSelected = (mask & (1UL << index)) != 0;

                if (isSelected)
                {
                    StyleIndex = index;
                    break;
                }
            }
            
        });

        SetCardStyleBtns();
    }

    /// <summary>
    /// 設置卡片皮膚按鈕
    /// </summary>
    private void SetCardStyleBtns()
    {
        for (int i = 0; i < PokerCollection.PokerDecks.Count; i++)
        {
            CardStyles[i].style.backgroundImage = new StyleBackground(PokerCollection.PokerDecks[i].BackSprite);
        }
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    private void StartGame(int showCount)
    {
        AddressableManager.Instance.LoadAssets(
                viewType: ViewEnum.PokerGame,
                callback: (obj) =>
                {
                    PokerGame pokerGame = obj.GetComponent<PokerGame>();
                    pokerGame.SetData(pokerStyle: (PokerStyle)StyleIndex , showCount: showCount);

                    Destroy(gameObject);
                });
    }
}