namespace ScannerMonitor.Components
{
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    /**
     * Component that will be added onto the paginator buttons. Gives the hover effect and clicking function.
     */
    public class PaginatorButton : OnScreenButton
    {
        public int AmountToChangePageBy = 1;

        [AssertNotNull]
        public Text text;

        protected override void Start()
        {
            HoverText = text.text;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if(InInteractionRange() && base.IsPointerInside)
            {
                ScannerMonitorDisplay.ChangePageBy(AmountToChangePageBy);
                base.OnPointerDown(eventData);
            }
        }
    }
}
