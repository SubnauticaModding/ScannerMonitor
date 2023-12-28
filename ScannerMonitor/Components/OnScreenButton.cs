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
        protected string HoverText;
        public float distance;

        public bool IsPointerInside { get; private set; }
        public bool HasSelection { get; private set; }

        public virtual void Update()
        {
            var inInteractionRange = InInteractionRange();

            if (this.IsPointerInside && inInteractionRange)
            {
                ScannerMonitorDisplay.ResetIdleTimer();
                HandReticle.main?.SetTextRaw(HandReticle.TextType.Hand, HoverText);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (InInteractionRange())
            {
                this.IsPointerInside = true;
                ScannerMonitorDisplay.ResetIdleTimer();
                base.OnPointerEnter(eventData);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if(InInteractionRange())
            {
                this.IsPointerInside = false;
                ScannerMonitorDisplay.ResetIdleTimer();
                base.OnPointerExit(eventData);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if(InInteractionRange() && this.IsPointerInside)
            {
                ScannerMonitorDisplay.ResetIdleTimer();
                base.OnPointerDown(eventData);
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            this.HasSelection = true;
            base.OnSelect(eventData);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            this.HasSelection = false;
            base.OnDeselect(eventData);
        }

        protected bool InInteractionRange()
        {
            distance = Mathf.Abs(Vector3.Distance(gameObject.transform.position, Player.main?.transform.position ?? gameObject.transform.position));
            return distance <= 2.5f;
        }
    }
}
