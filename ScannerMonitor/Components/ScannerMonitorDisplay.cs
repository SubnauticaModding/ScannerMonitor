namespace ScannerMonitor.Components
{
    using SMLHelper.V2.Handlers;
    using SMLHelper.V2.Utility;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Random = UnityEngine.Random;
#if SUBNAUTICA_STABLE
    using Oculus.Newtonsoft.Json;
#else
    using Newtonsoft.Json;
#endif

    /**
    * Component that contains the controller to the view. Majority of the view is set up already on the prefab inside of the unity editor.
    * Handles such things as the paginator, drawing all the items that are on the "current page"
    * Handles the idle screen saver.
    * Handles the welcome animations.
    */
    public class ScannerMonitorDisplay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler

#if !UNITY_EDITOR
        , IProtoEventListener
#endif
    {
        public static readonly float WELCOME_ANIMATION_TIME = 8.5f;
        public static readonly float MAIN_SCREEN_ANIMATION_TIME = 1.2f;
        public int ITEMS_PER_PAGE => Mathf.RoundToInt(6 * MapRoomFunctionality.transform.localScale.x);

        public List<TechType> TrackedResources { set; get; } = new List<TechType>();
        public Dictionary<TechType, GameObject> trackedResourcesDisplayElements;
        public MapRoomFunctionality MapRoomFunctionality;
        public Constructable Constructable;
        public int currentPage = 1;
        public int maxPage = 1;
        public float idlePeriodLength = 20f;
        public float timeSinceLastInteraction = 0f;
        public bool isIdle = false;
        public float nextColorTransitionCurrentTime;
        public float transitionIdleTime;
        public List<Color> PossibleIdleColors = new List<Color>()
        {
            new Color(0.07f, 0.38f, 0.70f), // BLUE
            new Color(0.86f, 0.22f, 0.22f), // RED
            new Color(0.22f, 0.86f, 0.22f) // GREEN
        };
        public Color currentColor;
        public Color nextColor;
        public bool isHovered = false;
        public bool isHoveredOutOfRange = false;

        public Vector3 lastscale = Vector3.zero;

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

        public void OnEnable()
        {
            if(Constructable.constructed)
            {
#if !UNITY_EDITOR
                LoadCache();
#if SN1
                foreach(TechType techType in ResourceTracker.resources.Keys)
#elif BZ
                foreach(TechType techType in ResourceTrackerDatabase.resources.Keys)
#endif
                {
                    if(!TrackedResources.Contains(techType))
                        TrackedResources.Add(techType);
                }
                SaveCache();
#endif
                trackedResourcesDisplayElements = new Dictionary<TechType, GameObject>();

                CalculateNewColourTransitionTime();
                CalculateNewIdleTime();
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
            TurnDisplayOff();
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
            int items = ITEMS_PER_PAGE;
            maxPage = items <= 0 ? 1 : Mathf.CeilToInt((TrackedResources.Count - 1) / items) + 1;
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

            int items = ITEMS_PER_PAGE;

#if SN1
            foreach(TechType techType in ResourceTracker.resources.Keys)
#elif BZ
            foreach(TechType techType in ResourceTrackerDatabase.resources.Keys)
#endif
            {
                if(!TrackedResources.Contains(techType))
                    TrackedResources.Add(techType);
            }
#if !UNITY_EDITOR
            SaveCache();
#endif
            var startingPosition = (currentPage - 1) * items;
            var endingPosition = startingPosition + items;
            if (endingPosition > TrackedResources.Count)
            {
                endingPosition = TrackedResources.Count;
            }

            ClearPage();
            if(items > 0)
            {
                float scale = Mathf.Min(200f, 2400f / (endingPosition - startingPosition));
                mainScreenItemGrid.cellSize = Vector2.one*scale;
                for(var i = startingPosition; i < endingPosition; i++)
                    CreateAndAddItemDisplay(TrackedResources[i]);
            }
            UpdatePaginator();
        }

        public void UpdatePaginator()
        {
            CalculateNewMaxPages();
            pageCounterText.text = $"Page {currentPage} Of {maxPage}";
            previousPageGameObject.SetActive(currentPage != 1);
            nextPageGameObject.SetActive(currentPage != maxPage);
        }

        public void ClearPage()
        {
            for (int i = 0; i < mainScreenItemGrid.transform.childCount; i++)
            {
                Destroy(mainScreenItemGrid.transform.GetChild(i).gameObject);
            }

            if (trackedResourcesDisplayElements != null)
            {
                trackedResourcesDisplayElements.Clear();
            }
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
            if(MapRoomFunctionality.transform.localScale != lastscale)
            {
                lastscale = MapRoomFunctionality.transform.localScale;
                DrawPage(currentPage);
            }

            if (isIdle == false && timeSinceLastInteraction < idlePeriodLength)
            {
                timeSinceLastInteraction += Time.deltaTime;
            }

            if (isIdle == false && timeSinceLastInteraction >= idlePeriodLength)
            {
                EnterIdleScreen();
            }

            if (isHovered == false && isHoveredOutOfRange == true && InIdleInteractionRange() == true)
            {
                isHovered = true;
                ExitIdleScreen();
            }

            if (isHovered == true)
            {
                ResetIdleTimer();
            }

            if (isIdle == true)
            {
                if (nextColorTransitionCurrentTime >= transitionIdleTime)
                {
                    nextColorTransitionCurrentTime = 0f;
                    for (int i = 0; i < PossibleIdleColors.Count; i++)
                    {

                        if (PossibleIdleColors[i] == nextColor)
                        {
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
                }

                nextColorTransitionCurrentTime += Time.deltaTime;
                idleScreenTitleBackgroundImage.color = Color.Lerp(currentColor, nextColor, nextColorTransitionCurrentTime / transitionIdleTime);
            }
        }

        public bool InIdleInteractionRange()
        {
            return Mathf.Abs(Vector3.Distance(gameObject.transform.position, Player.main?.transform.position ?? gameObject.transform.position)) <= 5f;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isIdle && InIdleInteractionRange())
            {
                ExitIdleScreen();
            }
            
            if (isIdle == false)
            {
                ResetIdleTimer();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHoveredOutOfRange = true;
            if (InIdleInteractionRange())
            {
                isHovered = true;
            }

            if (isIdle && InIdleInteractionRange())
            {
                ExitIdleScreen();
            }

            if (isIdle == false)
            {
                ResetIdleTimer();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
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
        }

        public void CalculateNewColourTransitionTime()
        {
            transitionIdleTime = 2f + Random.Range(0f, 2f);
        }

        public void OnApplicationQuit()
        {
            StopAllCoroutines();
        }

#if !UNITY_EDITOR
        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            SaveCache();
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
                            new TechTypeConverter()
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

        public void SaveCache()
        {
            string CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            var writer = new StreamWriter(CacheFilePath);
            try
            {
                writer.Write(
                    JsonConvert.SerializeObject(
                        TrackedResources, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            Converters = new List<JsonConverter>
                            {
                            new TechTypeConverter()
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
            LoadCache();
            var CurrentScanFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), $"{gameObject.GetComponent<PrefabIdentifier>().id}.json");
            if(File.Exists(CurrentScanFilePath))
            {
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
                                new TechTypeConverter()
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
        }

        public void LoadCache()
        {
            string CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            if(File.Exists(CacheFilePath))
            {
                var reader = new StreamReader(CacheFilePath);
                try
                {
                    TrackedResources = JsonConvert.DeserializeObject<List<TechType>>(
                        reader.ReadToEnd(),
                        new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            Converters = new List<JsonConverter>
                                {
                                new TechTypeConverter()
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
        }

        public class TechTypeConverter: JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, ((TechType)value).AsString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                string v = (string)serializer.Deserialize(reader, typeof(string));
                if(TechTypeExtensions.FromString(v, out TechType techType, true))
                    return techType;
                else
                    return TechTypeHandler.TryGetModdedTechType(v, out techType)
                        ? (object)techType
                        : throw new Exception($"Failed to parse {v} into a Techtype");
            }

            public override bool CanConvert(Type objectType) => objectType == typeof(TechType);
        }
#endif
    }
}