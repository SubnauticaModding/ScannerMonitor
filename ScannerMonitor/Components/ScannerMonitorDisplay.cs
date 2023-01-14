namespace ScannerMonitor.Components
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
#if !UNITY_EDITOR
    using UnityEngine.EventSystems;
    using SMLHelper.Utility;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using Random = UnityEngine.Random;
    using Newtonsoft.Json;
#endif

    /**
    * Component that contains the controller to the view. Majority of the view is set up already on the prefab inside of the unity editor.
    * Handles such things as the paginator, drawing all the items that are on the "current page"
    * Handles the idle screen saver.
    * Handles the welcome animations.
    */
    public class ScannerMonitorDisplay : MonoBehaviour
#if !UNITY_EDITOR
        , IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IProtoEventListener
#endif
    {
        public static readonly float WELCOME_ANIMATION_TIME = 8.5f;
        public static readonly float MAIN_SCREEN_ANIMATION_TIME = 1.2f;
        public int ITEMS_PER_PAGE => 12;
        
        public Dictionary<TechType, GameObject> trackedResourcesDisplayElements;
        public MapRoomFunctionality MapRoomFunctionality;
        private readonly HashSet<TechType> availableTechTypes = new();
        public Constructable Constructable;
        public int currentPage = 1;
        public int maxPage = 1;
        public float idlePeriodLength = 20f;
        public float timeSinceLastInteraction;
        public bool isIdle;
        public float nextColorTransitionCurrentTime;
        public float transitionIdleTime;
        public List<Color> PossibleIdleColors = new()
        {
            new Color(0.07f, 0.38f, 0.70f), // BLUE
            new Color(0.86f, 0.22f, 0.22f), // RED
            new Color(0.22f, 0.86f, 0.22f) // GREEN
        };
        public Color currentColor;
        public Color nextColor;
        public bool isHovered = false;
        public bool isHoveredOutOfRange = false;

        public Vector3 lastScale = Vector3.zero;

        public GameObject CanvasGameObject;
        public Animator animator;
        public GameObject blackCover;
        public GameObject welcomeScreen;
        public GameObject mainScreen;
        public GameObject mainScreensCover;
        public GridLayoutGroup mainScreenItemGrid;
        public GameObject previousPageGameObject;
        public GameObject nextPageGameObject;
        public GameObject pageCounterGameObject;
        public Text pageCounterText;
        public GameObject idleScreen;
        public Image idleScreenTitleBackgroundImage;
        public GameObject itemPrefab;

#if !UNITY_EDITOR
        public void OnEnable()
        {
            if(Constructable.constructed)
            {
                trackedResourcesDisplayElements = new Dictionary<TechType, GameObject>();
                MapRoomFunctionality.storageContainer.container.onAddItem += OnContainerChange;
                MapRoomFunctionality.storageContainer.container.onRemoveItem += OnContainerChange;
                CalculateNewColourTransitionTime();
                CalculateNewIdleTime();
                availableTechTypes.Clear();
                ResourceTrackerDatabase.GetTechTypesInRange(gameObject.transform.position, MapRoomFunctionality.GetScanRange(), availableTechTypes);
                UpdatePaginator();

                StartCoroutine(FinalSetup());
            }
            else
            {
                TurnDisplayOff();
            }
        }

        public void OnDisable()
        {
            MapRoomFunctionality.storageContainer.container.onAddItem -= OnContainerChange;
            MapRoomFunctionality.storageContainer.container.onRemoveItem -= OnContainerChange;
            TurnDisplayOff();
        }
        private void OnContainerChange(InventoryItem item)
        {
            DrawPage(0);
        }

        public IEnumerator FinalSetup()
        {
            animator.enabled = true;
            welcomeScreen.SetActive(false);
            blackCover.SetActive(false);
            mainScreen.SetActive(false);
            animator.Play("Reset");

            yield return new WaitForEndOfFrame();

            animator.Play("Welcome");

            yield return new WaitForSeconds(WELCOME_ANIMATION_TIME);

            animator.Play("ShowMainScreen");
            DrawPage(currentPage);

            yield return new WaitForSeconds(MAIN_SCREEN_ANIMATION_TIME);

            welcomeScreen.SetActive(false);
            blackCover.SetActive(false);
            mainScreen.SetActive(true);
            animator.enabled = false;
        }

        public void TurnDisplayOff()
        {
            StopCoroutine(FinalSetup());
            blackCover?.SetActive(true);
            trackedResourcesDisplayElements?.Clear();
            trackedResourcesDisplayElements = null;
        }

        public void CalculateNewMaxPages()
        {
            var items = ITEMS_PER_PAGE;
            var count = availableTechTypes.Count;
            maxPage = items <= 0 ? 1 : Mathf.CeilToInt((count - 1) / items) + 1;
            if (currentPage > maxPage)
            {
                currentPage = maxPage;
            }
        }

        public void ChangePageBy(int amount)
        {
            DrawPage(currentPage + amount);
        }

        public void DrawPage(int page)
        {
            currentPage = page;
            if (currentPage <= 0)
            {
                currentPage = 1;
            }
            else if (currentPage > maxPage)
            {
                currentPage = maxPage;
            }

            var items = ITEMS_PER_PAGE;
            
            var startingPosition = (currentPage - 1) * items;
            var endingPosition = startingPosition + items;
            availableTechTypes.Clear();
            ResourceTrackerDatabase.GetTechTypesInRange(gameObject.transform.position, MapRoomFunctionality.GetScanRange(), availableTechTypes);
            var count = availableTechTypes.Count;
            if(endingPosition > count)
            {
                endingPosition = count;
            }

            ClearPage();
            if(items > 0)
            {
                var scale = Mathf.Min(200f, 2400f / (endingPosition - startingPosition));
                mainScreenItemGrid.cellSize = Vector2.one*scale;
                var keys = availableTechTypes.ToList();
                keys.Sort(uGUI_MapRoomScanner.CompareByName);
                for(var i = startingPosition; i < endingPosition; i++)
                    CreateAndAddItemDisplay(keys[i]);
            }
            UpdatePaginator();
        }

        public void UpdatePaginator()
        {
            CalculateNewMaxPages();
            pageCounterGameObject.SetActive(maxPage > 1);
            pageCounterText.text = $"Page {currentPage} of {maxPage}";
            previousPageGameObject.SetActive(currentPage != 1);
            nextPageGameObject.SetActive(currentPage != maxPage);
        }

        public void ClearPage()
        {
            for (var i = 0; i < mainScreenItemGrid.transform.childCount; i++)
            {
                Destroy(mainScreenItemGrid.transform.GetChild(i).gameObject);
            }

            trackedResourcesDisplayElements?.Clear();
        }

        public void CreateAndAddItemDisplay(TechType type)
        {
            var itemDisplay = Instantiate(itemPrefab);
            itemDisplay.transform.SetParent(mainScreenItemGrid.transform, false);
            trackedResourcesDisplayElements.Add(type, itemDisplay);

            var itemButton = itemDisplay.GetComponent<ItemButton>();
            itemButton.ScannerMonitorDisplay = this;
            itemButton.mapRoomFunctionality = MapRoomFunctionality;
            itemButton.Type = type;

            var icon = itemDisplay.transform.Find("ItemHolder").gameObject.GetComponent<uGUI_Icon>();
                icon.sprite = SpriteManager.Get(type);
        }

        public void Update()
        {
            if(MapRoomFunctionality.transform.localScale != lastScale)
            {
                lastScale = MapRoomFunctionality.transform.localScale;
                DrawPage(0);
            }

            if (!isIdle && timeSinceLastInteraction < idlePeriodLength)
            {
                timeSinceLastInteraction += Time.deltaTime;
            }

            if (!isIdle && timeSinceLastInteraction >= idlePeriodLength)
            {
                EnterIdleScreen();
            }

            if (!isHovered && isHoveredOutOfRange && InIdleInteractionRange())
            {
                isHovered = true;
                ExitIdleScreen();
            }

            if (isHovered)
            {
                ResetIdleTimer();
            }

            if (!isIdle) return;
            if (nextColorTransitionCurrentTime >= transitionIdleTime)
            {
                nextColorTransitionCurrentTime = 0f;
                for (var i = 0; i < PossibleIdleColors.Count; i++)
                {
                    if (PossibleIdleColors[i] != nextColor) continue;
                    i++;
                    currentColor = nextColor;
                    if (i >= PossibleIdleColors.Count)
                    {
                        i = 0;
                    }
                    nextColor = PossibleIdleColors[i];
                    CalculateNewColourTransitionTime();
                }
            }

            nextColorTransitionCurrentTime += Time.deltaTime;
            idleScreenTitleBackgroundImage.color = Color.Lerp(currentColor, nextColor, nextColorTransitionCurrentTime / transitionIdleTime);
        }

        public bool InIdleInteractionRange()
        {
            return Mathf.Abs(Vector3.Distance(gameObject.transform.position, Player.main?.transform.position ?? gameObject.transform.position)) <= 5f;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerClick!");
            if (isIdle && InIdleInteractionRange())
            {
                ExitIdleScreen();
            }
            
            if (!isIdle)
            {
                ResetIdleTimer();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerEnter!");
            isHoveredOutOfRange = true;
            if (InIdleInteractionRange())
            {
                isHovered = true;
            }

            if (isIdle && InIdleInteractionRange())
            {
                ExitIdleScreen();
            }

            if (!isIdle)
            {
                ResetIdleTimer();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ErrorMessage.AddMessage($"OnPointerExit!");
            isHoveredOutOfRange = false;
            isHovered = false;
            if (isIdle && InIdleInteractionRange())
            {
                ExitIdleScreen();
            }

            if (isIdle == false)
            {
                ResetIdleTimer();
            }
        }

        public void EnterIdleScreen()
        {
            isIdle = true;
            mainScreen.SetActive(false);
            idleScreen.SetActive(true);
        }

        public void ExitIdleScreen()
        {
            isIdle = false;
            ResetIdleTimer();
            CalculateNewIdleTime();
            mainScreen.SetActive(true);
            idleScreen.SetActive(false);
            DrawPage(currentPage);
        }
        
        public void CalculateNewIdleTime()
        {
            idlePeriodLength = 20f + Random.Range(1f, 10f);
        }

        public void ResetIdleTimer()
        {
            timeSinceLastInteraction = 0f;
            DrawPage(currentPage);
        }

        public void CalculateNewColourTransitionTime()
        {
            transitionIdleTime = 2f + Random.Range(0f, 2f);
        }

        public void OnApplicationQuit()
        {
            StopAllCoroutines();
        }
        
        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            var CurrentScanFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), $"{gameObject.GetComponent<PrefabIdentifier>().id}.json");
            var writer = new StreamWriter(CurrentScanFilePath);
            try
            {
                writer.Write(
                    JsonConvert.SerializeObject(
                        MapRoomFunctionality.typeToScan, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            Converters = new List<JsonConverter>
                            {
                            new Main.TechTypeConverter()
                            }
                        })
                    );
                writer.Flush();
                writer.Close();
            }
            catch
            {
                writer.Close();
            }
        }
        
        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            var CurrentScanFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), $"{gameObject.GetComponent<PrefabIdentifier>().id}.json");
            if (!File.Exists(CurrentScanFilePath)) return;
            var reader = new StreamReader(CurrentScanFilePath);
            try
            {
                MapRoomFunctionality.typeToScan = JsonConvert.DeserializeObject<TechType>(
                    reader.ReadToEnd(),
                    new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented,
                        Converters = new List<JsonConverter>
                        {
                            new Main.TechTypeConverter()
                        }
                    }
                );
                reader.Close();
            }
            catch
            {
                reader.Close();
            }
        }

#endif
        }
}