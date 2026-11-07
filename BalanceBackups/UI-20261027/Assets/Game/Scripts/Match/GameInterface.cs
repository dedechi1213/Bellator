using System;
using System.Collections.Generic;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Native uGUI presentation. Match rules remain in GameManager/WaveManager.</summary>
    [DefaultExecutionOrder(200)]
    public sealed partial class GameInterface : MonoBehaviour
    {
        public const string MainScene = "Assets/Game/Scenes/MainMenu.unity";
        static readonly Color Ink = new Color(.13f, .105f, .085f, .98f);
        static readonly Color Gold = new Color(.85f, .67f, .35f);
        static readonly Color Parchment = new Color(.94f, .88f, .74f);
        Canvas canvas;
        GameManager game;
        GameObject home, hud, modal, settings, crosshair;
        Text wave, message, enemies, healthText, staminaText, weapon, ammo, controls, resultTitle, resultText;
        Image healthFill, staminaFill;
        Button resume, restart, menu, config, settingsBack;
        bool loading, settingsVisible, wasModal;
        GameObject previousSelection;
        MatchState lastState = MatchState.None;
        readonly List<GameObject> enemyBars = new List<GameObject>();
        public bool SettingsVisible => settingsVisible;
        public bool Loading => loading;
        public GameObject SettingsPanel => settings;

        void Start()
        {
            game = FindAnyObjectByType<GameManager>();
            // URP needs a presentation camera in standalone menu scenes, even for overlay-only UI.
            if(game==null && FindAnyObjectByType<Camera>()==null)
            {
                var menuCamera=new GameObject("MenuPresentationCamera",typeof(Camera)).GetComponent<Camera>();
                menuCamera.transform.SetParent(transform,false);menuCamera.clearFlags=CameraClearFlags.SolidColor;
                menuCamera.backgroundColor=Ink;menuCamera.cullingMask=0;menuCamera.depth=-100;
            }
            canvas = new GameObject("GameInterfaceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = .5f;
            if (game != null) BuildMatch();
            else if (FindAnyObjectByType<GladiatorSelectionController>() != null)
                MakeButton(canvas.transform, "SelectionMenuButton", "MENU PRINCIPAL", .025f, .895f, .20f, .94f, () => Load(MainScene));
            else BuildHome();
            BuildSettings();
            if(home!=null) { BuildAccess(); PlayerSession.Changed+=SessionChanged; SessionChanged(); }
            if (game == null) { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void BuildHome()
        {
            home = Panel(canvas.transform, "MainMenu", 0, 0, 1, 1, Ink);
            Panel(home.transform, "GoldRule", .10f, .85f, .90f, .854f, Gold);
            Label(home.transform, "Heading", "BELLATOR", .1f, .68f, .9f, .84f, 72, Gold);
            Label(home.transform, "Motto", "DA ARENA À LIBERDADE", .1f, .61f, .9f, .68f, 22, Parchment);
            profileLabel=Label(home.transform,"ConnectedPlayer","",.15f,.565f,.85f,.61f,17,Parchment);
            MakeButton(home.transform,"SwitchAccountButton","TROCAR CONTA",.79f,.915f,.97f,.975f,()=>PlayerSession.Logout());
            MakeButton(home.transform, "PlayButton", "JOGAR", .33f, .49f, .67f, .56f, Play);
            MakeButton(home.transform, "SelectCharacterButton", "SELECIONAR PERSONAGEM", .33f, .40f, .67f, .47f, () => Load(PlayerClassController.SelectionScene));
            MakeButton(home.transform, "RankingButton", "RANKING", .33f, .31f, .67f, .38f, OpenRanking);
            MakeButton(home.transform, "SettingsButton", "CONFIGURAÇÕES", .33f, .22f, .67f, .29f, OpenSettings);
            MakeButton(home.transform, "QuitButton", "SAIR", .33f, .13f, .67f, .20f, Quit);
            Label(home.transform, "Footnote", "Conquiste sua liberdade ou sobreviva para entrar para a história.", .08f, .045f, .92f, .10f, 17, Parchment);
            Focus(home.transform.Find("PlayButton").gameObject);
        }
        void Play() => OpenModeSelection();
        void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void Load(string scene)
        {
            if (loading) return;
            loading = true; GameSettings.Save(); Time.timeScale = 1;
            foreach (var selectable in canvas.GetComponentsInChildren<Selectable>(true)) selectable.interactable = false;
            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }

        void BuildMatch()
        {
            // Keep the previous prefab intact for earlier-stage regression suites.
            foreach (var legacy in FindObjectsByType<MatchHUD>(FindObjectsSortMode.None)) legacy.gameObject.SetActive(false);
            foreach (var legacy in FindObjectsByType<CombatHUD>(FindObjectsSortMode.None)) legacy.NativePresentation = true;
            hud = Panel(canvas.transform, "CombatHUD", 0, 0, 1, 1, Color.clear);
            hud.GetComponent<Image>().raycastTarget = false;
            var vitals = Panel(hud.transform, "Vitals", .02f, .76f, .35f, .97f, Ink);
            weapon = Label(vitals.transform, "Weapon", "", .05f, .70f, .95f, .98f, 19, Gold);
            healthFill = Bar(vitals.transform, "HealthBar", .05f, .44f, .95f, .65f, new Color(.66f, .14f, .13f));
            healthText = Label(vitals.transform, "HealthValue", "", .05f, .44f, .95f, .65f, 16, Color.white);
            staminaFill = Bar(vitals.transform, "StaminaBar", .05f, .12f, .95f, .33f, new Color(.32f, .47f, .32f));
            staminaText = Label(vitals.transform, "StaminaValue", "", .05f, .12f, .95f, .33f, 16, Color.white);
            var banner = Panel(hud.transform, "WavePanel", .51f, .79f, .98f, .97f, Ink);
            wave = Label(banner.transform, "Wave", "", .03f, .67f, .97f, .97f, 22, Gold);
            message = Label(banner.transform, "Message", "", .03f, .28f, .97f, .69f, 18, Parchment);
            enemies = Label(banner.transform, "Enemies", "", .03f, .02f, .97f, .30f, 16, Parchment);
            var ammunition = Panel(hud.transform, "AmmunitionPanel", .66f, .10f, .98f, .21f, Ink);
            ammo = Label(ammunition.transform, "Ammo", "", .04f, .05f, .96f, .95f, 18, Parchment);
            controls = Label(hud.transform, "Controls", "", .02f, .012f, .98f, .083f, 14, Parchment);
            crosshair = Panel(hud.transform, "RangedCrosshair", .5f, .5f, .5f, .5f, Color.clear);
            crosshair.GetComponent<Image>().raycastTarget = false;
            var crossRect = crosshair.GetComponent<RectTransform>(); crossRect.sizeDelta = new Vector2(28, 28);
            Panel(crosshair.transform, "Horizontal", .05f, .46f, .95f, .54f, Color.white).GetComponent<Image>().raycastTarget = false;
            Panel(crosshair.transform, "Vertical", .46f, .05f, .54f, .95f, Color.white).GetComponent<Image>().raycastTarget = false;
            modal = Panel(canvas.transform, "MatchModal", 0, 0, 1, 1, new Color(0, 0, 0, .83f));
            var card = Panel(modal.transform, "RomanCard", .23f, .12f, .77f, .88f, Ink);
            Panel(card.transform, "GoldRule", .07f, .93f, .93f, .936f, Gold);
            resultTitle = Label(card.transform, "ResultTitle", "", .07f, .76f, .93f, .92f, 42, Gold);
            resultText = Label(card.transform, "ResultDescription", "", .07f, .58f, .93f, .76f, 21, Parchment);
            resume = MakeButton(card.transform, "ResumeButton", "CONTINUAR", .13f, .44f, .87f, .54f, () => game.Resume());
            restart = MakeButton(card.transform, "RestartButton", "REINICIAR PARTIDA", .13f, .31f, .87f, .41f, () => { GameSettings.Save(); game.Restart(); });
            config = MakeButton(card.transform, "PauseSettingsButton", "CONFIGURAÇÕES", .13f, .18f, .87f, .28f, OpenSettings);
            menu = MakeButton(card.transform, "MenuButton", "MENU PRINCIPAL", .13f, .05f, .87f, .15f, () => { GameSettings.Save(); game.ReturnToMenu(); });
            modal.SetActive(false);
            if(game.IsInfinite) BuildInfiniteMatch();
        }

        void BuildSettings()
        {
            settings = Panel(canvas.transform, "SettingsPanel", 0, 0, 1, 1, new Color(0, 0, 0, .94f));
            var card = Panel(settings.transform, "SettingsCard", .15f, .06f, .85f, .94f, Ink);
            Label(card.transform, "Title", "CONFIGURAÇÕES", .05f, .90f, .95f, .99f, 34, Gold);
            var volumeText = Label(card.transform, "VolumeLabel", "", .08f, .83f, .92f, .89f, 21, Parchment);
            var volume = MakeSlider(card.transform, "VolumeSlider", .08f, .78f, .92f, .82f, 0, 1, GameSettings.Volume);
            volumeText.text = "Volume geral: " + Mathf.RoundToInt(volume.value * 100) + "%";
            volume.onValueChanged.AddListener(v => { GameSettings.SetVolume(v); volumeText.text = "Volume geral: " + Mathf.RoundToInt(v * 100) + "%"; });
            var sensitivityText = Label(card.transform, "SensitivityLabel", "Sensibilidade do mouse: " + GameSettings.Sensitivity.ToString("0.00") + "x", .08f, .70f, .92f, .76f, 21, Parchment);
            var sensitivity = MakeSlider(card.transform, "SensitivitySlider", .08f, .65f, .92f, .69f, .25f, 3, GameSettings.Sensitivity);
            sensitivity.onValueChanged.AddListener(v => { GameSettings.SetSensitivity(v); sensitivityText.text = "Sensibilidade do mouse: " + v.ToString("0.00") + "x"; });
            var horizontalText = Label(card.transform, "HorizontalSensitivityLabel", "Horizontal: " + GameSettings.HorizontalSensitivity.ToString("0.00") + "x", .08f, .56f, .47f, .62f, 20, Parchment);
            var horizontal = MakeSlider(card.transform, "HorizontalSensitivitySlider", .08f, .51f, .47f, .55f, .25f, 3, GameSettings.HorizontalSensitivity);
            horizontal.onValueChanged.AddListener(v => { GameSettings.SetHorizontalSensitivity(v); horizontalText.text = "Horizontal: " + v.ToString("0.00") + "x"; });
            var verticalText = Label(card.transform, "VerticalSensitivityLabel", "Vertical: " + GameSettings.VerticalSensitivity.ToString("0.00") + "x", .53f, .56f, .92f, .62f, 20, Parchment);
            var vertical = MakeSlider(card.transform, "VerticalSensitivitySlider", .53f, .51f, .92f, .55f, .25f, 3, GameSettings.VerticalSensitivity);
            vertical.onValueChanged.AddListener(v => { GameSettings.SetVerticalSensitivity(v); verticalText.text = "Vertical: " + v.ToString("0.00") + "x"; });
            var invert = MakeButton(card.transform, "InvertVerticalButton", "", .08f, .37f, .47f, .46f, null);
            Action refreshInvert = () => invert.GetComponentInChildren<Text>().text = "Inverter vertical: " + (GameSettings.InvertVertical ? "SIM" : "NÃO");
            refreshInvert(); invert.onClick.AddListener(() => { GameSettings.SetInvertVertical(!GameSettings.InvertVertical); refreshInvert(); });
            var shakeText = Label(card.transform, "CameraShakeLabel", "Impacto da câmera: " + Mathf.RoundToInt(GameSettings.CameraShake * 100) + "%", .53f, .41f, .92f, .47f, 20, Parchment);
            var shake = MakeSlider(card.transform, "CameraShakeSlider", .53f, .36f, .92f, .40f, 0, 1, GameSettings.CameraShake);
            shake.onValueChanged.AddListener(v => { GameSettings.SetCameraShake(v); shakeText.text = "Impacto da câmera: " + Mathf.RoundToInt(v * 100) + "%"; });
            var quality = MakeButton(card.transform, "QualityButton", "", .08f, .25f, .92f, .33f, null);
            Action refreshQuality = () => quality.GetComponentInChildren<Text>().text = "Qualidade gráfica: " + QualitySettings.names[GameSettings.Quality] + "  ›";
            refreshQuality(); quality.onClick.AddListener(() => { GameSettings.SetQuality((GameSettings.Quality + 1) % Mathf.Max(1, QualitySettings.names.Length)); refreshQuality(); });
            var screen = MakeButton(card.transform, "ScreenModeButton", "", .08f, .15f, .92f, .23f, null);
            Action refreshScreen = () => screen.GetComponentInChildren<Text>().text = GameSettings.SupportsScreenMode ? "Tela: " + (GameSettings.ScreenMode == 0 ? "Janela" : "Tela cheia sem bordas") + "  ›" : "Modo de tela disponível no jogo compilado";
            refreshScreen(); screen.interactable = GameSettings.SupportsScreenMode;
            screen.onClick.AddListener(() => { GameSettings.SetScreenMode(1 - GameSettings.ScreenMode); refreshScreen(); });
            settingsBack = MakeButton(card.transform, "SettingsBackButton", "SALVAR E VOLTAR", .22f, .04f, .78f, .13f, CloseSettings);
            settings.SetActive(false);
        }
        public void OpenSettings()
        {
            if (loading || (game != null && game.State != MatchState.Paused)) return;
            settingsVisible = true;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            settings.SetActive(true); if (modal != null) modal.SetActive(false); if (home != null) home.SetActive(false);
            Focus(settingsBack.gameObject);
        }
        public void CloseSettings()
        {
            GameSettings.Save(); settingsVisible = false; settings.SetActive(false);
            if (home != null) home.SetActive(true);
            if (game != null) modal.SetActive(game.State == MatchState.Paused || game.IsTerminal);
            if (previousSelection != null) Focus(previousSelection);
        }
        void Update()
        {
            if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if(rankingPanel != null && rankingPanel.activeSelf) { CloseRanking(); return; }
                if(modePanel != null && modePanel.activeSelf) { CloseModeSelection(); return; }
            }
            if (settingsVisible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseSettings();
        }
        void LateUpdate()
        {
            if (game == null) return;
            var state = game.State; bool showModal = state == MatchState.Paused || game.IsTerminal;
            bool infiniteOver=game.IsInfinite && state==MatchState.Defeat;
            hud.SetActive(!showModal); modal.SetActive(showModal && !settingsVisible && !infiniteOver);
            if(game.IsInfinite) UpdateInfiniteMatch(infiniteOver);
            if (showModal && !infiniteOver)
            {
                bool paused = state == MatchState.Paused;
                resume.gameObject.SetActive(paused); config.gameObject.SetActive(paused);
                resultTitle.text = paused ? "PAUSA" : state == MatchState.Victory ? "VITÓRIA" : "DERROTA";
                resultText.text = game.Loading ? "Carregando..." : paused ? "A arena aguarda. Continue quando estiver pronto." : state == MatchState.Victory ? "As correntes ficaram para trás.\nSua coragem conquistou a liberdade!" : "A areia guarda sua história.\nLevante-se e tente outra vez.";
                resume.interactable = restart.interactable = menu.interactable = config.interactable = !game.Loading;
                if ((!wasModal || lastState != state) && !settingsVisible) Focus(paused ? resume.gameObject : restart.gameObject);
            }
            wasModal = showModal; lastState = state;
            var actor = game.Player; var hp = actor.GetComponent<CharacterHealth>(); var sp = actor.GetComponent<CharacterStamina>();
            var equipment = actor.GetComponent<WeaponEquipment>(); var combat = actor.GetComponent<PlayerCombat>();
            healthFill.fillAmount = hp.Current / hp.Maximum; staminaFill.fillAmount = sp.Current / sp.Maximum;
            bool denied = Time.time - sp.LastDeniedTime < .4f;
            staminaFill.color = denied ? new Color(1, .55f, .12f) : new Color(.25f, .7f, .32f);
            healthText.text = "VIDA  " + Mathf.CeilToInt(hp.Current) + " / " + hp.Maximum;
            staminaText.text = "RESISTÊNCIA  " + Mathf.CeilToInt(sp.Current) + " / " + sp.Maximum;
            if (denied) staminaText.text = "RESISTÊNCIA INSUFICIENTE";
            var current = equipment.Current;
            weapon.text = (actor.Current != null ? actor.Current.displayName + " • " : "") + (current != null ? current.displayName : "");
            crosshair.SetActive(current != null && current.IsRanged);
            controls.text=CombatHUD.ControlHints(equipment,actor.GetComponent<CombatInput>().HeavyDisplay);
            ammo.transform.parent.gameObject.SetActive(current!=null && current.IsRanged);
            ammo.text = current == null || !current.IsRanged ? "MUNIÇÃO  —\nArma corpo a corpo" : current.type == EquipmentType.Crossbow ? "VIROTES  " + (combat.CrossbowLoaded ? "1" : "0") + " / ∞\n" + (combat.IsReloading ? "Recarga: " + ((1 - combat.ReloadProgress) * current.reloadSeconds).ToString("0.0") + " s" : combat.CrossbowLoaded ? "Besta pronta" : "R para recarregar") : "FLECHAS  ∞\nReserva ilimitada";
            var waves = game.Waves;
            UpdateEnemyBars();
            wave.text = "ONDA " + waves.WaveNumber + (game.IsInfinite ? " • INFINITO" : " / " + waves.WaveCount);
            enemies.text = "Inimigos restantes: " + (waves.AliveCount + waves.PendingCount) + "  •  Na arena: " + waves.AliveCount;
            message.text = state == MatchState.Preparing ? "Prepare-se • " + Mathf.CeilToInt(waves.Countdown) + " s" : state == MatchState.Intermission ? "Próxima onda: " + waves.WaveNumber + " • " + Mathf.CeilToInt(waves.Countdown) + " s" : state == MatchState.Escape ? "A liberdade espera. Atravesse o portão iluminado!" : waves.SpawnBlocked ? "Aguardando espaço na entrada..." : !game.IsInfinite && waves.WaveNumber == waves.WaveCount ? "Onda final • Lute por sua liberdade!" : "Sobreviva à arena";
        }
        void UpdateEnemyBars()
        {
            var actors = game.Waves.Actors; var camera = Camera.main;
            for (int i = 0; i < Mathf.Max(actors.Count, enemyBars.Count); i++)
            {
                if (i >= enemyBars.Count)
                {
                    var root = Panel(hud.transform, "EnemyHealth_" + i, .5f, .5f, .5f, .5f, Color.clear);
                    root.GetComponent<Image>().raycastTarget = false; root.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 28);
                    Bar(root.transform, "Health", 0, .05f, 1, .25f, new Color(.7f, .19f, .13f));
                    Label(root.transform, "Value", "", 0, .28f, 1, 1, 14, Parchment);
                    // Place enemy bars behind the fixed HUD panels.
                    root.transform.SetAsFirstSibling(); enemyBars.Add(root);
                }
                var bar = enemyBars[i]; var actor = i < actors.Count ? actors[i] : null;
                if (actor == null || camera == null) { bar.SetActive(false); continue; }
                var hp = actor.GetComponent<CharacterHealth>();
                var body = actor.GetComponent<CharacterController>();
                var point = camera.WorldToScreenPoint(actor.transform.position + Vector3.up * (body.height + .35f));
                bool visible = hp.IsAlive && actor.gameObject.activeInHierarchy && point.z > 0 && point.z < 35 && point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height;
                bar.SetActive(visible); if (!visible) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.GetComponent<RectTransform>(), point, null, out var local);
                bar.GetComponent<RectTransform>().anchoredPosition = local;
                bar.transform.Find("Health/Fill").GetComponent<Image>().fillAmount = hp.Current / hp.Maximum;
                bar.transform.Find("Value").GetComponent<Text>().text = Mathf.CeilToInt(hp.Current) + " / " + hp.Maximum;
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) GameSettings.Save(); }
        void OnApplicationQuit()
        {
            if(game!=null && game.CanRecordResult && game.State==MatchState.Defeat && !game.ResultSaved)
                game.SaveInfiniteResult();
            GameSettings.Save();
        }
        void OnDestroy() { PlayerSession.Changed-=SessionChanged; }
        static void Focus(GameObject target) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target); }
        static GameObject Panel(Transform parent, string name, float x, float y, float right, float top, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image)); g.transform.SetParent(parent, false);
            var rect = g.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero;
            g.GetComponent<Image>().color = color; return g;
        }
        static Text Label(Transform parent, string name, string content, float x, float y, float right, float top, int size, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Text)); g.transform.SetParent(parent, false);
            var rect = g.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = g.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = content; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = color; text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Min(14, size); text.resizeTextMaxSize = size; return text;
        }
        static Button MakeButton(Transform parent, string name, string title, float x, float y, float right, float top, Action action)
        {
            var g = Panel(parent, name, x, y, right, top, new Color(.40f, .16f, .13f));
            var button = g.AddComponent<Button>(); button.targetGraphic = g.GetComponent<Image>();
            var outline = g.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(1, -1);
            var colors = button.colors; colors.highlightedColor = new Color(1.3f, 1.2f, 1.1f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.7f, .65f, .6f); button.colors = colors;
            Label(g.transform, "Label", title, .025f, .03f, .975f, .97f, 21, Parchment);
            if (action != null) button.onClick.AddListener(() => action()); return button;
        }
        static Image Bar(Transform parent, string name, float x, float y, float right, float top, Color color)
        {
            var bg = Panel(parent, name, x, y, right, top, new Color(.07f, .06f, .05f));
            bg.GetComponent<Image>().raycastTarget = false;
            var image = Panel(bg.transform, "Fill", 0, 0, 1, 1, color).GetComponent<Image>();
            // Filled images need a sprite; create a native white sprite without external artwork.
            image.sprite = WhiteSprite; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.raycastTarget = false; return image;
        }
        static Sprite whiteSprite;
        static Sprite WhiteSprite => whiteSprite != null ? whiteSprite : whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f);
        static Slider MakeSlider(Transform parent, string name, float x, float y, float right, float top, float min, float max, float value)
        {
            var root = Panel(parent, name, x, y, right, top, new Color(.28f, .25f, .21f)); var slider = root.AddComponent<Slider>();
            var fill = Panel(root.transform, "Fill", 0, .25f, 1, .75f, Gold).GetComponent<RectTransform>();
            var handleArea = Panel(root.transform, "HandleArea", .015f, 0, .985f, 1, Color.clear); handleArea.GetComponent<Image>().raycastTarget = false;
            var handle = Panel(handleArea.transform, "Handle", 0, 0, 0, 1, Parchment).GetComponent<RectTransform>(); handle.sizeDelta = new Vector2(20, 0);
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value); return slider;
        }
    }
}
