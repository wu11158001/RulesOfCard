using UnityEngine;
using UnityEngine.UIElements;
using System;

public class CardDataContainer
{
    public PokerSkinData SkinData;
    public EventCallback<PointerDownEvent> PointerDownEvent;
    public EventCallback<PointerMoveEvent> PointerMoveEvent;
    public EventCallback<PointerUpEvent> PointerUpEvent;

    VisualElement DragPanel;
    Action<VisualElement> CheckDropTarget;

    // 拖曳參數
    private VisualElement Card;
    private bool IsDragging = false;
    private VisualElement OriginalParent;
    private Vector2 StartPosition;
    private Vector2 DragOffset;

    public CardDataContainer(PokerSkinData data, VisualElement card, VisualElement dragPanel, Action<VisualElement> checkDropTarget)
    {
        this.SkinData = data;
        this.Card = card;
        this.DragPanel = dragPanel;
        this.CheckDropTarget = checkDropTarget;

        PointerDownEvent = evt => OnPointerDown(evt, card);
        PointerMoveEvent = evt => OnPointerMove(evt, card);
        PointerUpEvent = evt => OnPointerUp(evt, card);
    }

    /// <summary>
    /// 監聽拖曳事件
    /// </summary>
    public void BindEvents()
    {
        UnbindEvents();

        this.Card.RegisterCallback(PointerDownEvent);
        this.Card.RegisterCallback(PointerMoveEvent);
        this.Card.RegisterCallback(PointerUpEvent);
    }

    /// <summary>
    /// 移除拖曳事件
    /// </summary>
    public void UnbindEvents()
    {
        this.Card.UnregisterCallback(PointerDownEvent);
        this.Card.UnregisterCallback(PointerMoveEvent);
        this.Card.UnregisterCallback(PointerUpEvent);
    }

    /// <summary>
    /// 返回原位置
    /// </summary>
    public void GoBack()
    {
        OriginalParent.Add(this.Card);
        this.Card.style.left = StartPosition.x;
        this.Card.style.top = StartPosition.y;
    }

    /// <summary>
    /// 開始拖曳
    /// </summary>
    private void OnPointerDown(PointerDownEvent evt, VisualElement card)
    {
        IsDragging = true;
        OriginalParent = card.parent;

        StartPosition = new(card.style.left.value.value, card.style.top.value.value);
        Vector2 worldPos = card.worldBound.position;
        Vector2 localPos = DragPanel.WorldToLocal(worldPos);
        Vector2 cardLocalPos = card.WorldToLocal(evt.position);
        DragOffset = cardLocalPos;

        card.style.position = Position.Absolute;
        card.style.left = localPos.x;
        card.style.top = localPos.y;

        DragPanel.Add(card);

        VisualElement front = card.Q<VisualElement>("FrontCard");
        if (front != null)
        {
            front.pickingMode = PickingMode.Ignore;
        }

        card.pickingMode = PickingMode.Ignore;
        card.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    /// <summary>
    /// 拖曳移動
    /// </summary>
    private void OnPointerMove(PointerMoveEvent evt, VisualElement card)
    {
        if (!IsDragging || !card.HasPointerCapture(evt.pointerId)) return;

        Vector2 localMousePos = DragPanel.WorldToLocal(evt.position);

        card.style.left = localMousePos.x - DragOffset.x;
        card.style.top = localMousePos.y - DragOffset.y;

        evt.StopPropagation();
    }

    /// <summary>
    /// 拖曳結束
    /// </summary>
    private void OnPointerUp(PointerUpEvent evt, VisualElement card)
    {
        if (!IsDragging || !card.HasPointerCapture(evt.pointerId)) return;

        IsDragging = false;
        card.ReleasePointer(evt.pointerId);

        this.CheckDropTarget?.Invoke(card);
    }
}
