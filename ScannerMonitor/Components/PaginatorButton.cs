namespace ScannerMonitor.Components
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    /**
     * Component that will be added onto the paginator buttons. Gives the hover effect and clicking function.
     */
    public class PaginatorButton : OnScreenButton
    {
        public int AmountToChangePageBy = 1;
        public Text text;

        public override void Start()
        {
            text = GetComponent<Text>();
            HoverText = text.text;
        }

        public override void OnEnable()
        {
            if (text != null)
            {
                text.color = Color.white;
            }
        }

        public override void OnDisable()
        {
            if (text != null)
            {
                text.color = Color.white;
            }
            base.OnDisable();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsHovered)
            {
                text.color = new Color(0.07f, 0.38f, 0.7f, 1f);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            text.color = Color.white;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (IsHovered)
            {
#if !UNITY_EDITOR
                ScannerMonitorDisplay.ChangePageBy(AmountToChangePageBy);
#endif
            }
        }
    }
}
