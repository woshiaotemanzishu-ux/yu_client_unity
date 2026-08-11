using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Audio;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Partner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 灵宠第三页“神巫”的 Pet 域实现。页面只消费 PartnerModel 的 14202/14201 快照；
    /// 选择、属性/传记切换、自动技能开关均为只读/本地状态。14203 跟随、14206 妖核、
    /// 14204 激活与 14205 培养在真实事务叶完成前只保留可命中入口，不发送协议。
    /// </summary>
    public sealed class PetPartnerPageView : BaseView
    {
        // 老端 PartnerDetailsView 按 12 项列表索引播放，切项先停上一条。
        private static readonly string[] PartnerVoices =
        {
            "NPC_Voice02b", "NPC_Voice18b", "NPC_Voice03b", "Pet_Voice14b",
            "NPC_Voice10b", "NPC_Voice34b", "NPC_Voice17b", "NPC_Voice31b",
            "NPC_Voice27b", "NPC_Voice21b", "NPC_Voice07b", "NPC_Voice04b"
        };

        [Header("Model and summary")]
        public RectTransform modelHost;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI stageText;
        public TextMeshProUGUI combatText;
        public TextMeshProUGUI starText;
        public Image starItemTemplate;
        [SerializeField] private Sprite activeStarSprite;
        [SerializeField] private Sprite inactiveStarSprite;
        public TextMeshProUGUI stateText;
        public TextMeshProUGUI blessingText;
        public TextMeshProUGUI trainCountText;

        [Header("Companion selector")]
        public ScrollRect companionScroll;
        public RectTransform companionViewport;
        public RectTransform companionContent;
        public GameObject companionItemTemplate;
        public Image previousButton;
        public Image nextButton;

        [Serializable]
        private sealed class PartnerVisualBinding
        {
            public int figureId;
            public Sprite icon;
            public Sprite cardFrame;
            public Sprite stageBadge;
            public Sprite biography;
            public Sprite skillIcon;
            public Sprite goodsIcon;
            public string unlockName;
        }

        [Serializable]
        private sealed class PartnerSkillVisualBinding
        {
            public int skillId;
            public Sprite icon;
        }

        [Serializable]
        private sealed class PartnerSkillSlotBinding
        {
            public Image icon;
            public GameObject lockOverlay;
            public TextMeshProUGUI lockText;
            public GameObject activeBadge;
        }

        [Header("Legacy partner card visuals")]
        [SerializeField] private PartnerVisualBinding[] partnerVisuals;
        [SerializeField] private PartnerSkillVisualBinding[] partnerSkillVisuals;
        [SerializeField] private TextAsset biographyConfig;
        [SerializeField] private TextAsset partnerSkillConfig;

        [Header("Read-only detail tabs")]
        public Image attributeTab;
        public Image biographyTab;
        public RectTransform detailSwitchEffectHost;
        [SerializeField] private Sprite switchToBiographySprite;
        [SerializeField] private Sprite switchToAttributeSprite;
        public GameObject attributePanel;
        public GameObject biographyPanel;
        public TextMeshProUGUI attributeText;
        public TextMeshProUGUI biographyText;
        public TextMeshProUGUI attributeSkillText;
        public Image attributeSkillIcon;
        public GameObject attributeSkillActiveBadge;
        public GameObject attributeSkillSourceRoot;
        public GameObject attributeSkillGroupRoot;
        [SerializeField] private PartnerSkillSlotBinding[] attributeSkillSlots;
        public TextMeshProUGUI attributeMaterialNameText;
        public TextMeshProUGUI attributeMaterialLabelText;
        public Image attributeMaterialIcon;
        public TextMeshProUGUI attributeProgressText;
        public Image attributeProgressFill;
        public TextMeshProUGUI attributeIncreaseText;
        public GameObject[] attributeIncreaseArrows;
        public GameObject biographyVisualRoot;
        public Image biographyPortrait;
        public TextMeshProUGUI biographyNameText;
        public TextMeshProUGUI biographyStageText;
        public Image[] biographyTabBackgrounds;
        public TextMeshProUGUI[] biographyTabTexts;
        public GameObject[] biographyTabRedDots;
        public GameObject biographyLockPanel;
        public Image biographyLockImage;
        public TextMeshProUGUI biographyUnlockText;
        [SerializeField] private Sprite biographyTabSelectedSprite;
        [SerializeField] private Sprite biographyTabNormalSprite;
        [SerializeField] private Sprite biographyLockedSprite;
        [SerializeField] private Sprite biographyPendingSprite;

        [Header("State controls")]
        public Image followButton;
        public Image followCheck;
        public Image autoButton;
        public Image autoCheck;
        public Image nucleusButton;
        public Image trainButton;
        public TextMeshProUGUI trainButtonText;

        private readonly List<GameObject> _selectorItems = new List<GameObject>();
        private readonly List<Image> _starItems = new List<Image>();
        private readonly List<CatalogEntry> _catalog = new List<CatalogEntry>();
        private readonly Dictionary<string, StageSnapshot> _stageSnapshots =
            new Dictionary<string, StageSnapshot>();
        private readonly Dictionary<int, List<BiographySnapshot>> _biographies =
            new Dictionary<int, List<BiographySnapshot>>();
        private readonly Dictionary<string, int> _skillUnlockStages = new Dictionary<string, int>();
        // 老端 PartnerModel 按伙伴 id 保存本地自动技能状态；保持为页面进程级状态，关闭/重开不丢失。
        private static readonly HashSet<int> AutoSkill = new HashSet<int>();
        private Task _catalogLoading;
        private bool _catalogLoaded;
        private static Task _poseConfigLoading;
        private Task _prewarmTask;
        private GameObject _prewarmedPrefab;
        private string _prewarmedModelKey;
        private string _prewarmTargetKey;
        private int _prewarmSelectedIndex = -1;
        private int _prewarmTaskEpoch;
        private int _prewarmEpoch;
        private UIModelStage _modelStage;
        private int _selectedIndex = -1;
        private int _refreshEpoch;
        private int _modelEpoch;
        private string _modelKey;
        private int _voiceEpoch;
        private int _voiceIndex = -1;
        private int _voiceLoadingIndex = -1;
        private AudioManager.PlaybackHandle _voice;
        private bool _showBiography;
        private int _selectedBiographyIndex;
        private int _biographyPartnerId;
        private UIEffectStage.Handle _detailSwitchEffect;
        private int _detailSwitchEffectEpoch;
        private string _detailSwitchEffectName;
        private bool _subscribed;
        private bool _renderProbeWarned;

        /// <summary>基础模型已由专用相机渲染，并在模型 RT 中读到非透明像素。</summary>
        public bool BaseModelReady { get; private set; }

        /// <summary>基础模型、idle 与常驻骨骼特效完成后，模型 RT 再次读到非透明像素。</summary>
        public bool FullVisualReady { get; private set; }

        /// <summary>最近一次 64x64 RT 探针读到的可见像素数，供真实 Web 用例绑定 ready。</summary>
        public int ModelReadyVisiblePixels { get; private set; }

        /// <summary>固定 12 项均已绑定头像与品质卡框；与模型 RT ready 分开记账。</summary>
        public bool SelectorVisualReady { get; private set; }

        protected override void OnInit()
        {
            if (companionItemTemplate != null) companionItemTemplate.SetActive(false);
            EnsureStarItems();
            if (companionScroll != null)
            {
                companionScroll.horizontal = true;
                companionScroll.vertical = false;
                companionScroll.viewport = companionViewport;
                companionScroll.content = companionContent;
            }

            Bind(previousButton, () => SelectRelative(-1));
            Bind(nextButton, () => SelectRelative(1));
            // 老端只有一枚 _btn_switch：当前为属性时显示“传记”，当前为传记时显示“属性”。
            Bind(attributeTab, () => SetBiography(!_showBiography));
            if (biographyTab != null && biographyTab.gameObject.activeSelf)
                Bind(biographyTab, () => SetBiography(true));
            if (biographyTabBackgrounds != null)
            {
                for (int i = 0; i < biographyTabBackgrounds.Length; i++)
                {
                    int index = i;
                    Bind(biographyTabBackgrounds[i], () => SelectBiographyTab(index));
                }
            }
            Bind(autoButton, ToggleAutoSkill);
            Bind(followButton, () => LogBlocked("14203 跟随/出战"));
            Bind(nucleusButton, () => LogBlocked("14206 妖核"));
            Bind(trainButton, () => LogBlocked(Current != null && Current.IsActive ? "14205 培养" : "14204 激活"));
            Subscribe();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            int epoch = ++_refreshEpoch;
            _ = RefreshAsync(true, epoch);
        }

        protected override void OnHide()
        {
            _refreshEpoch++;
            ClearDetailSwitchEffect();
            StopVoice();
            CancelPrewarm();
            ClearModel();
        }

        protected override void OnDispose()
        {
            Cleanup();
        }

        // PetFlow.Reset 直接释放 Addressable 实例时，框架不保证转发 InternalDispose；
        // Unity 销毁钩子与 OnDispose 共用幂等清理，避免独立模型相机/RT 和事件订阅常驻。
        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _refreshEpoch++;
            ClearDetailSwitchEffect();
            Unsubscribe();
            ClearSelectorItems();
            ClearStarItems();
            StopVoice();
            CancelPrewarm();
            DisposeModel();
        }

        private PartnerModel.CompanionVo Current
        {
            get => CurrentEntry == null || !PartnerModel.Instance.HasData
                ? null
                : PartnerModel.Instance.Get(CurrentEntry.Id);
        }

        private CatalogEntry CurrentEntry =>
            _selectedIndex >= 0 && _selectedIndex < _catalog.Count ? _catalog[_selectedIndex] : null;

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_PARTNER_UPDATE, OnPartnerUpdated);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_PARTNER_UPDATE, OnPartnerUpdated);
            _subscribed = false;
        }

        private void OnPartnerUpdated()
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy) return;
            int epoch = ++_refreshEpoch;
            _ = RefreshAsync(false, epoch);
        }

        private async Task RefreshAsync(bool resetScroll, int epoch)
        {
            try
            {
                // 本页目录已经包含 id/name/figure_id，不再与 PartnerConfigs 重复加载同一 config_companion。
                await EnsureCatalogLoaded();
            }
            catch (Exception e)
            {
                if (this && epoch == _refreshEpoch)
                    GameLog.Warn("Pet", "神巫目录加载失败: {0}", e.Message);
                return;
            }
            if (!this || epoch != _refreshEpoch || !gameObject.activeInHierarchy) return;

            int selectedId = CurrentEntry?.Id ?? 0;
            bool restoreSelection = selectedId > 0;
            if (_catalog.Count == 0) _selectedIndex = -1;
            else
            {
                _selectedIndex = FindCompanionIndex(selectedId);
                if (_selectedIndex < 0) _selectedIndex = 0;
            }

            RebuildSelector();
            RefreshSelected();
            if (resetScroll && !restoreSelection) ResetSelectorScroll();
            else EnsureSelectedVisible();
        }

        private int FindCompanionIndex(int companionId)
        {
            if (companionId <= 0) return -1;
            for (int i = 0; i < _catalog.Count; i++) if (_catalog[i].Id == companionId) return i;
            return -1;
        }

        /// <summary>
        /// 老端 PartnerDetailsView 的横向栏来自完整 config_companion，而不是只列服务器已激活快照；
        /// 14202/14201 仅叠加每项状态。目录固定按 id 排序，因而 12 条选择语音也稳定按同一索引消费。
        /// </summary>
        private Task EnsureCatalogLoaded()
        {
            if (_catalogLoaded) return Task.CompletedTask;
            return _catalogLoading ?? (_catalogLoading = LoadCatalogAsync());
        }

        private async Task LoadCatalogAsync()
        {
            TextAsset asset = null;
            TextAsset stageAsset = null;
            TextAsset skillAsset = null;
            try
            {
                Task<TextAsset> catalogTask =
                    ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_companion"));
                Task<TextAsset> stageTask =
                    ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_companion_stage"));
                Task<TextAsset> skillTask =
                    ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_skill"));
                await Task.WhenAll(catalogTask, stageTask, skillTask);
                asset = catalogTask.Result;
                stageAsset = stageTask.Result;
                skillAsset = skillTask.Result;
                _catalog.Clear();
                _stageSnapshots.Clear();
                _biographies.Clear();
                _skillUnlockStages.Clear();
                if (asset != null)
                {
                    JObject root = JObject.Parse(asset.text);
                    foreach (KeyValuePair<string, JToken> pair in root)
                    {
                        if (!(pair.Value is JObject row)) continue;
                        int id = row.Value<int?>("id") ?? 0;
                        int figureId = row.Value<int?>("figure_id") ?? 0;
                        if (id <= 0 || figureId <= 0) continue;
                        List<int> skillIds = ReadInts(row.Value<string>("skill_list"));
                        _catalog.Add(new CatalogEntry
                        {
                            Id = id,
                            FigureId = figureId,
                            Name = row.Value<string>("name") ?? ("神巫" + id),
                            GoodsId = row.Value<int?>("goods_id") ?? 0,
                            GoodsNum = row.Value<int?>("goods_num") ?? 0,
                            SkillIds = skillIds,
                            SkillId = skillIds.Count > 0 ? skillIds[0] : 0,
                            Condition = row.Value<string>("condition") ?? string.Empty
                        });
                    }
                    _catalog.Sort((a, b) => a.Id.CompareTo(b.Id));
                }
                if (skillAsset != null)
                {
                    JObject skillRoot = JObject.Parse(skillAsset.text);
                    for (int i = 0; i < _catalog.Count; i++)
                    {
                        CatalogEntry entry = _catalog[i];
                        entry.SkillNames.Clear();
                        for (int skillIndex = 0; skillIndex < entry.SkillIds.Count; skillIndex++)
                        {
                            int skillId = entry.SkillIds[skillIndex];
                            entry.SkillNames.Add(skillRoot[skillId.ToString()]?.Value<string>("name") ?? string.Empty);
                        }
                        entry.SkillName = entry.SkillNames.Count > 0 ? entry.SkillNames[0] : string.Empty;
                    }
                }
                if (stageAsset != null)
                {
                    JObject stageRoot = JObject.Parse(stageAsset.text);
                    foreach (KeyValuePair<string, JToken> pair in stageRoot)
                    {
                        if (!(pair.Value is JObject row)) continue;
                        int id = row.Value<int?>("0") ?? 0;
                        int stage = row.Value<int?>("1") ?? 0;
                        int star = row.Value<int?>("2") ?? 0;
                        if (id <= 0 || stage <= 0 || star < 0) continue;
                        _stageSnapshots[StageKey(id, stage, star)] = new StageSnapshot
                        {
                            RequiredGoods = row.Value<int?>("4") ?? 0,
                            Attrs = ParseAttributes(row.Value<string>("5"))
                        };
                    }
                }
                if (biographyConfig != null)
                {
                    JObject biographyRoot = JObject.Parse(biographyConfig.text);
                    foreach (KeyValuePair<string, JToken> pair in biographyRoot)
                    {
                        if (!int.TryParse(pair.Key, out int partnerId) || !(pair.Value is JArray rows)) continue;
                        var list = new List<BiographySnapshot>();
                        foreach (JToken token in rows)
                        {
                            if (!(token is JObject row)) continue;
                            list.Add(new BiographySnapshot
                            {
                                Id = row.Value<int?>("id") ?? list.Count + 1,
                                Title = row.Value<string>("title") ?? string.Empty,
                                Description = NormalizeBiographyText(row.Value<string>("desc"))
                            });
                        }
                        list.Sort((a, b) => a.Id.CompareTo(b.Id));
                        _biographies[partnerId] = list;
                    }
                }
                if (partnerSkillConfig != null)
                {
                    JObject skillUnlockRoot = JObject.Parse(partnerSkillConfig.text);
                    foreach (KeyValuePair<string, JToken> pair in skillUnlockRoot)
                    {
                        if (!(pair.Value is JObject row)) continue;
                        int companionId = row.Value<int?>("companion_id") ?? 0;
                        int skillId = row.Value<int?>("skill_id") ?? 0;
                        int unlockStage = row.Value<int?>("unlock_stage") ?? 0;
                        if (companionId > 0 && skillId > 0 && unlockStage > 0)
                            _skillUnlockStages[SkillKey(companionId, skillId)] = unlockStage;
                    }
                }

                // 缺表时只用已有权威快照降级，不能凭空补伙伴或绑定错误模型。
                if (_catalog.Count == 0)
                {
                    IReadOnlyList<PartnerModel.CompanionVo> snapshots = PartnerModel.Instance.Companions;
                    for (int i = 0; i < snapshots.Count; i++)
                    {
                        PartnerModel.CompanionVo vo = snapshots[i];
                        if (vo == null || vo.CompanionId <= 0 || vo.FigureId <= 0) continue;
                        string name = PartnerConfigs.GetName(vo.CompanionId);
                        _catalog.Add(new CatalogEntry
                        {
                            Id = vo.CompanionId,
                            FigureId = vo.FigureId,
                            Name = string.IsNullOrEmpty(name) ? "神巫" + vo.CompanionId : name
                        });
                    }
                    _catalog.Sort((a, b) => a.Id.CompareTo(b.Id));
                    GameLog.Warn("Pet", "config_companion 缺失，神巫选择栏仅显示 14202 快照项");
                }
                _catalogLoaded = true;
            }
            finally
            {
                if (asset != null) ResManager.Release(asset);
                if (stageAsset != null) ResManager.Release(stageAsset);
                if (skillAsset != null) ResManager.Release(skillAsset);
                _catalogLoading = null;
            }
        }

        private static string NormalizeBiographyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&nbsp;", " ").Replace("<br>", Environment.NewLine)
                .Replace("<br/>", Environment.NewLine).Replace("<br />", Environment.NewLine);
        }

        private static int ReadFirstInt(string raw)
        {
            List<int> values = ReadInts(raw);
            return values.Count > 0 ? values[0] : 0;
        }

        private static List<int> ReadInts(string raw)
        {
            var result = new List<int>();
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> items = root?.Items;
            if (items == null) return result;
            for (int i = 0; i < items.Count; i++) result.Add(items[i].As<int>());
            return result;
        }

        private static List<(int attrId, long val)> ParseAttributes(string raw)
        {
            var result = new List<(int attrId, long val)>();
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> items = root?.Items;
            if (items == null) return result;
            for (int i = 0; i < items.Count; i++)
            {
                IReadOnlyList<ErlangTerm> tuple = items[i]?.Items;
                if (tuple == null || tuple.Count < 2) continue;
                result.Add((tuple[0].As<int>(), tuple[1].As<long>()));
            }
            return result;
        }

        private static string StageKey(int id, int stage, int star) => id + "@" + stage + "@" + star;
        private static string SkillKey(int id, int skillId) => id + "@" + skillId;

        private void RebuildSelector()
        {
            ClearSelectorItems();
            SelectorVisualReady = false;
            if (companionContent == null || companionItemTemplate == null) return;

            bool visualsReady = _catalog.Count > 0;

            for (int i = 0; i < _catalog.Count; i++)
            {
                int index = i;
                CatalogEntry entry = _catalog[i];
                PartnerModel.CompanionVo vo = PartnerModel.Instance.Get(entry.Id);
                GameObject item = Instantiate(companionItemTemplate, companionContent);
                item.name = "Partner_" + entry.Id;
                item.SetActive(true);

                if (!ApplyCardState(item, entry, vo, i == _selectedIndex)) visualsReady = false;
                Image hit = item.GetComponent<Image>();
                if (hit != null) Bind(hit, () => Select(index));
                _selectorItems.Add(item);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(companionContent);
            SelectorVisualReady = visualsReady;
        }

        private bool ApplyCardState(GameObject item, CatalogEntry entry, PartnerModel.CompanionVo vo,
            bool selectedState)
        {
            if (item == null || entry == null) return false;
            bool active = vo != null && vo.IsActive;
            bool fighting = active && PartnerModel.Instance.FightId == entry.Id;
            PartnerVisualBinding visual = FindPartnerVisual(entry.FigureId);

            Image cardFrame = Find<Image>(item.transform, "Frame") ?? item.GetComponent<Image>();
            if (cardFrame != null && visual?.cardFrame != null) cardFrame.sprite = visual.cardFrame;

            Image icon = Find<Image>(item.transform, "Icon");
            if (icon != null)
            {
                icon.sprite = visual?.icon;
                // 老端 Util.SetImageGray 是保亮度灰阶，不是把 RGB 乘到 0.38 后整体压黑。
                UIGrayStyle.Apply(icon, !active);
                icon.enabled = icon.sprite != null;
            }

            Image badge = Find<Image>(item.transform, "StageBadge");
            if (badge != null)
            {
                badge.sprite = visual?.stageBadge;
                badge.enabled = badge.sprite != null;
                // 老端切 skin 后按原图尺寸展开；锚点/右缘在 Prefab，尺寸只取 Sprite 原生像素。
                if (badge.sprite != null) badge.SetNativeSize();
            }

            TextMeshProUGUI name = Find<TextMeshProUGUI>(item.transform, "Name");
            if (name != null) name.text = entry.Name;
            TextMeshProUGUI stage = Find<TextMeshProUGUI>(item.transform, "Stage");
            if (stage != null)
            {
                stage.gameObject.SetActive(active);
                stage.text = active ? vo.Stage + "阶" : string.Empty;
            }

            GameObject inactive = FindChild(item.transform, "Unactive");
            if (inactive != null) inactive.SetActive(!active);
            GameObject fight = FindChild(item.transform, "Fighting");
            if (fight != null) fight.SetActive(fighting);
            Image selected = Find<Image>(item.transform, "Selected");
            if (selected != null) selected.gameObject.SetActive(selectedState);

            // 旧文本模板只作为 Prefab/C# 分批落盘时的兼容回退；完整卡片使用 Name/Stage。
            TextMeshProUGUI fallback = Find<TextMeshProUGUI>(item.transform, "Label");
            if (fallback != null)
            {
                fallback.gameObject.SetActive(name == null);
                fallback.text = entry.Name + (active ? "\n" + vo.Stage + "阶" : "\n未激活");
                fallback.color = active ? new Color(0.40f, 0.22f, 0.08f, 1f) :
                    new Color(0.46f, 0.42f, 0.40f, 1f);
            }
            return visual != null && visual.icon != null && visual.cardFrame != null &&
                visual.stageBadge != null;
        }

        private PartnerVisualBinding FindPartnerVisual(int figureId)
        {
            if (partnerVisuals == null) return null;
            for (int i = 0; i < partnerVisuals.Length; i++)
            {
                PartnerVisualBinding visual = partnerVisuals[i];
                if (visual != null && visual.figureId == figureId) return visual;
            }
            return null;
        }

        private Sprite FindPartnerSkillIcon(int skillId)
        {
            if (partnerSkillVisuals == null || skillId <= 0) return null;
            for (int i = 0; i < partnerSkillVisuals.Length; i++)
            {
                PartnerSkillVisualBinding visual = partnerSkillVisuals[i];
                if (visual != null && visual.skillId == skillId) return visual.icon;
            }
            return null;
        }

        private int ResolveSkillUnlockStage(int companionId, int skillId, int index)
        {
            if (_skillUnlockStages.TryGetValue(SkillKey(companionId, skillId), out int stage) && stage > 0)
                return stage;
            // 配置缺失时仅保持老端固定四槽的最低可见语义，不借此伪造已激活状态。
            switch (index)
            {
                case 1: return 3;
                case 2: return 6;
                case 3: return 8;
                default: return 1;
            }
        }

        private static GameObject FindChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
            return null;
        }

        private void ClearSelectorItems()
        {
            for (int i = 0; i < _selectorItems.Count; i++)
            {
                GameObject item = _selectorItems[i];
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
            }
            _selectorItems.Clear();
        }

        private void EnsureStarItems()
        {
            if (starItemTemplate == null) return;
            starItemTemplate.gameObject.SetActive(false);
            if (starText != null) starText.enabled = false;
            if (_starItems.Count == 10) return;

            ClearStarItems();
            Transform parent = starItemTemplate.transform.parent;
            for (int i = 0; i < 10; i++)
            {
                Image item = Instantiate(starItemTemplate, parent);
                item.name = "Star_" + i;
                item.raycastTarget = false;
                item.gameObject.SetActive(true);
                _starItems.Add(item);
            }
            if (parent is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void ClearStarItems()
        {
            for (int i = 0; i < _starItems.Count; i++)
            {
                Image item = _starItems[i];
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item.gameObject); else DestroyImmediate(item.gameObject);
            }
            _starItems.Clear();
        }

        private void RefreshStarItems(int stars)
        {
            EnsureStarItems();
            for (int i = 0; i < _starItems.Count; i++)
            {
                Image item = _starItems[i];
                if (item == null) continue;
                item.sprite = i < stars ? activeStarSprite : inactiveStarSprite;
                item.enabled = item.sprite != null;
            }
        }

        private void SelectRelative(int delta)
        {
            int count = _catalog.Count;
            if (count == 0) return;
            int next = _selectedIndex < 0 ? 0 : (_selectedIndex + delta + count) % count;
            Select(next);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _catalog.Count || index == _selectedIndex) return;
            _selectedIndex = index;
            RefreshSelected();
            for (int i = 0; i < _selectorItems.Count; i++)
            {
                Image selected = Find<Image>(_selectorItems[i].transform, "Selected");
                if (selected != null) selected.gameObject.SetActive(i == _selectedIndex);
            }
            EnsureSelectedVisible();
        }

        private void ResetSelectorScroll()
        {
            if (companionScroll == null) return;
            companionScroll.StopMovement();
            companionScroll.velocity = Vector2.zero;
            companionScroll.horizontalNormalizedPosition = 0f;
        }

        /// <summary>
        /// 重开保留详情时不能把列表强制回到首项；按独立 Viewport 的真实边界只移动必要距离，
        /// 既让选中项完整进入裁剪区，也保留用户此前的横向位置。
        /// </summary>
        private void EnsureSelectedVisible()
        {
            if (companionScroll == null || companionViewport == null || companionContent == null ||
                _selectedIndex < 0 || _selectedIndex >= _selectorItems.Count) return;
            RectTransform selected = _selectorItems[_selectedIndex] != null
                ? _selectorItems[_selectedIndex].transform as RectTransform
                : null;
            if (selected == null) return;

            companionScroll.StopMovement();
            companionScroll.velocity = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(companionContent);
            Canvas.ForceUpdateCanvases();

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(companionViewport, selected);
            Rect viewportRect = companionViewport.rect;
            float offset = 0f;
            if (bounds.min.x < viewportRect.xMin) offset = viewportRect.xMin - bounds.min.x;
            else if (bounds.max.x > viewportRect.xMax) offset = viewportRect.xMax - bounds.max.x;
            if (Mathf.Abs(offset) > 0.01f)
            {
                companionContent.anchoredPosition += new Vector2(offset, 0f);
                Canvas.ForceUpdateCanvases();
                companionScroll.horizontalNormalizedPosition =
                    Mathf.Clamp01(companionScroll.horizontalNormalizedPosition);
            }
        }

        private void RefreshSelected()
        {
            CatalogEntry entry = CurrentEntry;
            PartnerModel.CompanionVo vo = Current;
            bool hasEntry = entry != null;
            bool active = vo != null && vo.IsActive;
            bool fighting = active && PartnerModel.Instance.FightId == entry.Id;
            if (nameText != null)
                nameText.text = hasEntry ? entry.Name + (active ? " " + vo.Stage + "阶" : string.Empty) : "暂无神巫";
            if (stageText != null) stageText.text = active ? vo.Stage + "阶" : "未激活";
            if (combatText != null) combatText.text = vo != null ? "战力 " + vo.Combat : "战力 0";
            int stars = active ? Mathf.Clamp(vo.Star, 0, 10) : 0;
            RefreshStarItems(stars);
            if (starText != null)
            {
                starText.text = hasEntry
                    ? new string('★', stars) + new string('☆', 10 - stars)
                    : "☆☆☆☆☆☆☆☆☆☆";
                starText.enabled = starItemTemplate == null;
            }
            if (stateText != null)
                stateText.text = !hasEntry ? "未取得配置目录" : (fighting ? "出战中" : (active ? "已激活" : "未激活"));
            if (blessingText != null) blessingText.text = active ? "祝福 " + vo.Blessing : "祝福 --";
            if (trainCountText != null) trainCountText.text = active ? "妖核培养 " + vo.TrainNum + " 次" : "妖核培养 --";
            if (followButton != null) followButton.gameObject.SetActive(active);
            if (autoButton != null) autoButton.gameObject.SetActive(active);
            if (nucleusButton != null) nucleusButton.gameObject.SetActive(active);
            if (trainButton != null) trainButton.gameObject.SetActive(hasEntry);
            if (followCheck != null) followCheck.gameObject.SetActive(fighting);
            if (autoCheck != null) autoCheck.gameObject.SetActive(active && AutoSkill.Contains(entry.Id));
            if (trainButtonText != null) trainButtonText.text = active ? "升星" : "激活";
            // 老端以真实 ScrollRect 拖动和卡片点击切换，没有额外左右翻页箭头。
            if (previousButton != null) previousButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            RefreshAttributeDetails(entry, vo);
            RefreshBiographyDetails(entry, vo);
            SetBiography(_showBiography);
            if (!_showBiography) RefreshModel(entry?.FigureId ?? vo?.FigureId ?? 0);
            StartVoice(_selectedIndex);
        }

        private void RefreshAttributeDetails(CatalogEntry entry, PartnerModel.CompanionVo vo)
        {
            bool active = vo != null && vo.IsActive;
            StageSnapshot currentStage = ResolveStageSnapshot(entry, vo);
            StageSnapshot nextStage = ResolveNextStageSnapshot(entry, vo);
            IReadOnlyList<(int attrId, long val)> attrs =
                vo?.Attrs != null && vo.Attrs.Count > 0 ? vo.Attrs : currentStage?.Attrs;

            if (attributeText != null) attributeText.text = FormatAttributes(attrs);
            if (attributeIncreaseText != null)
                attributeIncreaseText.text = active ? FormatAttributeIncrease(attrs, nextStage?.Attrs) : string.Empty;
            bool hasIncrease = active && !string.IsNullOrEmpty(attributeIncreaseText?.text);
            if (attributeIncreaseArrows != null)
                for (int i = 0; i < attributeIncreaseArrows.Length; i++)
                    if (attributeIncreaseArrows[i] != null) attributeIncreaseArrows[i].SetActive(hasIncrease);

            PartnerVisualBinding visual = FindPartnerVisual(entry?.FigureId ?? 0);
            if (attributeSkillSourceRoot != null) attributeSkillSourceRoot.SetActive(!active);
            if (attributeSkillGroupRoot != null) attributeSkillGroupRoot.SetActive(active);
            if (attributeSkillIcon != null)
            {
                attributeSkillIcon.sprite = visual?.skillIcon;
                attributeSkillIcon.enabled = attributeSkillIcon.sprite != null;
            }
            if (attributeSkillActiveBadge != null) attributeSkillActiveBadge.SetActive(false);
            if (attributeSkillText != null)
            {
                string condition = string.IsNullOrEmpty(entry?.Condition) ? "来源待配置" : entry.Condition;
                string skillName = !string.IsNullOrEmpty(entry?.SkillName)
                    ? entry.SkillName : "技能 " + (entry?.SkillId ?? 0);
                attributeSkillText.text = "<color=#f28abd>" + condition + "</color>  " + skillName +
                    Environment.NewLine + (active ? "已激活" : "未激活");
            }
            if (attributeSkillSlots != null)
            {
                for (int i = 0; i < attributeSkillSlots.Length; i++)
                {
                    PartnerSkillSlotBinding slot = attributeSkillSlots[i];
                    if (slot == null) continue;
                    int skillId = entry != null && i < entry.SkillIds.Count ? entry.SkillIds[i] : 0;
                    Sprite icon = FindPartnerSkillIcon(skillId);
                    if (slot.icon != null)
                    {
                        slot.icon.sprite = icon;
                        slot.icon.enabled = active && icon != null;
                    }
                    int unlockStage = ResolveSkillUnlockStage(entry?.Id ?? 0, skillId, i);
                    bool unlocked = active && vo != null && vo.Stage >= unlockStage;
                    if (slot.icon != null)
                        slot.icon.color = unlocked ? Color.white : new Color32(0x72, 0x72, 0x72, 0xff);
                    if (slot.lockOverlay != null) slot.lockOverlay.SetActive(active && !unlocked);
                    if (slot.lockText != null) slot.lockText.text = unlockStage + "阶\n激活";
                    if (slot.activeBadge != null) slot.activeBadge.SetActive(active && unlocked && i == 0);
                }
            }
            if (attributeMaterialIcon != null)
            {
                attributeMaterialIcon.sprite = visual?.goodsIcon;
                attributeMaterialIcon.enabled = attributeMaterialIcon.sprite != null;
            }
            if (attributeMaterialNameText != null)
            {
                attributeMaterialNameText.text = visual != null && !string.IsNullOrEmpty(visual.unlockName)
                    ? visual.unlockName : entry?.Name ?? string.Empty;
                attributeMaterialNameText.color = ResolveGoodsNameColor(entry?.Id ?? 0);
            }
            if (attributeMaterialLabelText != null)
            {
                attributeMaterialLabelText.text = active ? "升星消耗：" : "激活消耗：";
                attributeMaterialLabelText.color = new Color32(0x61, 0x36, 0x17, 0xff);
            }

            int required = active ? (currentStage?.RequiredGoods ?? 0) : (entry?.GoodsNum ?? 0);
            long current = active ? Math.Max(0L, vo?.Blessing ?? 0L) : 0L;
            if (attributeProgressText != null)
            {
                attributeProgressText.text = current + "/" + Math.Max(0, required);
                attributeProgressText.color = new Color32(0x6b, 0x52, 0x33, 0xff);
            }
            if (attributeProgressFill != null)
            {
                attributeProgressFill.type = Image.Type.Filled;
                attributeProgressFill.fillMethod = Image.FillMethod.Horizontal;
                attributeProgressFill.fillOrigin = 0;
                attributeProgressFill.fillAmount = required > 0
                    ? Mathf.Clamp01((float)current / required)
                    : 0f;
            }
        }

        private StageSnapshot ResolveStageSnapshot(CatalogEntry entry, PartnerModel.CompanionVo vo)
        {
            if (entry == null) return null;
            int stage = vo != null && vo.Stage > 0 ? vo.Stage : 1;
            int star = vo != null && vo.IsActive ? Mathf.Clamp(vo.Star, 0, 10) : 0;
            _stageSnapshots.TryGetValue(StageKey(entry.Id, stage, star), out StageSnapshot snapshot);
            if (snapshot == null) _stageSnapshots.TryGetValue(StageKey(entry.Id, 1, 0), out snapshot);
            return snapshot;
        }

        private StageSnapshot ResolveNextStageSnapshot(CatalogEntry entry, PartnerModel.CompanionVo vo)
        {
            if (entry == null || vo == null || !vo.IsActive) return null;
            int stage = Math.Max(1, vo.Stage);
            int star = Mathf.Clamp(vo.Star, 0, 10);
            string key = star < 10
                ? StageKey(entry.Id, stage, star + 1)
                : StageKey(entry.Id, stage + 1, 1);
            _stageSnapshots.TryGetValue(key, out StageSnapshot snapshot);
            return snapshot;
        }

        private static string FormatAttributes(IReadOnlyList<(int attrId, long val)> attrs)
        {
            if (attrs == null || attrs.Count == 0) return "暂无属性数据";
            var sb = new StringBuilder();
            for (int i = 0; i < attrs.Count; i++)
            {
                (int attrId, long val) attr = attrs[i];
                if (i > 0)
                {
                    if (i % 2 == 0) sb.AppendLine();
                    else sb.Append("              ");
                }
                sb.Append(GetAttributeName(attr.attrId)).Append("：<color=#d15e00>")
                    .Append(attr.val).Append("</color>");
            }
            return sb.ToString();
        }

        private static string FormatAttributeIncrease(IReadOnlyList<(int attrId, long val)> current,
            IReadOnlyList<(int attrId, long val)> next)
        {
            if (current == null || next == null || current.Count == 0 || next.Count == 0) return string.Empty;
            var values = new Dictionary<int, long>();
            for (int i = 0; i < current.Count; i++) values[current[i].attrId] = current[i].val;
            var sb = new StringBuilder();
            for (int i = 0; i < next.Count; i++)
            {
                if (!values.TryGetValue(next[i].attrId, out long now) || next[i].val <= now) continue;
                if (sb.Length > 0)
                {
                    if (i % 2 == 0) sb.AppendLine();
                    else sb.Append("              ");
                }
                sb.Append("<color=#0a953e>+").Append(next[i].val - now).Append("</color>");
            }
            return sb.ToString();
        }

        private static string GetAttributeName(int attrId)
        {
            switch (attrId)
            {
                case 1: return "攻击";
                case 2: return "生命";
                case 3: return "破甲";
                case 4: return "防御";
                default: return "属性" + attrId;
            }
        }

        private static Color32 ResolveGoodsNameColor(int companionId)
        {
            // 当前 config_companion/config_goods 固定 12 项的品质分段为 3/4/5/7。
            if (companionId <= 2) return new Color32(0xc7, 0x62, 0xef, 0xff);
            if (companionId <= 4) return new Color32(0x4f, 0x96, 0xff, 0xff);
            if (companionId <= 6) return new Color32(0xff, 0x8a, 0x32, 0xff);
            return new Color32(0xff, 0x72, 0xc2, 0xff);
        }

        private void SelectBiographyTab(int index)
        {
            if (index < 0 || index >= 3) return;
            _selectedBiographyIndex = index;
            RefreshBiographyDetails(CurrentEntry, Current);
        }

        private void RefreshBiographyDetails(CatalogEntry entry, PartnerModel.CompanionVo vo)
        {
            if (entry == null) return;
            if (_biographyPartnerId != entry.Id)
            {
                _biographyPartnerId = entry.Id;
                _selectedBiographyIndex = 0;
            }

            PartnerVisualBinding visual = FindPartnerVisual(entry.FigureId);
            if (biographyPortrait != null)
            {
                biographyPortrait.sprite = visual?.biography;
                biographyPortrait.enabled = biographyPortrait.sprite != null;
            }
            if (biographyNameText != null) biographyNameText.text = entry.Name;
            if (biographyStageText != null)
                biographyStageText.text = (vo != null && vo.Stage > 0 ? vo.Stage : 1) + "阶";

            int selectedStatus = 0;
            for (int i = 0; i < 3; i++)
            {
                int thresholdStage = 1 + i * 2;
                bool unlocked = vo?.BiogLvs != null && vo.BiogLvs.Contains(i + 1);
                bool reached = vo != null && vo.IsActive &&
                    (vo.Stage > thresholdStage || vo.Stage == thresholdStage && vo.Star >= 10);
                int status = unlocked ? 2 : reached ? 1 : 0;
                if (i == _selectedBiographyIndex) selectedStatus = status;

                if (biographyTabBackgrounds != null && i < biographyTabBackgrounds.Length &&
                    biographyTabBackgrounds[i] != null)
                    biographyTabBackgrounds[i].sprite =
                        i == _selectedBiographyIndex ? biographyTabSelectedSprite : biographyTabNormalSprite;
                if (biographyTabTexts != null && i < biographyTabTexts.Length && biographyTabTexts[i] != null)
                {
                    biographyTabTexts[i].text = unlocked ? "传记·" + GetChineseNumber(i + 1) : "未解锁";
                    biographyTabTexts[i].color = i == _selectedBiographyIndex
                        ? new Color32(0x9b, 0x57, 0x2f, 0xff)
                        : Color.white;
                }
                if (biographyTabRedDots != null && i < biographyTabRedDots.Length &&
                    biographyTabRedDots[i] != null)
                    biographyTabRedDots[i].SetActive(status == 1);
            }

            int unlockStage = 1 + _selectedBiographyIndex * 2;
            string unlockName = visual != null && !string.IsNullOrEmpty(visual.unlockName)
                ? visual.unlockName : entry.Name;
            if (biographyUnlockText != null)
            {
                // 所有片段显式着色，避免 TMP 默认白色把“达到/解锁”吞进半透明标题条。
                biographyUnlockText.text = "<color=#ff72c2>" + unlockName +
                    "</color><color=#d15e00>达到</color><color=#ef4848>" + unlockStage +
                    "阶10星</color><color=#d15e00>解锁</color>";
                biographyUnlockText.color = new Color32(0xd1, 0x5e, 0x00, 0xff);
            }
            if (biographyLockPanel != null) biographyLockPanel.SetActive(selectedStatus != 2);
            if (biographyLockImage != null)
            {
                biographyLockImage.sprite = selectedStatus == 1 ? biographyPendingSprite : biographyLockedSprite;
                biographyLockImage.enabled = selectedStatus != 2 && biographyLockImage.sprite != null;
            }
            if (biographyText != null)
            {
                biographyText.gameObject.SetActive(selectedStatus == 2);
                BiographySnapshot snapshot = FindBiography(entry.Id, _selectedBiographyIndex);
                biographyText.text = selectedStatus == 2 && snapshot != null
                    ? "<color=#d15e00>" + snapshot.Title + "</color>" + Environment.NewLine + snapshot.Description
                    : string.Empty;
            }
        }

        private BiographySnapshot FindBiography(int partnerId, int index)
        {
            if (!_biographies.TryGetValue(partnerId, out List<BiographySnapshot> rows) ||
                rows == null || index < 0 || index >= rows.Count)
                return null;
            return rows[index];
        }

        private static string GetChineseNumber(int number)
        {
            switch (number)
            {
                case 2: return "贰";
                case 3: return "叁";
                default: return "壹";
            }
        }

        private void SetBiography(bool biography)
        {
            _showBiography = biography;
            if (attributePanel != null) attributePanel.SetActive(!biography);
            if (biographyPanel != null) biographyPanel.SetActive(biography);
            if (biographyVisualRoot != null) biographyVisualRoot.SetActive(biography);
            if (modelHost != null) modelHost.gameObject.SetActive(!biography);
            bool active = Current != null && Current.IsActive;
            bool hasEntry = CurrentEntry != null;
            if (followButton != null) followButton.gameObject.SetActive(!biography && active);
            if (autoButton != null) autoButton.gameObject.SetActive(!biography && active);
            if (nucleusButton != null) nucleusButton.gameObject.SetActive(!biography && active);
            if (trainButton != null) trainButton.gameObject.SetActive(!biography && hasEntry);
            if (combatText != null) combatText.gameObject.SetActive(!biography);
            if (nameText != null && nameText.transform.parent != null)
                nameText.transform.parent.gameObject.SetActive(!biography);
            if (stageText != null) stageText.gameObject.SetActive(!biography);
            if (stateText != null) stateText.gameObject.SetActive(!biography);
            if (blessingText != null) blessingText.gameObject.SetActive(!biography);
            if (trainCountText != null) trainCountText.gameObject.SetActive(!biography);
            if (starText != null)
            {
                starText.enabled = starItemTemplate == null;
            }
            if (attributeTab != null)
            {
                attributeTab.color = Color.white;
                attributeTab.sprite = biography ? switchToAttributeSprite : switchToBiographySprite;
            }
            if (biographyTab != null) biographyTab.gameObject.SetActive(false);
            RefreshDetailSwitchEffect();
            if (!biography && hasEntry)
                RefreshModel(CurrentEntry.FigureId);
        }

        private void RefreshDetailSwitchEffect()
        {
            if (!IsShown || detailSwitchEffectHost == null || !detailSwitchEffectHost.gameObject.activeInHierarchy)
                return;
            // 老端：属性态展示“传记”效果，传记态展示“属性”效果。
            string effectName = _showBiography ? "effect_ui_shuxing" : "effect_ui_zhuanji";
            if (_detailSwitchEffectName == effectName) return;
            int epoch = ++_detailSwitchEffectEpoch;
            _detailSwitchEffect?.Dispose();
            _detailSwitchEffect = null;
            _detailSwitchEffectName = effectName;
            _ = LoadDetailSwitchEffectAsync(effectName, epoch);
        }

        private async Task LoadDetailSwitchEffectAsync(string effectName, int epoch)
        {
            UIEffectStage.Handle handle = null;
            try
            {
                handle = await UIEffectStage.AddAsync(effectName, detailSwitchEffectHost,
                    Vector2.zero, new Vector3(5f, 5f, 5f), 0f, new Vector2(108f, 275f));
                if (this == null || !IsShown || epoch != _detailSwitchEffectEpoch ||
                    detailSwitchEffectHost == null)
                {
                    handle?.Dispose();
                    return;
                }
                _detailSwitchEffect = handle;
            }
            catch (Exception exception)
            {
                handle?.Dispose();
                if (epoch == _detailSwitchEffectEpoch) _detailSwitchEffectName = null;
                GameLog.Warn("Pet", "神巫属性/传记切签特效加载失败 effect={0} error={1}",
                    effectName, exception.Message);
            }
        }

        private void ClearDetailSwitchEffect()
        {
            _detailSwitchEffectEpoch++;
            _detailSwitchEffect?.Dispose();
            _detailSwitchEffect = null;
            _detailSwitchEffectName = null;
        }

        private void ToggleAutoSkill()
        {
            PartnerModel.CompanionVo vo = Current;
            if (vo == null || !vo.IsActive) return;
            if (!AutoSkill.Add(vo.CompanionId)) AutoSkill.Remove(vo.CompanionId);
            if (autoCheck != null) autoCheck.gameObject.SetActive(AutoSkill.Contains(vo.CompanionId));
            GameLog.Info("Pet", "神巫自动技能本地状态 companion={0} enabled={1}",
                vo.CompanionId, AutoSkill.Contains(vo.CompanionId));
        }

        private void LogBlocked(string leaf)
        {
            GameLog.Info("Pet", "神巫事务叶保持 blocked: leaf={0} companion={1}", leaf, CurrentEntry?.Id ?? 0);
        }

        private void RefreshModel(int figureId)
        {
            if (modelHost == null || figureId <= 0)
            {
                ClearModel();
                return;
            }
            string name = "model_pet_" + figureId;
            string address = "object/pet/" + name + "/" + name;
            if (_modelKey == address) return;
            _modelStage?.ClearStage();
            _modelKey = address;
            BaseModelReady = false;
            FullVisualReady = false;
            ModelReadyVisiblePixels = 0;
            int epoch = ++_modelEpoch;
            _ = LoadModelAsync(epoch, address, figureId);
        }

        private void StartVoice(int index)
        {
            if (index < 0 || index >= PartnerVoices.Length)
            {
                StopVoice();
                return;
            }
            if (_voiceIndex == index && (_voice != null || _voiceLoadingIndex == index)) return;
            StopVoice();
            _voiceIndex = index;
            _voiceLoadingIndex = index;
            int epoch = _voiceEpoch;
            _ = StartVoiceAsync(PartnerVoices[index], index, epoch);
        }

        private async Task StartVoiceAsync(string name, int index, int epoch)
        {
            AudioManager.PlaybackHandle handle = null;
            try
            {
                handle = await AudioManager.PlayNpc(name);
                if (!this || epoch != _voiceEpoch || index != _voiceIndex || !gameObject.activeInHierarchy)
                {
                    handle?.Stop();
                    return;
                }
                _voice?.Stop();
                _voice = handle;
                handle = null;
            }
            catch (Exception e)
            {
                handle?.Stop();
                if (this && epoch == _voiceEpoch && index == _voiceIndex)
                    GameLog.Warn("Pet", "神巫语音加载失败 name={0} error={1}", name, e.Message);
            }
            finally
            {
                if (this && epoch == _voiceEpoch && index == _voiceIndex && _voiceLoadingIndex == index)
                    _voiceLoadingIndex = -1;
            }
        }

        private void StopVoice()
        {
            _voiceEpoch++;
            _voiceIndex = -1;
            _voiceLoadingIndex = -1;
            _voice?.Stop();
            _voice = null;
        }

        private async Task LoadModelAsync(int epoch, string address, int figureId)
        {
            GameObject prefab = null;
            GameObject instance = null;
            Task<GameObject> prefabTask = null;
            try
            {
                Task prewarm = _prewarmTask;
                bool samePrewarm = prewarm != null && !prewarm.IsCompleted &&
                    _prewarmTaskEpoch == _prewarmEpoch &&
                    (_prewarmTargetKey == address ||
                     (_prewarmTargetKey == null && _prewarmSelectedIndex == _selectedIndex));
                if (samePrewarm) await prewarm;
                if (!IsCurrentModel(epoch, address)) return;

                prefab = TakePrewarmedPrefab(address);
                ReleasePrewarmedIfDifferent(address);
                prefabTask = prefab != null
                    ? Task.FromResult(prefab)
                    : ResManager.LoadAsync<GameObject>(address);
                Task poseTask = EnsurePoseConfigLoaded();
                await Task.WhenAll(prefabTask, poseTask);
                prefab = prefabTask.Result;

                if (!IsCurrentModel(epoch, address) || modelHost == null) return;
                if (prefab == null)
                {
                    GameLog.Warn("Pet", "神巫模型缺失: {0}", address);
                    _modelKey = null;
                    return;
                }

                instance = Instantiate(prefab);
                LoadedAssetReleaser.Track(instance, prefab);
                prefab = null; // 引用所有权已转交实例。
                if (!IsCurrentModel(epoch, address)) return;

                if (_modelStage == null) _modelStage = new UIModelStage();
                _modelStage.EnableDragRotate(true);
                UiModelParameterConfigs.ModelParam mp =
                    ClientOutWardPosConfigs.Get("p_" + figureId, "default_partner");
                // 老 H5 的 Partner 模型相机从角色正面取景；同一份转换模型在 Unity 模型台 0 度会背向镜头。
                // 只在神巫专页补足半圈，不改共享 UIModelStage 或其他外观页。
                _modelStage.PlaceInstance(modelHost, instance, mp.Scale, mp.Position,
                    Mathf.Repeat(mp.Rotate + 180f, 360f));

                Task<bool> baseReadyTask = AwaitRenderedModelAsync(epoch, address, false);
                Task effectTask = EffectBinder.AttachAlways(instance, "pet", figureId.ToString());
                // idle 不参与基础模型上台门槛；独立加载并由旧实例/epoch 负责晚到回收。
                Task idleApplyTask = PlayIdleAsync(instance, figureId, null);

                bool baseReady = await baseReadyTask;
                if (IsCurrentModel(epoch, address)) BaseModelReady = baseReady;
                // 即使切项/关页，也要观察完已启动的加载任务；其资源跟随旧实例销毁，不能留下晚到异常。
                await Task.WhenAll(effectTask, idleApplyTask);
                if (!IsCurrentModel(epoch, address)) return;
                bool fullReady = await AwaitRenderedModelAsync(epoch, address, true);
                if (!IsCurrentModel(epoch, address)) return;
                FullVisualReady = fullReady;
                if (FullVisualReady) BaseModelReady = true;
            }
            catch (Exception e)
            {
                if (this && epoch == _modelEpoch && _modelKey == address)
                {
                    _modelKey = null;
                    BaseModelReady = false;
                    FullVisualReady = false;
                    ModelReadyVisiblePixels = 0;
                    _modelStage?.ClearStage();
                    GameLog.Warn("Pet", "神巫模型加载失败 address={0} error={1}", address, e.Message);
                }
            }
            finally
            {
                if (prefab == null && instance == null && prefabTask != null &&
                    prefabTask.Status == TaskStatus.RanToCompletion)
                    prefab = prefabTask.Result;
                if (prefab != null) ResManager.Release(prefab);
                if (instance != null && !IsCurrentModel(epoch, address)) Destroy(instance);
            }
        }

        private static async Task PlayIdleAsync(GameObject instance, int figureId, AnimationClip preloadedClip)
        {
            AnimationClip clip = preloadedClip;
            bool transferred = false;
            try
            {
                if (instance == null) return;
                Animation anim = instance.GetComponent<Animation>();
                if (anim != null && anim.GetClip("idle") != null)
                {
                    anim.Play("idle");
                    return;
                }
                if (clip == null)
                    clip = await ResManager.LoadAsync<AnimationClip>("object/pet/action/" + figureId + "/idle");
                if (instance == null || clip == null) return;
                LoadedAssetReleaser.Track(instance, clip);
                transferred = true;
                if (anim == null) anim = instance.AddComponent<Animation>();
                if (anim.GetClip("idle") == null) anim.AddClip(clip, "idle");
                anim.Play("idle");
            }
            finally
            {
                if (!transferred && clip != null) ResManager.Release(clip);
            }
        }

        /// <summary>
        /// Pet 窗打开后、第三 Tab 被点击前，只保留当前一套精确模型引用。
        /// 不预热 12 项，不创建隐藏模型台；关闭、Reset、目标变化或晚到都会归还引用。
        /// </summary>
        internal void BeginPrewarm()
        {
            if (!this || _prewarmedPrefab != null || (_prewarmTask != null && !_prewarmTask.IsCompleted)) return;
            int epoch = ++_prewarmEpoch;
            int requestedIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
            _prewarmTaskEpoch = epoch;
            _prewarmSelectedIndex = requestedIndex;
            _prewarmTargetKey = null;
            _prewarmTask = PrewarmCurrentModelAsync(epoch, requestedIndex);
        }

        internal void CancelPrewarm()
        {
            _prewarmEpoch++;
            _prewarmTargetKey = null;
            _prewarmSelectedIndex = -1;
            ReleasePrewarmedPrefab();
        }

        private async Task PrewarmCurrentModelAsync(int epoch, int requestedIndex)
        {
            GameObject prefab = null;
            Task<GameObject> prefabTask = null;
            try
            {
                Task poseTask = EnsurePoseConfigLoaded();
                await Task.WhenAll(EnsureCatalogLoaded(), poseTask);
                if (!this || epoch != _prewarmEpoch || _catalog.Count == 0) return;

                int index = Mathf.Clamp(requestedIndex, 0, _catalog.Count - 1);
                CatalogEntry entry = _catalog[index];
                string name = "model_pet_" + entry.FigureId;
                string address = "object/pet/" + name + "/" + name;
                if (epoch == _prewarmEpoch) _prewarmTargetKey = address;
                prefabTask = ResManager.LoadAsync<GameObject>(address);
                await prefabTask;
                prefab = prefabTask.Result;
                if (!this || epoch != _prewarmEpoch || prefab == null) return;
                if (gameObject.activeInHierarchy && _selectedIndex >= 0 && _selectedIndex != index) return;

                ReleasePrewarmedPrefab();
                _prewarmedPrefab = prefab;
                _prewarmedModelKey = address;
                prefab = null;
                GameLog.Info("Pet", "神巫精确预热完成 address={0}", address);
            }
            catch (Exception e)
            {
                if (this && epoch == _prewarmEpoch)
                    GameLog.Warn("Pet", "神巫精确预热失败: {0}", e.Message);
            }
            finally
            {
                if (prefab == null && prefabTask != null && prefabTask.Status == TaskStatus.RanToCompletion &&
                    _prewarmedPrefab != prefabTask.Result)
                    prefab = prefabTask.Result;
                if (prefab != null) ResManager.Release(prefab);
            }
        }

        private GameObject TakePrewarmedPrefab(string address)
        {
            if (_prewarmedPrefab == null || _prewarmedModelKey != address) return null;
            GameObject prefab = _prewarmedPrefab;
            _prewarmedPrefab = null;
            _prewarmedModelKey = null;
            return prefab;
        }

        private void ReleasePrewarmedIfDifferent(string address)
        {
            if (_prewarmedPrefab != null && _prewarmedModelKey != address) ReleasePrewarmedPrefab();
        }

        private void ReleasePrewarmedPrefab()
        {
            if (_prewarmedPrefab != null) ResManager.Release(_prewarmedPrefab);
            _prewarmedPrefab = null;
            _prewarmedModelKey = null;
        }

        private static Task EnsurePoseConfigLoaded()
        {
            return _poseConfigLoading ?? (_poseConfigLoading = LoadPoseConfigAsync());
        }

        private static async Task LoadPoseConfigAsync()
        {
            try
            {
                await ClientOutWardPosConfigs.EnsureLoaded();
            }
            catch
            {
                _poseConfigLoading = null;
                throw;
            }
        }

        private bool IsCurrentModel(int epoch, string address)
        {
            return this && epoch == _modelEpoch && _modelKey == address && gameObject.activeInHierarchy;
        }

        private async Task<bool> AwaitRenderedModelAsync(int epoch, string address, bool fullVisual)
        {
            const int minVisiblePixels = 8;
            // 先给 Animation/ParticleSystem 一个玩家帧，再显式渲染；第二帧只做一次 64x64 读回。
            // 禁止循环同步 GPU readback，否则 ready 探针本身会制造 Web 冷开卡顿。
            await Task.Yield();
            if (!IsCurrentModel(epoch, address) || _modelStage == null) return false;
            _modelStage.RenderStageNow();
            await Task.Yield();
            if (!IsCurrentModel(epoch, address) || _modelStage == null) return false;
            _modelStage.RenderStageNow();
            int visible = ProbeVisiblePixels();
            if (visible < 0) return false;
            ModelReadyVisiblePixels = visible;
            bool ready = visible >= minVisiblePixels;
            if (ready)
                GameLog.Info("Pet", "神巫模型RT出帧 ready={0} pixels={1} address={2}",
                    fullVisual ? "full" : "base", visible, address);
            else
                GameLog.Warn("Pet", "神巫模型RT未出帧 ready={0} pixels={1} address={2}",
                    fullVisual ? "full" : "base", visible, address);
            return ready;
        }

        private int ProbeVisiblePixels()
        {
            RenderTexture source = null;
            if (modelHost != null)
            {
                foreach (RawImage image in modelHost.GetComponentsInChildren<RawImage>(true))
                {
                    if (image.texture is RenderTexture rt) { source = rt; break; }
                }
            }
            if (source == null || !source.IsCreated()) return 0;

            RenderTexture previous = RenderTexture.active;
            RenderTexture sampleRt = null;
            Texture2D sample = null;
            try
            {
                sampleRt = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                Graphics.Blit(source, sampleRt);
                RenderTexture.active = sampleRt;
                sample = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
                sample.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0, false);
                sample.Apply(false, false);
                Color32[] pixels = sample.GetPixels32();
                int visible = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 p = pixels[i];
                    if (p.a > 8 && (p.r > 4 || p.g > 4 || p.b > 4)) visible++;
                }
                return visible;
            }
            catch (Exception e)
            {
                if (!_renderProbeWarned)
                {
                    _renderProbeWarned = true;
                    GameLog.Warn("Pet", "神巫模型RT像素探针失败: {0}", e.Message);
                }
                return -1;
            }
            finally
            {
                RenderTexture.active = previous;
                if (sampleRt != null) RenderTexture.ReleaseTemporary(sampleRt);
                if (sample != null)
                {
                    if (Application.isPlaying) Destroy(sample); else DestroyImmediate(sample);
                }
            }
        }

        private void ClearModel()
        {
            _modelEpoch++;
            _modelKey = null;
            BaseModelReady = false;
            FullVisualReady = false;
            ModelReadyVisiblePixels = 0;
            _modelStage?.ClearStage();
        }

        private void DisposeModel()
        {
            _modelEpoch++;
            _modelKey = null;
            BaseModelReady = false;
            FullVisualReady = false;
            ModelReadyVisiblePixels = 0;
            _modelStage?.Dispose();
            _modelStage = null;
        }

        private static void Bind(Image image, System.Action action)
        {
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private static T Find<T>(Transform root, string name) where T : Component
        {
            if (root == null) return null;
            foreach (T component in root.GetComponentsInChildren<T>(true))
                if (component.name == name) return component;
            return null;
        }

        private sealed class CatalogEntry
        {
            public int Id;
            public int FigureId;
            public string Name;
            public int GoodsId;
            public int GoodsNum;
            public int SkillId;
            public string SkillName;
            public List<int> SkillIds = new List<int>();
            public List<string> SkillNames = new List<string>();
            public string Condition;
        }

        private sealed class StageSnapshot
        {
            public int RequiredGoods;
            public List<(int attrId, long val)> Attrs;
        }

        private sealed class BiographySnapshot
        {
            public int Id;
            public string Title;
            public string Description;
        }
    }
}
