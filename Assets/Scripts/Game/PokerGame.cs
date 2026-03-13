using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class PokerGame : MonoBehaviour
{
    [SerializeField] PokerCollection PokerCollection;

    VisualElement[] TableauPanels;
    VisualElement[] FoundationPanels;

    private PokerStyle DefaultStyle = PokerStyle.Deck01;
    private List<VisualElement> Cards = new();
    private List<PokerSkinData> PokerSkinDatas = new();
    private int ShowCount = 1;

    private Stack<RecordData> StepRecordDatas = new();

    private readonly Vector2 StockAdd = new(-1, -1);
    private const float ColumnAddY = 40;
    private const float RowAddX = 50;

    /// <summary>
    /// 記錄每次行動資料
    /// </summary>
    private class RecordData
    {
        public List<RecordCardData> RecordCardDatas;
        public CardDataContainer OpenCard;
    }

    /// <summary>
    /// 記錄每次行動卡片資料
    /// </summary>
    private class RecordCardData
    {
        public VisualElement Card;
        public Vector2 PrePosition;
        public VisualElement PreContainer;
    }

    public void SetData(PokerStyle pokerStyle = PokerStyle.Deck01, int showCount = 1)
    {
        InitializeDocument();

        DefaultStyle = pokerStyle;
        ShowCount = showCount;

        // 操作區
        TableauPanels = new VisualElement[]
        {
            Tableau_0, Tableau_1, Tableau_2, Tableau_3, Tableau_4, Tableau_5, Tableau_6
        };

        // 結算區
        FoundationPanels = new VisualElement[]
        {
            FoundationPanel_0, FoundationPanel_1, FoundationPanel_2, FoundationPanel_3
        };

        // 上一步按鈕
        PreStepBtn.clicked += () =>
        {
            DoPreStep();
        };

        // 新遊戲按鈕
        NewGameBtn.clicked += () =>
        {
            AddressableManager.Instance.LoadAssets(
                viewType: ViewEnum.SelectModeView, 
                callback: (obj) =>
                {
                    Destroy(gameObject);
                });
        };

        // 重新開始按鈕
        ReStartBtn.clicked += () =>
        {
            CreateStockPokers(pokerStyle, false);
        };

        DragBox.pickingMode = PickingMode.Ignore;
        StockPanel.RegisterCallback<ClickEvent>(OnStockClick);

        BasePoker.RemoveFromHierarchy();

        CreateStockPokers(pokerStyle, true);
    }

    /// <summary>
    /// 創建牌推
    /// </summary>
    private void CreateStockPokers(PokerStyle pokerStyle, bool isNewGame)
    {
        StepRecordDatas.Clear();
        for (int i = 0; i < Cards.Count; i++)
        {
            Cards[i].RemoveFromHierarchy();
        }

        if(isNewGame || PokerSkinDatas.Count == 0)
        {
            PokerSkinDatas.Clear();
            foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
            {
                foreach (RankEnum rank in Enum.GetValues(typeof(RankEnum)))
                {

                    PokerSkinData pokerData = new() { Suit = suit, Rank = rank };
                    PokerSkinDatas.Add(pokerData);
                }
            }

            PokerSkinDatas.Shuffle();
        }

        // 產生牌
        int tableauIndex = 0;
        int tableauClumnIndex = 0;
        int stockIndex = 0;

        PokerDeck pokerDeck = PokerCollection.GetFrontSprite(pokerStyle: pokerStyle);
        for (int i = 0; i < PokerSkinDatas.Count; i++)
        {
            PokerSkinData pokerSkinData = pokerDeck.pokerDatas.Find(x => x.Suit == PokerSkinDatas[i].Suit && x.Rank == PokerSkinDatas[i].Rank);

            // 背面
            VisualElement backCard = new();
            backCard.name = $"{pokerSkinData.Suit}_{pokerSkinData.Rank}";
            backCard.AddToClassList("base-poker");
            backCard.style.backgroundImage = new StyleBackground(pokerDeck.BackSprite);
            backCard.pickingMode = PickingMode.Ignore;

            // 正面
            VisualElement frontCard = new();
            frontCard.name = "FrontCard";
            frontCard.AddToClassList("base-poker-value");
            frontCard.style.backgroundImage = new StyleBackground(pokerSkinData.FrontSprite);

            CardDataContainer cardDataContainer = new (
                data: pokerSkinData, 
                backCard: backCard,
                fontCard: frontCard,
                dragPanel: DragPanel,
                wastePanel: WastePanel,
                checkDropTargetAction: CheckDropTarget,
                checkDoubleClickAction: CheckDoubleClick);

            backCard.userData = cardDataContainer;
            backCard.Add(frontCard);

            Cards.Add(backCard);

            if (tableauIndex < TableauPanels.Length)
            {
                // 操作區域初始牌

                backCard.style.top = ColumnAddY * tableauClumnIndex;

                if (tableauClumnIndex == tableauIndex)
                {
                    frontCard.style.visibility = Visibility.Visible;
                    backCard.style.visibility = Visibility.Hidden;

                    CardDataContainer data = backCard.userData as CardDataContainer;
                    data.OriginalParent = TableauPanels[tableauIndex];
                    data.StartPosition = new(backCard.style.left.value.value, backCard.style.top.value.value);
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

                CardDataContainer data = backCard.userData as CardDataContainer;
                data.OriginalParent = StockPanel;
                data.StartPosition = new(backCard.style.left.value.value, backCard.style.top.value.value);

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
                data = card.userData as CardDataContainer;

                card.style.display = DisplayStyle.Flex;
                card.style.visibility = Visibility.Visible;

                card.style.left = StockAdd.x * stockIndex;
                card.style.top = StockAdd.y * stockIndex;

                data.FontCard.pickingMode = PickingMode.Position;
                data.FontCard.style.visibility = Visibility.Hidden;
                data.OriginalParent = StockPanel;
                data.StartPosition = new(card.style.left.value.value, card.style.top.value.value);
                data.UnbindEvents();

                StockPanel.Add(card);

                stockIndex++;
            }

            return;
        }

        // 影藏發牌區所有牌
        foreach (VisualElement card in WastePanel.Children())
        {
            card.style.left = 0;
            card.style.top = 0;
        }

        // 發牌
        int index = 0;
        int totalChildren = StockPanel.childCount;
        List<CardDataContainer> recordDatas = new();
        for (int i = totalChildren - 1; i >= totalChildren - ShowCount; i--)
        {
            if (i < 0)
                break;

            VisualElement card = StockPanel[i];
            card.style.left = RowAddX * index;
            card.style.top = 0;
            card.style.visibility = Visibility.Hidden;

            data = card.userData as CardDataContainer;
            data.FontCard.style.visibility = Visibility.Visible;
            data.BindEvents();

            WastePanel.Add(card);
            recordDatas.Add(data);

            index++;
        }

        recordDatas.Reverse();
        SetRecordData(recordDatas, null);
        FreshWasteCard();
    }

    /// <summary>
    /// 檢測放置目標
    /// </summary>
    private void CheckDropTarget(CardDataContainer data)
    {
        foreach (var dragCard in data.DragCards)
        {
            dragCard.FontCard.pickingMode = PickingMode.Ignore;
        }

        // 取得滑鼠放開位置下方的元素
        VisualElement target = Root.panel.Pick(data.BackCard.worldBound.center);
        CardDataContainer targetData = target?.parent?.userData != null ? target?.parent?.userData as CardDataContainer : null;

        // 目標容器
        VisualElement tableauPanel = null;
        foreach (var tableau in TableauPanels)
        {
            if (tableau.Contains(target))
            {
                tableauPanel = tableau;
                break;
            }
        }

        if (FoundationPanels.Contains(target) && target.childCount == 0 && data.SkinData.Rank == RankEnum.Ace)
        {
            // 放置在空的結算區

            target.Add(data.BackCard);
            data.BackCard.style.left = 0;
            data.BackCard.style.top = 0;

            List<CardDataContainer> wasteRecord = FreshWasteCard();
            List<CardDataContainer> recordDatas = new(wasteRecord);
            recordDatas.Add(data);

            CardDataContainer openCard = OpenNextCard(data, tableauPanel);
            SetRecordData(recordDatas, openCard);
            InitPickMode(data);
        }
        else if(TableauPanels.Contains(target) && target.childCount == 0 && data.SkinData.Rank == RankEnum.King)
        {
            // 放置在空的操作區

            for (int i = 0; i < data.DragCards.Count; i++)
            {
                CardDataContainer cardDataContainer = data.DragCards[i];

                target.Add(cardDataContainer.BackCard);
                cardDataContainer.BackCard.style.left = 0;
                cardDataContainer.BackCard.style.top = ColumnAddY * i;
            }

            List<CardDataContainer> wasteRecord = FreshWasteCard();
            List<CardDataContainer> recordDatas = new(wasteRecord);
            recordDatas.AddRange(data.DragCards);

            CardDataContainer openCard = OpenNextCard(data, tableauPanel);
            SetRecordData(recordDatas, openCard);
            InitPickMode(data);
        }
        else if(targetData != null)
        {
            // 放置在其他牌

            bool isTop = TopBox.Contains(target);
            bool isBottom = BottomBox.Contains(target);

            if (isBottom)
            {
                // 目標在操作區

                if (data.SkinData.SuitColor != targetData.SkinData.SuitColor
                    && (int)targetData.SkinData.Rank == (int)data.SkinData.Rank + 1)
                {
                    if (tableauPanel != null && target.parent == tableauPanel.Children().Last())
                    {
                        for (int i = 0; i < data.DragCards.Count; i++)
                        {
                            CardDataContainer cardDataContainer = data.DragCards[i];

                            tableauPanel.Add(cardDataContainer.BackCard);
                            cardDataContainer.BackCard.style.left = 0;
                            cardDataContainer.BackCard.style.top = target.parent.resolvedStyle.top + (ColumnAddY * (i + 1));
                        }

                        List<CardDataContainer> wasteRecord = FreshWasteCard();
                        List<CardDataContainer> recordDatas = new(wasteRecord);
                        recordDatas.AddRange(data.DragCards);

                        CardDataContainer openCard = OpenNextCard(data, tableauPanel);
                        SetRecordData(recordDatas, openCard);
                        InitPickMode(data);
                        return;
                    }
                    else
                    {
                        // 沒碰到任何結算區，返回原位
                        data.GoBack();
                    }
                }
                else
                {
                    data.GoBack();
                }
            }
            else if(isTop && data.DragCards.Count == 1)
            {
                // 目標卡在結算區

                VisualElement foundationPanels = null;
                foreach (var foundation in FoundationPanels)
                {
                    if (foundation.Contains(target))
                    {
                        foundationPanels = foundation;
                        break;
                    }
                }

                if(data.SkinData.Suit == targetData.SkinData.Suit
                    && (int)targetData.SkinData.Rank == (int)data.SkinData.Rank - 1)
                {
                    foundationPanels.Add(data.BackCard);
                    data.BackCard.style.left = 0;
                    data.BackCard.style.top = 0;

                    List<CardDataContainer> wasteRecord = FreshWasteCard();
                    List<CardDataContainer> recordDatas = new(wasteRecord);
                    recordDatas.Add(data);

                    CardDataContainer openCard = OpenNextCard(data, tableauPanel);
                    SetRecordData(recordDatas, openCard);
                    InitPickMode(data);
                    JudgeWin();
                    return;
                }
                else
                {
                    data.GoBack();
                }
            }
            else
            {
                data.GoBack();
            }
        }
        else
        {
            data.GoBack();
        }
    }

    /// <summary>
    /// 檢測雙擊
    /// </summary>
    private bool CheckDoubleClick(CardDataContainer data)
    {
        foreach (var foundation in FoundationPanels)
        {
            if (foundation.Contains(data.BackCard))
            {
                // 已在結算區返回
                return false;
            }
        }

        if (data.SkinData.Rank == RankEnum.Ace)
        {
            // Ace直接找結算區空位

            foreach (var foundation in FoundationPanels)
            {
                if (foundation.childCount == 0)
                {
                    foundation.Add(data.BackCard);
                    data.BackCard.style.left = 0;
                    data.BackCard.style.top = 0;

                    List<CardDataContainer> wasteRecord = FreshWasteCard();
                    List<CardDataContainer> recordDatas = new(wasteRecord);
                    recordDatas.Add(data);

                    CardDataContainer openCard = OpenNextCard(data, foundation);
                    SetRecordData(recordDatas, openCard);
                    return true;
                }
            }
        }
        else if(data.SkinData.Rank == RankEnum.King && !ChechFoundationPanels(data))
        {
            // King尋找操作區空位

            List<VisualElement> followCards = new();
            int index = data.BackCard.parent.IndexOf(data.BackCard);
            for (int i = index; i < data.BackCard.parent.childCount; i++)
            {
                followCards.Add(data.BackCard.parent[i]);
            }

            foreach (var tableau in TableauPanels)
            {
                if (tableau.childCount == 0)
                {
                    List<CardDataContainer> cardDataContainers = new();

                    for (int i = 0; i < followCards.Count; i++)
                    {
                        tableau.Add(followCards[i]);
                        followCards[i].style.left = 0;
                        followCards[i].style.top = ColumnAddY * i;

                        CardDataContainer cardDataContainer = followCards[i].userData as CardDataContainer;
                        cardDataContainers.Add(cardDataContainer);
                    }

                    List<CardDataContainer> wasteRecord = FreshWasteCard();
                    cardDataContainers.AddRange(wasteRecord);

                    CardDataContainer openCard = OpenNextCard(data, tableau);
                    SetRecordData(cardDataContainers, openCard);
                    return true;
                }
            }
        }
        else
        {
            // 尋找結算區同花色的區域

            return ChechFoundationPanels(data);
        }

        return false;
    }

    /// <summary>
    /// 檢測是否可放置在結算區
    /// </summary>
    private bool ChechFoundationPanels(CardDataContainer data)
    {
        foreach (var foundation in FoundationPanels)
        {
            if (foundation.childCount > 0)
            {
                VisualElement lastCard = foundation.Children().Last();
                CardDataContainer lastCardData = lastCard.userData as CardDataContainer;
                if (lastCardData != null
                    && lastCardData.SkinData.Suit == data.SkinData.Suit
                    && lastCardData.SkinData.Rank == data.SkinData.Rank - 1)
                {
                    foundation.Add(data.BackCard);
                    data.BackCard.style.left = 0;
                    data.BackCard.style.top = 0;

                    List<CardDataContainer> wasteRecord = FreshWasteCard();
                    List<CardDataContainer> recordDatas = new(wasteRecord);
                    recordDatas.Add(data);

                    CardDataContainer openCard = OpenNextCard(data, foundation);
                    SetRecordData(recordDatas, openCard);
                    JudgeWin();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 重製點擊模式
    /// </summary>
    private void InitPickMode(CardDataContainer data)
    {
        foreach (var dragCard in data.DragCards)
        {
            dragCard.FontCard.pickingMode = PickingMode.Position;
        }
    }

    /// <summary>
    /// 發牌區重新整理
    /// </summary>
    /// <param name="data"></param>
    private List<CardDataContainer> FreshWasteCard()
    {
        List<CardDataContainer> recordDatas = new();

        // 重新排列
        int totalChildren = WastePanel.childCount;

        int offsetIndex = ShowCount - Mathf.Min(totalChildren, ShowCount);
        int index = ShowCount - offsetIndex - 1;
        for (int i = totalChildren - 1; i >= totalChildren - ShowCount; i--)
        {
            if (i < 0)
                break;

            VisualElement card = WastePanel[i];
            CardDataContainer cardData = card.userData as CardDataContainer;
            cardData.OriginalParent = WastePanel;
            cardData.StartPosition = new(card.style.left.value.value, card.style.top.value.value);

            card.style.left = RowAddX * index;
            card.style.top = 0;

            recordDatas.Add(card.userData as CardDataContainer);

            index--;
        }

        recordDatas.Reverse();
        return recordDatas;
    }

    /// <summary>
    /// 翻開操作區下一張牌
    /// </summary>
    private CardDataContainer OpenNextCard(CardDataContainer data, VisualElement tableauPanel)
    {
        if (data.OriginalParent != tableauPanel && TableauPanels.Contains(data.OriginalParent) && data.OriginalParent.childCount > 0)
        {
            VisualElement openCard = data.OriginalParent.Children().Last();
            CardDataContainer openCardData = openCard?.userData as CardDataContainer;
            if (openCardData != null)
            {
                openCardData.BackCard.style.visibility = Visibility.Hidden;
                openCardData.FontCard.style.visibility = Visibility.Visible;
                openCardData.FontCard.pickingMode = PickingMode.Position;
                openCardData.BindEvents();

                return openCardData;
            }
        }

        return null;
    }

    /// <summary>
    /// 判斷獲勝
    /// </summary>
    private void JudgeWin()
    {
        foreach (var foundation in FoundationPanels)
        {
            if(foundation.childCount != 13)
            {
                return;
            }
        }

        Debug.Log("Win");

        AddressableManager.Instance.LoadAssets(
            viewType: ViewEnum.WinView,
            callback: (obj) =>
            {
                WinView winView = obj.GetComponent<WinView>();
                winView.SetData(() =>
                {
                    CreateStockPokers(DefaultStyle, true);
                });
            });
    }

    #region 操作記錄

    /// <summary>
    /// 記錄操作資料
    /// </summary>
    private void SetRecordData(List<CardDataContainer> data, CardDataContainer openCard)
    {
        List<RecordCardData> recordCardDatas = new();

        foreach (var card in data)
        {
            RecordCardData recordCardData = new();
            recordCardData.Card = card.BackCard;
            recordCardData.PrePosition = card.StartPosition;
            recordCardData.PreContainer = card.OriginalParent;

            recordCardDatas.Add(recordCardData);
        }
               
        RecordData recordData = new();
        recordData.RecordCardDatas = recordCardDatas;
        recordData.OpenCard = openCard;

        StepRecordDatas.Push(recordData);
    }

    /// <summary>
    /// 執行上一步
    /// </summary>
    private void DoPreStep()
    {
        if (StepRecordDatas.Count == 0)
            return;

        // 卡片位置回復
        RecordData recordData = StepRecordDatas.Pop();
        foreach (var cardData in recordData.RecordCardDatas)
        {
            VisualElement card = cardData.Card;

            cardData.PreContainer.Add(card);
            card.style.left = cardData.PrePosition.x;
            card.style.top = cardData.PrePosition.y;

            // 返回牌堆
            if(cardData.PreContainer == StockPanel)
            {
                CardDataContainer cardDataContainer = card.userData as CardDataContainer;
                cardDataContainer.BackCard.style.visibility = Visibility.Visible;
                cardDataContainer.FontCard.style.visibility = Visibility.Hidden;
            }
        }

        // 復原翻開的卡
        if(recordData.OpenCard != null)
        {
            recordData.OpenCard.BackCard.style.visibility = Visibility.Visible;
            recordData.OpenCard.FontCard.style.visibility = Visibility.Hidden;
            recordData.OpenCard.FontCard.pickingMode = PickingMode.Ignore;
            recordData.OpenCard.UnbindEvents();
        }

        FreshWasteCard();
    }

    #endregion
}