namespace ScannerMonitor.Components
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /**
     * Component that buttons on the resource monitor will inherit from. Handles working on whether something is hovered via IsHovered as well as interaction text.
     */
    public abstract class OnScreenButton : MonoBehaviour
    {
        public ScannerMonitorDisplay ScannerMonitorDisplay;
        protected bool IsHovered;
        protected string HoverText;
        public bool isHoveredOutOfRange;

        public virtual void OnDisable()
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
#if SN1
                HandReticle.main?.SetInteractTextRaw(HoverText, "");
#elif BZ
                HandReticle.main?.SetTextRaw(HandReticle.TextType.Hand, HoverText);
#endif
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

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (InInteractionRange())
            {
                IsHovered = true;
            }

            isHoveredOutOfRange = true;
            ScannerMonitorDisplay.ResetIdleTimer();
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            isHoveredOutOfRange = false;
            ScannerMonitorDisplay.ResetIdleTimer();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            ScannerMonitorDisplay.ResetIdleTimer();
        }

        protected bool InInteractionRange()
        {
            return Mathf.Abs(Vector3.Distance(gameObject.transform.position, Player.main?.transform.position ?? gameObject.transform.position)) <= 2.5f;
        }
    }
}
