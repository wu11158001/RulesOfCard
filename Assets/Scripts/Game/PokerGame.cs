using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class PokerGame : MonoBehaviour
{
    [SerializeField] PokerCollection pokerCollection;

    private readonly Vector2 StockAdd = new(-1, -1);
    private const float ColumnAddY = -40;

    private class PokerData
    {
        public SuitEnum Suit;
        public RankEnum Rank;
    }

    private void Start()
    {
        InitializeDocument();

        CreateStockPokers();
    }

    /// <summary>
    /// 創建牌推
    /// </summary>
    private void CreateStockPokers()
    {
        BasePoker.style.display = DisplayStyle.None;

        // 牌組皮膚
        PokerStyle pokerStyle = PokerStyle.Deck01;

        // 產生牌資料
        List<PokerData> pokerDatas = new();
        foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
        {
            foreach (RankEnum rank in Enum.GetValues(typeof(RankEnum)))
            {
                PokerData pokerData = new() { Suit = suit, Rank = rank};
                pokerDatas.Add(pokerData);
            }
        }
        pokerDatas.Shuffle();

        // 產生牌
        VisualElement[] tableauPanel = new VisualElement[]
        {
            Tableau_0, Tableau_1, Tableau_2, Tableau_3, Tableau_4, Tableau_5, Tableau_6
        };

        int tableauIndex = 0;
        int tableauClumnIndex = 0;
        int stockIndex = 0;

        PokerDeck pokerDeck = pokerCollection.GetFrontSprite(pokerStyle: pokerStyle);
        for (int i = 0; i < pokerDatas.Count; i++)
        {
            PokerSkinData pokerSkinData = pokerDeck.pokerDatas.Find(x => x.Suit == pokerDatas[i].Suit && x.Rank == pokerDatas[i].Rank);

            // 背面
            VisualElement backCard = new();
            backCard.name = $"{pokerSkinData.Suit}_{pokerSkinData.Rank}";
            backCard.AddToClassList("base-poker");

            backCard.style.backgroundImage = new StyleBackground(pokerDeck.BackSprite);

            // 正面
            VisualElement frontCard = new();
            frontCard.name = "FrontCard";
            frontCard.AddToClassList("base-poker-value");
            frontCard.style.backgroundImage = new StyleBackground(pokerSkinData.FrontSprite);
            
            backCard.Add(frontCard);

            if(tableauIndex < tableauPanel.Length)
            {
                // 操作區域初始牌

                backCard.style.bottom = ColumnAddY * tableauClumnIndex;

                if (tableauClumnIndex == tableauIndex)
                {
                    frontCard.style.visibility = Visibility.Visible;
                    backCard.style.visibility = Visibility.Hidden;
                }
                else
                {
                    frontCard.style.visibility = Visibility.Hidden;
                    backCard.style.visibility = Visibility.Visible;
                }

                tableauPanel[tableauIndex].Add(backCard);

                if(tableauClumnIndex == tableauIndex)
                {
                    tableauIndex++;
                    tableauClumnIndex = 0;
                }
                else
                {
                    tableauClumnIndex++;
                }                
            }
            else
            {
                // 牌堆初始牌

                frontCard.style.visibility = Visibility.Hidden;
                backCard.style.visibility = Visibility.Visible;

                backCard.style.left = StockAdd.x * stockIndex;
                backCard.style.top = StockAdd.y * stockIndex;

                StockPanel.Add(backCard);

                stockIndex++;
            }
        }
    }
}