using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.TempleAwaken
{
    /// <summary>
    /// 天命觉醒(神殿觉醒之路)数据层(对标老端 TempleAwakenModel.ts;服务端 pt_429 lib_temple_awaken)。
    /// 主线卡点 #64:task 100590(ctype81 Open_function)要求发 42900 完成初始任务开启觉醒之路。
    /// 无轮询——42909 是服务端对前置任务(100580)完成态的推送;42900 是客户端唯一发起点,成功后
    /// 服务端 open_temple_awaken 推进 100590,由通用 30001 任务推送自动刷新(本层不重复维护任务态)。
    /// </summary>
    public sealed class TempleAwakenModel
    {
        public static readonly TempleAwakenModel Instance = new TempleAwakenModel();
        private TempleAwakenModel() { }

        /// <summary>前置任务(100580)是否已完成(42909 is_finish:c 推送)。</summary>
        public bool PreTaskFinished { get; private set; }

        /// <summary>觉醒之路是否已开启(42900 error_code==1 成功)。</summary>
        public bool Opened { get; private set; }
        public sealed class StageEntry { public ushort Stage { get; } public byte Status { get; } public ulong Process { get; } public StageEntry(ushort stage, byte status, ulong process) { Stage = stage; Status = status; Process = process; } }
        public sealed class SubChapterEntry { public ushort SubChapter { get; } public byte Status { get; } public IReadOnlyList<StageEntry> Stages { get; } public SubChapterEntry(ushort subChapter, byte status, List<StageEntry> stages) { SubChapter = subChapter; Status = status; Stages = (stages ?? new List<StageEntry>()).AsReadOnly(); } }
        public sealed class ChapterEntry { public ushort Chapter { get; } public byte Status { get; } public byte IsWear { get; } public IReadOnlyList<SubChapterEntry> Subs { get; } public ChapterEntry(ushort chapter, byte status, byte isWear, List<SubChapterEntry> subs) { Chapter = chapter; Status = status; IsWear = isWear; Subs = (subs ?? new List<SubChapterEntry>()).AsReadOnly(); } }
        private readonly List<ChapterEntry> _chapters = new List<ChapterEntry>();
        private readonly HashSet<ushort> _loadedChapterDetails = new HashSet<ushort>();
        public byte TaskComplete { get; private set; }
        public IReadOnlyList<ChapterEntry> Chapters => _chapters.AsReadOnly();
        public bool IsTaskComplete => TaskComplete != 0;
        public bool HasInfo { get; private set; }
        public bool HasChapterStatusDelta { get; private set; }
        public ushort LastChapterStatusChapter { get; private set; }
        public byte LastChapterStatus { get; private set; }
        public bool HasSubStatusDelta { get; private set; }
        public ushort LastSubStatusChapter { get; private set; }
        public ushort LastSubStatusSubChapter { get; private set; }
        public byte LastSubStatus { get; private set; }
        public bool HasStageProgressDelta { get; private set; }
        public ushort LastStageProgressChapter { get; private set; }
        public ushort LastStageProgressSubChapter { get; private set; }
        public ushort LastStageProgressStage { get; private set; }
        public ulong LastStageProgress { get; private set; }
        public byte LastStageProgressStatus { get; private set; }

        public bool HasChapterDetail(ushort chapter) => _loadedChapterDetails.Contains(chapter);

        public void ReplaceInfo(byte taskComplete, List<ChapterEntry> chapters)
        {
            TaskComplete = taskComplete;
            _chapters.Clear();
            _loadedChapterDetails.Clear();
            if (chapters != null)
            {
                _chapters.AddRange(chapters);
                foreach (ChapterEntry chapter in chapters) _loadedChapterDetails.Add(chapter.Chapter);
            }
            HasInfo = true;
        }

        public void ReplaceChapterDetail(ushort chapter, byte status, List<SubChapterEntry> subs)
        {
            int index = FindChapterIndex(chapter);
            byte isWear = index >= 0 ? _chapters[index].IsWear : (byte)0;
            var replacement = new ChapterEntry(chapter, status, isWear, subs);
            if (index >= 0) _chapters[index] = replacement;
            else _chapters.Add(replacement);
            _loadedChapterDetails.Add(chapter);
        }

        public void ApplyChapterStatus(ushort chapter, byte status)
        {
            HasChapterStatusDelta = true;
            LastChapterStatusChapter = chapter;
            LastChapterStatus = status;
            int index = FindChapterIndex(chapter);
            if (index < 0)
            {
                _chapters.Add(new ChapterEntry(chapter, status, 0, null));
                return;
            }
            ChapterEntry old = _chapters[index];
            _chapters[index] = new ChapterEntry(chapter, status, old.IsWear,
                new List<SubChapterEntry>(old.Subs));
        }

        public void ApplySubStatus(ushort chapter, ushort subChapter, byte status)
        {
            HasSubStatusDelta = true;
            LastSubStatusChapter = chapter;
            LastSubStatusSubChapter = subChapter;
            LastSubStatus = status;
            int chapterIndex = EnsureChapter(chapter);
            ChapterEntry oldChapter = _chapters[chapterIndex];
            var subs = new List<SubChapterEntry>(oldChapter.Subs);
            int subIndex = FindSubIndex(subs, subChapter);
            if (subIndex < 0) subs.Add(new SubChapterEntry(subChapter, status, null));
            else
            {
                SubChapterEntry oldSub = subs[subIndex];
                subs[subIndex] = new SubChapterEntry(subChapter, status, new List<StageEntry>(oldSub.Stages));
            }
            _chapters[chapterIndex] = new ChapterEntry(chapter, oldChapter.Status, oldChapter.IsWear, subs);
        }

        public void ApplyStageProgress(ushort chapter, ushort subChapter, ushort stage, ulong process, byte status)
        {
            HasStageProgressDelta = true;
            LastStageProgressChapter = chapter;
            LastStageProgressSubChapter = subChapter;
            LastStageProgressStage = stage;
            LastStageProgress = process;
            LastStageProgressStatus = status;
            int chapterIndex = EnsureChapter(chapter);
            ChapterEntry oldChapter = _chapters[chapterIndex];
            var subs = new List<SubChapterEntry>(oldChapter.Subs);
            int subIndex = FindSubIndex(subs, subChapter);
            SubChapterEntry oldSub;
            if (subIndex < 0)
            {
                oldSub = new SubChapterEntry(subChapter, 0, null);
                subs.Add(oldSub);
                subIndex = subs.Count - 1;
            }
            else oldSub = subs[subIndex];
            var stages = new List<StageEntry>(oldSub.Stages);
            int stageIndex = -1;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].Stage != stage) continue;
                stageIndex = i;
                break;
            }
            var replacement = new StageEntry(stage, status, process);
            if (stageIndex < 0) stages.Add(replacement);
            else stages[stageIndex] = replacement;
            subs[subIndex] = new SubChapterEntry(subChapter, oldSub.Status, stages);
            _chapters[chapterIndex] = new ChapterEntry(chapter, oldChapter.Status, oldChapter.IsWear, subs);
        }

        private int EnsureChapter(ushort chapter)
        {
            int index = FindChapterIndex(chapter);
            if (index >= 0) return index;
            _chapters.Add(new ChapterEntry(chapter, 0, 0, null));
            return _chapters.Count - 1;
        }

        private int FindChapterIndex(ushort chapter)
        {
            for (int i = 0; i < _chapters.Count; i++)
                if (_chapters[i].Chapter == chapter) return i;
            return -1;
        }

        private static int FindSubIndex(List<SubChapterEntry> subs, ushort subChapter)
        {
            for (int i = 0; i < subs.Count; i++)
                if (subs[i].SubChapter == subChapter) return i;
            return -1;
        }

        public void SetPreTaskFinished(bool finished)
        {
            PreTaskFinished = finished;
        }

        public void SetOpened(bool opened)
        {
            Opened = opened;
        }

        public void Clear()
        {
            PreTaskFinished = false;
            Opened = false;
            TaskComplete = 0;
            _chapters.Clear();
            _loadedChapterDetails.Clear();
            HasInfo = false;
            HasChapterStatusDelta = false;
            LastChapterStatusChapter = 0;
            LastChapterStatus = 0;
            HasSubStatusDelta = false;
            LastSubStatusChapter = 0;
            LastSubStatusSubChapter = 0;
            LastSubStatus = 0;
            HasStageProgressDelta = false;
            LastStageProgressChapter = 0;
            LastStageProgressSubChapter = 0;
            LastStageProgressStage = 0;
            LastStageProgress = 0;
            LastStageProgressStatus = 0;
        }
    }

    /// <summary>
    /// 天命觉醒 HUD 配置读取器。章节、阶段和 KV 都来自老端同名真实配表；
    /// 显隐/进度只消费 42901 权威快照与这些配置，不在界面层写死等级、章节或职业道具。
    /// </summary>
    public static class TempleAwakenConfigs
    {
        private static JObject _kv;
        private static JObject _chapters;
        private static JObject _stages;
        private static Task _loadingTask;

        public sealed class HudInfo
        {
            public bool Visible;
            public bool Active;
            public bool Complete;
            public bool CanReceive;
            public int OpenLevel;
            public int ChapterId;
            public int GoodsId;
            public int CurrentStage;
            public int TotalStage;
            public string StageName;
        }

        private sealed class ChapterCfg
        {
            public int Id;
            public int OpenLevel;
            public int SubCount;
            public int StageCount;
            public string GoodsByCareer;
        }

        private sealed class StageCfg
        {
            public int Chapter;
            public int SubChapter;
            public int Stage;
            public int OpenLevel;
            public string Name;
        }

        public static bool IsLoaded => _kv != null && _chapters != null && _stages != null;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loadingTask == null) _loadingTask = LoadAll();
            return _loadingTask;
        }

        private static async Task LoadAll()
        {
            try
            {
                _kv = await LoadServer("config_temple_awaken_kv");
                _chapters = await LoadServer("config_temple_awaken");
                _stages = await LoadServer("config_temple_awaken_stage");
                GameLog.Info("TempleAwaken", "configs kv={0} chapters={1} stages={2}",
                    _kv.Count, _chapters.Count, _stages.Count);
            }
            finally
            {
                _loadingTask = null;
            }
        }

        private static async Task<JObject> LoadServer(string configName)
        {
            string key = GameResPath.GetServerConfigPath(configName);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("TempleAwaken", "missing {0}: {1}(未同步?跑 神霄/配表/同步客户端配置)", configName, key);
                return new JObject();
            }
            JObject result = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return result;
        }

        /// <summary>KV(2) 前置任务对原始字符串,如 "{100580, 100590}"(Erlang 字面量,不解析)。</summary>
        public static string GetPreTaskPairRaw()
        {
            return GetValueRaw(2);
        }

        /// <summary>KV(6) 等级门槛原始字符串,如 "[{lv, 48}]"(Erlang 字面量,不解析——服务端二次校验实际门槛)。</summary>
        public static string GetLevelGateRaw()
        {
            return GetValueRaw(6);
        }

        /// <summary>
        /// 对标老端 TempleAwakenModel.GetNowTask：低于全局开启等级时不显示；达到门槛后，
        /// 根据 42901 当前章节/子章/阶段状态显示进行中、可领奖、下一阶段解锁或全部完成。
        /// </summary>
        public static HudInfo BuildHudInfo(TempleAwakenModel model, int roleLevel, int career)
        {
            var hidden = new HudInfo();
            if (!IsLoaded || model == null || !model.HasInfo || career <= 0) return hidden;

            List<ChapterCfg> chapterCfgs = GetChapterConfigs();
            if (chapterCfgs.Count == 0) return hidden;
            int globalOpenLevel = ReadConditionValue(GetLevelGateRaw(), "lv");
            if (globalOpenLevel <= 0 || roleLevel < globalOpenLevel) return hidden;

            ChapterCfg currentCfg = chapterCfgs[0];
            TempleAwakenModel.ChapterEntry currentChapter = FindChapter(model, currentCfg.Id);
            var serverChapters = new List<TempleAwakenModel.ChapterEntry>(model.Chapters);
            serverChapters.Sort((a, b) => a.Chapter.CompareTo(b.Chapter));
            foreach (TempleAwakenModel.ChapterEntry chapter in serverChapters)
            {
                ChapterCfg cfg = FindChapterCfg(chapterCfgs, chapter.Chapter);
                if (cfg == null || cfg.OpenLevel > roleLevel) continue;
                currentCfg = cfg;
                currentChapter = chapter;
                if (chapter.Status == 1) break;
            }

            List<StageCfg> currentStageCfgs = GetStageConfigs(currentCfg.Id);
            if (currentStageCfgs.Count == 0) return hidden;
            int currentSubId = currentStageCfgs[0].SubChapter;
            TempleAwakenModel.SubChapterEntry currentSub = FindSub(currentChapter, currentSubId);
            if (currentChapter != null)
            {
                foreach (TempleAwakenModel.SubChapterEntry sub in currentChapter.Subs)
                {
                    StageCfg first = FindStageCfg(currentCfg.Id, sub.SubChapter, 1);
                    if (first == null || first.OpenLevel > roleLevel) continue;
                    currentSubId = sub.SubChapter;
                    currentSub = sub;
                    if (sub.Status == 1) break;
                }
            }

            bool chapterComplete = currentChapter != null && currentChapter.Status == 3;
            bool subComplete = IsSubComplete(currentCfg, currentSubId, currentSub);
            bool allComplete = currentCfg.Id == chapterCfgs[chapterCfgs.Count - 1].Id && chapterComplete;
            if (allComplete)
            {
                return new HudInfo { Visible = true, Complete = true };
            }

            if (!chapterComplete && !subComplete && currentSub != null)
            {
                HudInfo active = BuildActiveInfo(currentCfg, currentSubId, currentSub, career);
                if (active != null) return active;
            }

            int nextChapterId = currentCfg.Id;
            int nextSubId = currentSubId;
            if (chapterComplete)
            {
                int index = chapterCfgs.IndexOf(currentCfg);
                if (index + 1 >= chapterCfgs.Count) return hidden;
                nextChapterId = chapterCfgs[index + 1].Id;
                List<StageCfg> nextStages = GetStageConfigs(nextChapterId);
                if (nextStages.Count == 0) return hidden;
                nextSubId = nextStages[0].SubChapter;
            }
            else if (subComplete)
            {
                StageCfg nextSub = currentStageCfgs.Find(v => v.SubChapter > currentSubId);
                if (nextSub == null) return hidden;
                nextSubId = nextSub.SubChapter;
            }

            StageCfg nextStage = FindStageCfg(nextChapterId, nextSubId, 1);
            if (nextStage == null || nextStage.OpenLevel <= 0) return hidden;
            return new HudInfo
            {
                Visible = true,
                OpenLevel = nextStage.OpenLevel,
                ChapterId = nextChapterId
            };
        }

        private static HudInfo BuildActiveInfo(ChapterCfg cfg, int subId,
            TempleAwakenModel.SubChapterEntry sub, int career)
        {
            var ordered = new List<TempleAwakenModel.StageEntry>(sub.Stages);
            ordered.Sort((a, b) => a.Stage.CompareTo(b.Stage));
            int doingCount = 0;
            bool found = false;
            bool canReceive = false;
            string stageName = null;
            foreach (TempleAwakenModel.StageEntry stage in ordered)
            {
                if (stage.Status == 1)
                {
                    doingCount++;
                    if (!found && !canReceive)
                    {
                        StageCfg row = FindStageCfg(cfg.Id, subId, stage.Stage);
                        stageName = row?.Name;
                        found = !string.IsNullOrEmpty(stageName);
                    }
                }
                else if (stage.Status == 2)
                {
                    doingCount++;
                    found = true;
                    canReceive = true;
                    stageName = "可领取奖励";
                }
            }
            if (!found) return null;

            int goodsId = ReadCareerGoods(cfg.GoodsByCareer, career);
            if (goodsId <= 0) return null;
            return new HudInfo
            {
                Visible = true,
                Active = true,
                CanReceive = canReceive,
                ChapterId = cfg.Id,
                GoodsId = goodsId,
                CurrentStage = cfg.StageCount * (subId - 1) + Math.Max(0, cfg.StageCount - doingCount),
                TotalStage = cfg.SubCount * cfg.StageCount,
                StageName = stageName
            };
        }

        private static bool IsSubComplete(ChapterCfg cfg, int subId, TempleAwakenModel.SubChapterEntry sub)
        {
            if (sub == null || cfg.StageCount <= 0) return false;
            for (int stageId = 1; stageId <= cfg.StageCount; stageId++)
            {
                if (FindStageCfg(cfg.Id, subId, stageId) == null) return false;
                TempleAwakenModel.StageEntry entry = FindStage(sub, stageId);
                if (entry == null || entry.Status != 3) return false;
            }
            return true;
        }

        private static List<ChapterCfg> GetChapterConfigs()
        {
            var result = new List<ChapterCfg>();
            if (_chapters == null) return result;
            foreach (JProperty property in _chapters.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int id = row.Value<int?>("chapter_id") ?? 0;
                if (id <= 0) continue;
                result.Add(new ChapterCfg
                {
                    Id = id,
                    OpenLevel = ReadConditionValue(row.Value<string>("condition"), "lv"),
                    GoodsByCareer = row.Value<string>("show_goods_id"),
                    SubCount = row.Value<int?>("sub_chapter_num") ?? 0,
                    StageCount = row.Value<int?>("sub_chapter_stage_num") ?? 0
                });
            }
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result;
        }

        private static List<StageCfg> GetStageConfigs(int chapterId)
        {
            var result = new List<StageCfg>();
            if (_stages == null) return result;
            foreach (JProperty property in _stages.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int chapter = row.Value<int?>("chapter_id") ?? 0;
                if (chapter != chapterId) continue;
                result.Add(new StageCfg
                {
                    Chapter = chapter,
                    SubChapter = row.Value<int?>("sub_chapter") ?? 0,
                    Stage = row.Value<int?>("stage") ?? 0,
                    OpenLevel = ReadConditionValue(row.Value<string>("open_con"), "lv"),
                    Name = row.Value<string>("stage_name")
                });
            }
            result.Sort((a, b) =>
            {
                int sub = a.SubChapter.CompareTo(b.SubChapter);
                return sub != 0 ? sub : a.Stage.CompareTo(b.Stage);
            });
            return result;
        }

        private static StageCfg FindStageCfg(int chapterId, int subId, int stageId)
        {
            if (_stages?[$"{chapterId}@{subId}@{stageId}"] is JObject row)
            {
                return new StageCfg
                {
                    Chapter = chapterId,
                    SubChapter = subId,
                    Stage = stageId,
                    OpenLevel = ReadConditionValue(row.Value<string>("open_con"), "lv"),
                    Name = row.Value<string>("stage_name")
                };
            }
            return null;
        }

        private static ChapterCfg FindChapterCfg(List<ChapterCfg> configs, int id)
        {
            return configs.Find(v => v.Id == id);
        }

        private static TempleAwakenModel.ChapterEntry FindChapter(TempleAwakenModel model, int id)
        {
            foreach (TempleAwakenModel.ChapterEntry chapter in model.Chapters)
                if (chapter.Chapter == id) return chapter;
            return null;
        }

        private static TempleAwakenModel.SubChapterEntry FindSub(
            TempleAwakenModel.ChapterEntry chapter, int id)
        {
            if (chapter == null) return null;
            foreach (TempleAwakenModel.SubChapterEntry sub in chapter.Subs)
                if (sub.SubChapter == id) return sub;
            return null;
        }

        private static TempleAwakenModel.StageEntry FindStage(
            TempleAwakenModel.SubChapterEntry sub, int id)
        {
            if (sub == null) return null;
            foreach (TempleAwakenModel.StageEntry stage in sub.Stages)
                if (stage.Stage == id) return stage;
            return null;
        }

        private static int ReadCareerGoods(string raw, int career)
        {
            ErlangTerm root = Parse(raw);
            if (root?.Items == null) return 0;
            foreach (ErlangTerm pair in root.Items)
            {
                if (pair?.Items == null || pair.Items.Count < 2 || pair.Get<int>(0) != career) continue;
                return pair.Get<int>(1);
            }
            return 0;
        }

        private static int ReadConditionValue(string raw, string key)
        {
            ErlangTerm root = Parse(raw);
            if (root?.Items == null) return 0;
            foreach (ErlangTerm pair in root.Items)
            {
                if (pair?.Items == null || pair.Items.Count < 2 || pair.Get<string>(0) != key) continue;
                return pair.Get<int>(1);
            }
            return 0;
        }

        private static ErlangTerm Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                return ErlangParser.Parse(raw);
            }
            catch (Exception ex)
            {
                GameLog.Warn("TempleAwaken", "parse config term failed raw={0}: {1}", raw, ex.Message);
                return null;
            }
        }

        private static string GetValueRaw(int kvKey)
        {
            if (_kv?[kvKey.ToString()] is JObject obj)
            {
                string value = obj.Value<string>("value");
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }
    }
}
