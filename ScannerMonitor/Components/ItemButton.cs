namespace ScannerMonitor.Components
{
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class ItemButton : OnScreenButton
    {
        public TechType type = TechType.None;

        [AssertNotNull]
        public uGUI_Icon icon;

        [AssertNotNull]
        public MapRoomFunctionality mapRoomFunctionality;

        public TechType Type
        {
            set
            {
                HoverText = Language.main?.Get(value) ?? value.AsString();
                type = value;
            }
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            if(InInteractionRange() && (IsPointerInside || this.ScannerMonitorDisplay.Buttons.Any(b=>b.IsPointerInside)))
                base.OnDeselect(eventData);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if(InInteractionRange())
            {
                if(IsPointerInside)
                {
                    this.ScannerMonitorDisplay.Buttons.ForEach(b => { if(b != this) b.OnDeselect(null); });
                    if(this.HasSelection)
                    {
                        mapRoomFunctionality.StartScanning(TechType.None);
                        this.OnDeselect(null);
                    }
                    else
                    {
                        OnSelect(null);
                    }
                    return;
                }
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            if(InInteractionRange())
            {
                mapRoomFunctionality.StartScanning(type);
                base.OnSelect(eventData);
            }
        }
    }
}
