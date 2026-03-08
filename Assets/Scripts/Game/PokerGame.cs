using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class PokerGame : MonoBehaviour
{
    [SerializeField] PokerCollection pokerCollection;

    // 拖曳撲克牌
    private VisualElement OriginalParent;
    private Vector2 StartMousePos;
    private Vector2 StartElementPos;
    private bool IsDragging = false;
    private Vector2 ClickOffset;

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

    /// <summary>
    /// 牌堆點擊
    /// </summary>
    private void OnStockClick(ClickEvent evt)
    {
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
                    frontCard.style.visibility = Visibility.Hidden;
                }

                card.style.left = StockAdd.x * stockIndex;
                card.style.top = StockAdd.y * stockIndex;

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

        DragPoker(topCard);
        WastePanel.Add(topCard);
    }

    /// <summary>
    /// 拖曳撲克牌
    /// </summary>
    private void DragPoker(VisualElement card)
    {
        card.RegisterCallback<PointerDownEvent>(evt =>
        {
            OriginalParent = card.parent;
            IsDragging = true;
            StartMousePos = evt.position;

            // 1. 獲取卡牌當前在視窗中的世界座標
            Vector2 worldPos = card.worldBound.position;

            // 2. 將世界座標轉換為 DragPanel 內部的相對座標
            Vector2 localPos = DragPanel.WorldToLocal(worldPos);

            Vector2 cardLocalPos = card.WorldToLocal(evt.position);
            ClickOffset = cardLocalPos;

            // 3. 設定卡牌的樣式，改為絕對定位 (Position: Absolute)
            // 這樣它才能脫離原本的 Flex 佈局自由移動
            card.style.position = Position.Absolute;
            card.style.left = localPos.x;
            card.style.top = localPos.y;

            // 4. 移動到 DragPanel
            DragPanel.Add(card);

            card.pickingMode = PickingMode.Ignore;
            card.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        card.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!IsDragging || !card.HasPointerCapture(evt.pointerId)) return;

            // 1. 將當前滑鼠的絕對位置轉換為 DragPanel 的本地座標
            // 這一步能確保座標基準永遠是正確的
            Vector2 localMousePos = DragPanel.WorldToLocal(evt.position);

            // 2. 減去原本點擊時的偏移量 (Offset)
            // 這樣卡牌就不會瞬間跳到滑鼠左上角，而是保持你當初點擊的位置
            card.style.left = localMousePos.x - ClickOffset.x;
            card.style.top = localMousePos.y - ClickOffset.y;

            evt.StopPropagation();
        });

        card.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!IsDragging || !card.HasPointerCapture(evt.pointerId)) return;

            IsDragging = false;
            card.ReleasePointer(evt.pointerId);
            card.pickingMode = PickingMode.Position;

            CheckDropTarget(card);
        });
    }

    /// <summary>
    /// 檢測卡撲克牌放置目標
    /// </summary>
    private void CheckDropTarget(VisualElement card)
    {
        // 取得滑鼠放開位置下方的元素
        VisualElement target = Root.panel.Pick(card.worldBound.center);

        OriginalParent.Add(card);
        card.style.left = StartElementPos.x;
        card.style.top = StartElementPos.y;
    }
}