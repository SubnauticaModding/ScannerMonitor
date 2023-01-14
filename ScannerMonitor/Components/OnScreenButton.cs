namespace ScannerMonitor.Components
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /**
     * Component that buttons on the resource monitor will inherit from. Handles working on whether something is hovered via IsHovered as well as interaction text.
     */
    public abstract class OnScreenButton : Selectable
    {
        public ScannerMonitorDisplay ScannerMonitorDisplay;
        protected bool IsHovered;
        protected string HoverText;
        public bool isHoveredOutOfRange;

        public override void OnDisable()
        {
            IsHovered = false;
            isHoveredOutOfRange = false;
        }

        public virtual void Update()
        {
            var inInteractionRange = InInteractionRange();

            if (IsHovered && inInteractionRange)
            {
#if !UNITY_EDITOR
                HandReticle.main?.SetTextRaw(HandReticle.TextType.Hand, HoverText);
#else
                Debug.Log(HoverText);
#endif
            }

            if (IsHovered && inInteractionRange == false)
            {
                IsHovered = false;
            }

            if (IsHovered == false && isHoveredOutOfRange && inInteractionRange)
            {
                IsHovered = true;
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerEnter!");
            if (InInteractionRange())
            {
                IsHovered = true;
            }

            isHoveredOutOfRange = true;
#if !UNITY_EDITOR
            ScannerMonitorDisplay.ResetIdleTimer();
#endif
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerExit!");
            IsHovered = false;
            isHoveredOutOfRange = false;
#if !UNITY_EDITOR
            ScannerMonitorDisplay.ResetIdleTimer();
#endif
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerDown!");
#if !UNITY_EDITOR
            ScannerMonitorDisplay.ResetIdleTimer();
#endif
        }

        protected bool InInteractionRange()
        {
            return Mathf.Abs(Vector3.Distance(gameObject.transform.position, Player.main?.transform.position ?? gameObject.transform.position)) <= 2.5f;
        }
    }
}
