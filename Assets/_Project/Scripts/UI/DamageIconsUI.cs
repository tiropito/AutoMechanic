using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Панель 5 иконок поломок. Скрывается, если ЛЮБАЯ из полноэкранных панелей открыта.
    /// </summary>
    public class DamageIconsUI : MonoBehaviour
    {
        [Header("Карточки (Button на каждой)")]
        [SerializeField] private Button smokeCard;
        [SerializeField] private Button sparkCard;
        [SerializeField] private Button wheelCard;
        [SerializeField] private Button brakesCard;
        [SerializeField] private Button rustCard;

        [Header("CanvasGroup каждой карточки")]
        [SerializeField] private CanvasGroup smokeGroup;
        [SerializeField] private CanvasGroup sparkGroup;
        [SerializeField] private CanvasGroup wheelGroup;
        [SerializeField] private CanvasGroup brakesGroup;
        [SerializeField] private CanvasGroup rustGroup;

        [Header("Скрытие при открытии меню")]
        [Tooltip("CanvasGroup на самой панели — скрывается, когда любое полноэкранное меню открыто")]
        [SerializeField] private CanvasGroup panelGroup;

        [Header("Логирование")]
        [SerializeField] private bool writeLogFile = false;

        private bool _subscribed;
        private bool _isHidden;
        private ShopPanelUI _cachedShop;
        private UpgradePanelUI _cachedUpgrade;
        private CollectionPanelUI _cachedCollection;

        private readonly StringBuilder _logBuf = new StringBuilder();
        private string LogPath => Path.Combine(Application.persistentDataPath, "DamageIconsLog.txt");

        // ==================== ЛОГ ====================

        private void Log(string msg)
        {
            Debug.Log($"[DamageIcons] {msg}");
            if (!writeLogFile) return;
            _logBuf.AppendLine($"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}");
            try { File.WriteAllText(LogPath, _logBuf.ToString()); } catch { }
        }

        // ==================== ЖИЗНЕННЫЙ ЦИКЛ ====================

        private void OnEnable() { TrySubscribe(); Refresh(); }
        private void Start() { TrySubscribe(); Refresh(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
            CheckPanelState();
        }

        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        // ==================== ПРОВЕРКА ПАНЕЛЕЙ ====================

        private void CheckPanelState()
        {
            // Кэшируем ссылки (FindObjectOfType работает и с неактивными через true)
            if (_cachedShop == null) _cachedShop = FindObjectOfType<ShopPanelUI>(true);
            if (_cachedUpgrade == null) _cachedUpgrade = FindObjectOfType<UpgradePanelUI>(true);
            if (_cachedCollection == null) _cachedCollection = FindObjectOfType<CollectionPanelUI>(true);

            bool anyOpen =
                (_cachedShop != null && _cachedShop.IsOpen) ||
                (_cachedUpgrade != null && _cachedUpgrade.IsOpen) ||
                (_cachedCollection != null && _cachedCollection.IsOpen);

            if (anyOpen == _isHidden) return;

            _isHidden = anyOpen;
            Log($"Панель меню: {(anyOpen ? "открыта → скрываю иконки" : "закрыта → показываю иконки")}");

            if (panelGroup != null)
            {
                panelGroup.alpha = anyOpen ? 0f : 1f;
                panelGroup.interactable = !anyOpen;
                panelGroup.blocksRaycasts = !anyOpen;
            }
        }

        // ==================== ПОДПИСКА ====================

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (GarageManager.Instance == null) return;
            if (DiagnosticManager.Instance == null) return;

            Subscribe();
            _subscribed = true;
        }

        private void Subscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnSessionsChanged += Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionChanged;
                GarageManager.Instance.OnCurrentSessionChanged += OnSessionChanged;
            }
            if (DiagnosticManager.Instance != null)
            {
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagUpdated;
                DiagnosticManager.Instance.OnDiagnosticsUpdated += OnDiagUpdated;
            }

            if (smokeCard != null)
            {
                smokeCard.onClick.RemoveAllListeners();
                smokeCard.onClick.AddListener(() => OnIconClicked(BreakdownVisual.Smoke));
            }
            if (sparkCard != null)
            {
                sparkCard.onClick.RemoveAllListeners();
                sparkCard.onClick.AddListener(() => OnIconClicked(BreakdownVisual.NoLight));
            }
            if (wheelCard != null)
            {
                wheelCard.onClick.RemoveAllListeners();
                wheelCard.onClick.AddListener(() => OnIconClicked(BreakdownVisual.Flat));
            }
            if (brakesCard != null)
            {
                brakesCard.onClick.RemoveAllListeners();
                brakesCard.onClick.AddListener(() => OnIconClicked(BreakdownVisual.Squeak));
            }
            if (rustCard != null)
            {
                rustCard.onClick.RemoveAllListeners();
                rustCard.onClick.AddListener(() => OnIconClicked(BreakdownVisual.Rust));
            }
        }

        private void Unsubscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionChanged;
            }
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagUpdated;

            _subscribed = false;
        }

        private void OnSessionChanged(int idx) => Refresh();
        private void OnDiagUpdated(int idx) => Refresh();

        // ==================== ОБНОВЛЕНИЕ ====================

        private void Refresh()
        {
            if (GarageManager.Instance == null) { HideAll(); return; }

            var sessions = GarageManager.Instance.Sessions;
            if (sessions.Count == 0) { HideAll(); return; }

            int idx = GarageManager.Instance.CurrentSessionIndex;
            if (idx < 0 || idx >= sessions.Count) { HideAll(); return; }

            var session = sessions[idx];
            if (session.state == RepairState.NotDiagnosed) { HideAll(); return; }

            bool smoke = HasUnfixed(idx, BreakdownVisual.Smoke);
            bool spark = HasUnfixed(idx, BreakdownVisual.NoLight);
            bool wheel = HasUnfixed(idx, BreakdownVisual.Flat);
            bool brakes = HasUnfixed(idx, BreakdownVisual.Squeak);
            bool rust = HasUnfixed(idx, BreakdownVisual.Rust);

            SetCard(smokeCard, smokeGroup, smoke);
            SetCard(sparkCard, sparkGroup, spark);
            SetCard(wheelCard, wheelGroup, wheel);
            SetCard(brakesCard, brakesGroup, brakes);
            SetCard(rustCard, rustGroup, rust);
        }

        private bool HasUnfixed(int idx, BreakdownVisual visual)
        {
            if (DiagnosticManager.Instance == null) return false;
            var list = DiagnosticManager.Instance.GetBreakdowns(idx);
            foreach (var bd in list)
            {
                if (bd == null) continue;
                if (bd.visual != visual) continue;
                if (!DiagnosticManager.Instance.IsFixed(idx, bd)) return true;
            }
            return false;
        }

        private void SetCard(Button btn, CanvasGroup group, bool show)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(show);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = show;
                group.blocksRaycasts = show;
            }
            btn.interactable = show;
        }

        private void HideAll()
        {
            if (smokeCard != null) smokeCard.gameObject.SetActive(false);
            if (sparkCard != null) sparkCard.gameObject.SetActive(false);
            if (wheelCard != null) wheelCard.gameObject.SetActive(false);
            if (brakesCard != null) brakesCard.gameObject.SetActive(false);
            if (rustCard != null) rustCard.gameObject.SetActive(false);
        }

        // ==================== КЛИК ====================

        private void OnIconClicked(BreakdownVisual visual)
        {
            var shop = FindObjectOfType<ShopPanelUI>(true);
            if (shop == null) { Log("ShopPanelUI не найден"); return; }

            string partId = GetPartIdForVisual(visual);

            shop.Open();

            if (!string.IsNullOrEmpty(partId))
            {
                DOVirtual.DelayedCall(0.05f, () =>
                {
                    shop.HighlightPart(partId, 5f);
                });
            }
        }

        private string GetPartIdForVisual(BreakdownVisual visual)
        {
            if (GarageManager.Instance == null) return null;
            if (DiagnosticManager.Instance == null) return null;

            var sessions = GarageManager.Instance.Sessions;
            if (sessions.Count == 0) return null;

            int idx = GarageManager.Instance.CurrentSessionIndex;
            if (idx < 0 || idx >= sessions.Count) return null;

            var session = sessions[idx];

            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                if (bd.visual != visual) continue;
                if (DiagnosticManager.Instance.IsFixed(idx, bd)) continue;
                if (bd.requiredParts == null || bd.requiredParts.Length == 0) continue;
                return bd.requiredParts[0].id;
            }

            if (session.car != null && session.car.possibleBreakdowns != null)
            {
                foreach (var bd in session.car.possibleBreakdowns)
                {
                    if (bd == null) continue;
                    if (bd.visual != visual) continue;
                    if (bd.requiredParts == null || bd.requiredParts.Length == 0) continue;
                    return bd.requiredParts[0].id;
                }
            }

            return null;
        }
    }
}