using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

public class CardDataContainer
{
    public PokerSkinData SkinData;
    public EventCallback<PointerDownEvent> PointerDownEvent;
    public EventCallback<PointerMoveEvent> PointerMoveEvent;
    public EventCallback<PointerUpEvent> PointerUpEvent;

    VisualElement DragPanel;
    VisualElement WastePanel;
    Action<CardDataContainer> CheckDropTargetAction;
    Func<CardDataContainer, bool> CheckDoubleClickAction;
    Action<bool> AnyDragEventChengeAction;

    public VisualElement BackCard;
    public VisualElement FontCard;
    public VisualElement OriginalParent;
    public List<CardDataContainer> DragCards = new();

    // 拖曳
    public bool IsAnyDragging = false;
    private bool IsDragging = false;
    public Vector2 StartPosition;
    private Vector2 DragOffset;

    // 點擊判斷
    private float ClickTime;
    private float ChechTime = 0.25f;

    public CardDataContainer(PokerSkinData data, VisualElement backCard, VisualElement fontCard, VisualElement dragPanel, VisualElement wastePanel,
        Action<CardDataContainer> checkDropTargetAction, Func<CardDataContainer, bool> checkDoubleClickAction, Action<bool> anyDragEventChengeAction)
    {
        this.SkinData = data;
        this.BackCard = backCard;
        this.FontCard = fontCard;
        this.DragPanel = dragPanel;
        this.WastePanel = wastePanel;
        this.CheckDropTargetAction = checkDropTargetAction;
        this.CheckDoubleClickAction = checkDoubleClickAction;
        this.AnyDragEventChengeAction = anyDragEventChengeAction;

        PointerDownEvent = evt => OnPointerDown(evt);
        PointerMoveEvent = evt => OnPointerMove(evt);
        PointerUpEvent = evt => OnPointerUp(evt);

        this.BackCard.RegisterCallback<PointerEnterEvent>(OnPointEnterEvent);
        this.BackCard.RegisterCallback<PointerLeaveEvent>(OnPointLeaveEvent);
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

    #region 拖曳

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

    #region 拖曳事件

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
    /// 滑鼠點擊
    /// </summary>
    private void OnPointerDown(PointerDownEvent evt)
    {
        VisualElement parent = this.BackCard.parent;
        if (parent == null) return;

        DragCards.Clear();

        if (parent == WastePanel)
        {
            // 牌在發牌區

            bool isLastCard = parent.Children().Last() == this.BackCard;

            // 最後一張才能移動
            if (isLastCard)
            {
                DragOffset = this.BackCard.WorldToLocal(evt.position);

                CardDataContainer data = this.BackCard.userData as CardDataContainer;
                SetDragCard(data, parent);
                this.BackCard.CapturePointer(evt.pointerId);
            }
        }
        else
        {
            // 牌在操作區或結算區

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

                SetDragCard(data, parent);
            }

            this.BackCard.CapturePointer(evt.pointerId);
        }

        ClickTime = Time.time;
        evt.StopPropagation();
    }

    /// <summary>
    /// 設置拖曳卡片
    /// </summary>
    private void SetDragCard(CardDataContainer data, VisualElement parent)
    {
        VisualElement backCard = data.BackCard;

        this.AnyDragEventChengeAction?.Invoke(true);

        data.IsDragging = true;
        data.OriginalParent = parent;
        data.StartPosition = new Vector2(backCard.style.left.value.value, backCard.style.top.value.value);

        // 轉換座標到拖曳層
        Vector2 cLocalInDragPanel = DragPanel.WorldToLocal(backCard.worldBound.position);
        backCard.style.position = Position.Absolute;
        backCard.style.left = cLocalInDragPanel.x;
        backCard.style.top = cLocalInDragPanel.y;

        DragPanel.Add(backCard);
        data.BackCard.pickingMode = PickingMode.Ignore;
        data.FontCard.pickingMode = PickingMode.Ignore;

        DragCards.Add(data);
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
        if (!this.BackCard.HasPointerCapture(evt.pointerId)) return;

        this.AnyDragEventChengeAction?.Invoke(false);

        bool? isFind = false;
        float currentTime = Time.time;
        if (currentTime - ClickTime <= ChechTime)
        {
            isFind = CheckDoubleClickAction?.Invoke(this);
        }

        List<CardDataContainer> cardsToProcess = new(DragCards);
        foreach (var cData in cardsToProcess)
        {
            cData.FontCard.pickingMode = PickingMode.Position;
            cData.IsDragging = false;
        }

        this.BackCard.ReleasePointer(evt.pointerId);

        if(isFind == false)
        {
            this.CheckDropTargetAction?.Invoke(this);
        }            
    }

    #endregion

    #region 懸停事件

    /// <summary>
    /// 滑鼠進入
    /// </summary>
    private void OnPointEnterEvent(PointerEnterEvent evt)
    {
        VisualElement parent = this.BackCard.parent;
        if (parent == null || IsAnyDragging) return;

        // 找出下方的所有牌透明
        int index = parent.IndexOf(this.BackCard);
        for (int i = index + 1; i < parent.childCount; i++)
        {
            CardDataContainer cardData = parent[i].userData as CardDataContainer;

            cardData.BackCard.style.opacity = 0.2f;
        }
    }

    /// <summary>
    /// 滑鼠離開
    /// </summary>
    private void OnPointLeaveEvent(PointerLeaveEvent evt)
    {
        VisualElement parent = this.BackCard.parent;
        if (parent == null) return;

        // 找出下方的所有牌透明回復
        List<VisualElement> followCards = new();
        int index = parent.IndexOf(this.BackCard);
        for (int i = index; i < parent.childCount; i++)
        {
            CardDataContainer cardData = parent[i].userData as CardDataContainer;

            cardData.BackCard.style.opacity = 1;
        }
    }

    #endregion
}
