using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class PokerGame : MonoBehaviour
{
    [SerializeField] PokerCollection pokerCollection;

    VisualElement[] TableauPanels;
    VisualElement[] FoundationPanels;

    private readonly Vector2 StockAdd = new(-1, -1);
    private const float ColumnAddY = 40;

    private void Start()
    {
        InitializeDocument();

        TableauPanels = new VisualElement[]
        {
            Tableau_0, Tableau_1, Tableau_2, Tableau_3, Tableau_4, Tableau_5, Tableau_6
        };

        FoundationPanels = new VisualElement[]
        {
            FoundationPanel_0, FoundationPanel_1, FoundationPanel_2, FoundationPanel_3
        };

        DragBox.pickingMode = PickingMode.Ignore;
        StockPanel.RegisterCallback<ClickEvent>(OnStockClick);

        CreateStockPokers();
    }

    /// <summary>
    /// 創建牌推
    /// </summary>
    private void CreateStockPokers()
    {
        BasePoker.RemoveFromHierarchy();

        // 牌組皮膚
        PokerStyle pokerStyle = PokerStyle.Deck01;

        // 產生牌資料
        List<PokerSkinData> pokerDatas = new();
        foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
        {
            foreach (RankEnum rank in Enum.GetValues(typeof(RankEnum)))
            {

                PokerSkinData pokerData = new() { Suit = suit, Rank = rank };
                pokerDatas.Add(pokerData);
            }
        }
        pokerDatas.Shuffle();

        // 產生牌
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

            CardDataContainer cardDataContainer = new (
                data: pokerSkinData, 
                card: backCard, 
                dragPanel: DragPanel, 
                checkDropTarget: CheckDropTarget);

            frontCard.userData = cardDataContainer;

            backCard.Add(frontCard);

            if(tableauIndex < TableauPanels.Length)
            {
                // 操作區域初始牌

                backCard.style.top = ColumnAddY * tableauClumnIndex;

                if (tableauClumnIndex == tableauIndex)
                {
                    frontCard.style.visibility = Visibility.Visible;
                    backCard.style.visibility = Visibility.Hidden;

                    CardDataContainer data = frontCard.userData as CardDataContainer;
                    data.BindEvents();
                }
                else
                {
                    frontCard.style.visibility = Visibility.Hidden;
                    backCard.style.visibility = Visibility.Visible;
                }

                TableauPanels[tableauIndex].Add(backCard);

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

    /// <summary>
    /// 牌堆點擊
    /// </summary>
    private void OnStockClick(ClickEvent evt)
    {
        CardDataContainer data = null;

        // 重製牌堆
        if (StockPanel.childCount == 0)
        {
            var cardsToMove = WastePanel.Children().ToList();
            cardsToMove.Reverse();

            int stockIndex = 0;
            foreach (var card in cardsToMove)
            {
                card.style.display = DisplayStyle.Flex;
                card.style.visibility = Visibility.Visible;

                VisualElement frontCard = card.Q<VisualElement>("FrontCard");
                if (frontCard != null)
                {
                    frontCard.pickingMode = PickingMode.Position;
                    frontCard.style.visibility = Visibility.Hidden;
                }

                card.style.left = StockAdd.x * stockIndex;
                card.style.top = StockAdd.y * stockIndex;

                data = frontCard.userData as CardDataContainer;
                data.UnbindEvents();
                StockPanel.Add(card);

                stockIndex++;
            }

            return;
        }

        // 已出現的牌影藏
        foreach (var wasteCard in WastePanel.Children())
        {
            wasteCard.style.display = DisplayStyle.None;
        }        

        // 取得牌堆最上面那張牌
        VisualElement topCard = StockPanel.Children().Last();

        topCard.style.left = 0;
        topCard.style.top = 0;

        VisualElement front = topCard.Q<VisualElement>("FrontCard");
        if (front != null)
        {
            topCard.style.visibility = Visibility.Hidden;
            front.style.visibility = Visibility.Visible;
        }

        data = front.userData as CardDataContainer;
        data.BindEvents();

        WastePanel.Add(topCard);
    }

    /// <summary>
    /// 檢測卡撲克牌放置目標
    /// </summary>
    private void CheckDropTarget(VisualElement card)
    {
        card.pickingMode = PickingMode.Ignore;
        VisualElement cardFront = card.Q<VisualElement>("FrontCard");
        CardDataContainer cardPokerData = cardFront.userData as CardDataContainer;

        // 取得滑鼠放開位置下方的元素
        VisualElement target = Root.panel.Pick(card.worldBound.center);
        CardDataContainer targetPokerData = target?.userData != null ? target?.userData as CardDataContainer : null;

        if (FoundationPanels.Contains(target) && cardPokerData.SkinData.Rank == RankEnum.Ace)
        {
            // 放置在結算區 && 結算區未有牌

            target.Add(card);
            card.style.left = 0;
            card.style.top = 0;
        }
        else if(targetPokerData != null && (int)targetPokerData.SkinData.Rank == (int)cardPokerData.SkinData.Rank - 1)
        {
            // 放置在其他牌

            bool isTop = TopBox.Contains(target);
            bool isBottom = BottomBox.Contains(target);

            if (isBottom)
            {
                // 目標在待結算區

                VisualElement tableauPanel = null;
                foreach (var tableau in TableauPanels)
                {
                    if(tableau.Contains(target))
                    {
                        tableauPanel = tableau;
                        break;
                    }
                }

                if (cardPokerData.SkinData.SuitColor != targetPokerData.SkinData.SuitColor)
                {
                    if (tableauPanel != null && target.parent == tableauPanel.Children().Last())
                    {
                        tableauPanel.Add(card);
                        card.style.left = 0;
                        card.style.top = target.parent.resolvedStyle.top + ColumnAddY;
                    }
                    else
                    {
                        // 沒碰到任何結算區，返回原位
                        cardPokerData.GoBack();
                    }
                }
                else
                {
                    cardPokerData.GoBack();
                }
            }
            else
            {
                cardPokerData.GoBack();
            }
        }
        else
        {
            cardPokerData.GoBack();
        }

        // 重製事件處理
        card.pickingMode = PickingMode.Position;
        cardFront.pickingMode = PickingMode.Position;
    }
}