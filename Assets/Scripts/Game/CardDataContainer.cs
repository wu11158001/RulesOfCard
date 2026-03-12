using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class CardDataContainer
{
    public PokerSkinData SkinData;
    public EventCallback<PointerDownEvent> PointerDownEvent;
    public EventCallback<PointerMoveEvent> PointerMoveEvent;
    public EventCallback<PointerUpEvent> PointerUpEvent;

    VisualElement DragPanel;
    Action<CardDataContainer> CheckDropTargetAction;
    Action<CardDataContainer> CheckDoubleClickAction;

    public VisualElement BackCard;
    public VisualElement FontCard;
    public VisualElement OriginalParent;
    public List<CardDataContainer> DragCards = new();

    // 拖曳
    private bool IsDragging = false;
    public Vector2 StartPosition;
    private Vector2 DragOffset;

    // 雙擊
    private float lastClickTime = 0f;
    private const float DoubleClickThreshold = 0.25f;

    public CardDataContainer(PokerSkinData data, VisualElement backCard, VisualElement fontCard, VisualElement dragPanel, 
        Action<CardDataContainer> checkDropTargetAction, Action<CardDataContainer> checkDoubleClickAction)
    {
        this.SkinData = data;
        this.BackCard = backCard;
        this.FontCard = fontCard;
        this.DragPanel = dragPanel;
        this.CheckDropTargetAction = checkDropTargetAction;
        this.CheckDoubleClickAction = checkDoubleClickAction;

        PointerDownEvent = evt => OnPointerDown(evt);
        PointerMoveEvent = evt => OnPointerMove(evt);
        PointerUpEvent = evt => OnPointerUp(evt);
    }

    /// <summary>
    /// 監聽拖曳事件
    /// </summary>
    public void BindEvents()
    {
        UnbindEvents();

        this.BackCard.RegisterCallback(PointerDownEvent);
        this.BackCard.RegisterCallback(PointerMoveEvent);
        this.BackCard.RegisterCallback(PointerUpEvent);
    }

    /// <summary>
    /// 移除拖曳事件
    /// </summary>
    public void UnbindEvents()
    {
        this.BackCard.UnregisterCallback(PointerDownEvent);
        this.BackCard.UnregisterCallback(PointerMoveEvent);
        this.BackCard.UnregisterCallback(PointerUpEvent);
    }

    /// <summary>
    /// 返回原位置
    /// </summary>
    public void GoBack()
    {
        // 跟隨牌與主牌返回原位置
        foreach (var cData in DragCards)
        {
            cData.DoGoBack();
        }     
    }

    public void DoGoBack()
    {
        OriginalParent.Add(this.BackCard);
        this.BackCard.style.left = StartPosition.x;
        this.BackCard.style.top = StartPosition.y;

        FontCard.pickingMode = PickingMode.Position;
    }

    #region Drag

    /// <summary>
    /// 拖曳移動
    /// </summary>
    private void DragMove(PointerMoveEvent evt, VisualElement card)
    {
        // 只有捕捉了指標的那張牌才驅動所有牌移動
        if (!IsDragging || !card.HasPointerCapture(evt.pointerId)) return;

        Vector2 localMousePos = DragPanel.WorldToLocal(evt.position);

        // 計算第一張牌應該在的位置
        float targetX = localMousePos.x - DragOffset.x;
        float targetY = localMousePos.y - DragOffset.y;

        // 計算位移差 (Delta)
        float deltaX = targetX - card.style.left.value.value;
        float deltaY = targetY - card.style.top.value.value;

        // 讓整疊牌一起移動相同的距離
        foreach (var cData in DragCards)
        {
            cData.BackCard.style.left = cData.BackCard.style.left.value.value + deltaX;
            cData.BackCard.style.top = cData.BackCard.style.top.value.value + deltaY;
        }

        evt.StopPropagation();
    }

    #endregion

    #region PointEvent

    /// <summary>
    /// 滑鼠點擊
    /// </summary>
    private void OnPointerDown(PointerDownEvent evt)
    {
        float currentTime = Time.time;

        // 檢查兩次點擊間隔
        if (currentTime - lastClickTime < DoubleClickThreshold)
        {
            // 觸發雙擊
            lastClickTime = 0f;
            CheckDoubleClickAction?.Invoke(this);
        }
        else
        {
            // 單擊
            lastClickTime = currentTime;

            VisualElement parent = this.BackCard.parent;
            if (parent == null) return;

            DragCards.Clear();

            // 找出包含自己及下方的所有牌
            List<VisualElement> followCards = new();
            int index = parent.IndexOf(this.BackCard);
            for (int i = index; i < parent.childCount; i++)
            {
                followCards.Add(parent[i]);
            }

            DragOffset = this.BackCard.WorldToLocal(evt.position);

            foreach (var c in followCards)
            {
                CardDataContainer data = c.userData as CardDataContainer;

                data.IsDragging = true;
                data.OriginalParent = parent;
                data.StartPosition = new Vector2(c.style.left.value.value, c.style.top.value.value);

                // 轉換座標到拖曳層
                Vector2 cLocalInDragPanel = DragPanel.WorldToLocal(c.worldBound.position);
                c.style.position = Position.Absolute;
                c.style.left = cLocalInDragPanel.x;
                c.style.top = cLocalInDragPanel.y;

                DragPanel.Add(c);
                data.BackCard.pickingMode = PickingMode.Ignore;
                data.FontCard.pickingMode = PickingMode.Ignore;

                DragCards.Add(data);
            }

            this.BackCard.CapturePointer(evt.pointerId);
        }
                
        evt.StopPropagation();
    }

    /// <summary>
    /// 滑鼠拖曳移動
    /// </summary>
    private void OnPointerMove(PointerMoveEvent evt)
    {
        foreach (var cData in DragCards)
        {
            cData.DragMove(evt, cData.BackCard);
        }
    }

    /// <summary>
    /// 拖曳結束
    /// </summary>
    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!IsDragging || !this.BackCard.HasPointerCapture(evt.pointerId)) return;

        // 建立一個臨時清單，避免在迴圈中修改集合
        List<CardDataContainer> cardsToProcess = new List<CardDataContainer>(DragCards);

        // 統一結束拖曳狀態
        foreach (var cData in cardsToProcess)
        {
            cData.FontCard.pickingMode = PickingMode.Position;
            cData.IsDragging = false;
        }

        this.BackCard.ReleasePointer(evt.pointerId);

        // 只對「第一張牌」執行放置檢測
        this.CheckDropTargetAction?.Invoke(this);
    }

    #endregion
}
