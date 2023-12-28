namespace ScannerMonitor.Components
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using UnityEngine.EventSystems;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using Random = UnityEngine.Random;
    using Newtonsoft.Json;
    using System;
#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
    using Nautilus.Utility;
#endif

    /**
    * Component that contains the controller to the view. Majority of the view is set up already on the prefab inside of the unity editor.
    * Handles such things as the paginator, drawing all the items that are on the "current page"
    * Handles the idle screen saver.
    * Handles the welcome animations.
    */
    public class ScannerMonitorDisplay : MonoBehaviour
        , IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IProtoEventListener
    {
        public const float WELCOME_ANIMATION_TIME = 8.5f;
        public const float MAIN_SCREEN_ANIMATION_TIME = 1.2f;
        public const int ITEMS_PER_PAGE = 12;

        [AssertNotNull]
        public ItemButton[] Buttons;
        [AssertNotNull]
        public MapRoomFunctionality mapRoomFunctionality;
        [AssertNotNull]
        public Constructable constructable;

        private readonly HashSet<TechType> availableTechTypes = new HashSet<TechType>();
        public int currentPage = 1;
        public int maxPage = 1;
        public float idlePeriodLength = 60f;
        public float timeSinceLastInteraction;
        public bool isIdle;
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

        public void Awake()
        {
            if(mapRoomFunctionality == null || constructable == null)
            {
                PrefabIdentifier identifier = this.gameObject.GetComponentInParent<PrefabIdentifier>();
                mapRoomFunctionality = identifier.gameObject.GetComponent<MapRoomFunctionality>();
                constructable = identifier.gameObject.GetComponent<Constructable>();
            }
        }

        public void OnEnable()
        {
            mapRoomFunctionality.storageContainer.container.onAddItem += OnContainerChange;
            mapRoomFunctionality.storageContainer.container.onRemoveItem += OnContainerChange;

            if(Application.isEditor || (constructable.constructed && Builder.prefab != mapRoomFunctionality.gameObject))
            {
                CalculateNewColourTransitionTime();
                CalculateNewIdleTime();
                RefreshAvailable();
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

        private void OnContainerChange(InventoryItem item)
        {
            DrawPage(0);
        }

        public IEnumerator FinalSetup()
        {
            if(!Application.isEditor)
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

                yield return new WaitForSeconds(MAIN_SCREEN_ANIMATION_TIME);
            }
            DrawPage(currentPage);
            welcomeScreen.SetActive(false);
            blackCover.SetActive(false);
            mainScreen.SetActive(true);
            animator.enabled = false;
        }

        public void TurnDisplayOff()
        {
            StopCoroutine(FinalSetup());
            blackCover?.SetActive(true);
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
            if(currentPage <= 0)
            {
                currentPage = 1;
            }
            else if(currentPage > maxPage)
            {
                currentPage = maxPage;
            }

            var items = ITEMS_PER_PAGE;

            var startingPosition = (currentPage - 1) * items;
            var endingPosition = startingPosition + items;
            RefreshAvailable();
            var count = availableTechTypes.Count;
            if(endingPosition > count)
            {
                endingPosition = count;
            }

            if(items > 0)
            {
                var scale = Mathf.Min(200f, 2400f / (endingPosition - startingPosition));
                mainScreenItemGrid.cellSize = Vector2.one * scale;
                var keys = availableTechTypes.ToList();
                keys.Sort(uGUI_MapRoomScanner.CompareByName);
                int buttonCount = 0;
                for(var i = startingPosition; i < endingPosition; i++)
                {
                    DisplayTypeOnButton(buttonCount++, keys[i]);
                }
                while(buttonCount < ITEMS_PER_PAGE)
                {
                    DisplayTypeOnButton(buttonCount++, TechType.None);
                }
            }
            UpdatePaginator();
        }

        private void RefreshAvailable()
        {
            if(Application.isEditor)
            {
                if(availableTechTypes.Count == 0)
                {
                    availableTechTypes.AddRange((TechType[])Enum.GetValues(typeof(TechType)));
                }
            }
            else
            {
                availableTechTypes.Clear();
                ResourceTrackerDatabase.GetTechTypesInRange(gameObject.transform.position, mapRoomFunctionality.GetScanRange(), availableTechTypes);
            }
        }

        public void UpdatePaginator()
        {
            CalculateNewMaxPages();
            pageCounterGameObject.SetActive(maxPage > 1);
            pageCounterText.text = $"Page {currentPage} of {maxPage}";
            previousPageGameObject.SetActive(currentPage != 1);
            nextPageGameObject.SetActive(currentPage != maxPage);
        }

        public void DisplayTypeOnButton(int index, TechType type)
        {
            var itemButton = Buttons[index];
            itemButton.Type = type;
            if(type == TechType.None)
            {
                itemButton.gameObject.SetActive(false);
                itemButton.icon.sprite = SpriteManager.defaultSprite;
                itemButton.icon.color = Color.clear;
            }
            else
            {
                itemButton.gameObject.SetActive(true);
                if(!Application.isEditor)
                {
                    itemButton.icon.sprite = SpriteManager.Get(type);
                    itemButton.icon.color = Color.white;
                    if(itemButton.type == mapRoomFunctionality.typeToScan)
                    {
                        itemButton.OnSelect(null);
                    }
                }
            }
        }

        public void Update()
        {
            if(Builder.prefab == constructable.gameObject)
                return;

            if(mapRoomFunctionality.transform.localScale != lastScale)
            {
                lastScale = mapRoomFunctionality.transform.localScale;
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
            idlePeriodLength = 60f + Random.Range(1f, 10f);
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
#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
            var CurrentScanFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), $"{constructable.gameObject.GetComponent<PrefabIdentifier>().id}.json");
            var writer = new StreamWriter(CurrentScanFilePath);
            try
            {
                writer.Write(
                    JsonConvert.SerializeObject(
                        mapRoomFunctionality.typeToScan.AsString(), new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented
                        })
                    );
                writer.Flush();
                writer.Close();
            }
            catch
            {
                writer.Close();
            }
#endif
        }
        
        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
            var CurrentScanFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), $"{constructable.gameObject.GetComponent<PrefabIdentifier>().id}.json");
            if (!File.Exists(CurrentScanFilePath)) return;
            var reader = new StreamReader(CurrentScanFilePath);
            try
            {
                string techString = JsonConvert.DeserializeObject<string>(
                    reader.ReadToEnd(),
                    new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented
                    }
                );
                reader.Close();

                if(!Enum.TryParse(techString, out mapRoomFunctionality.typeToScan))
                    mapRoomFunctionality.typeToScan = TechType.None;
            }
            catch
            {
                reader.Close();
            }
#endif
        }
    }
}