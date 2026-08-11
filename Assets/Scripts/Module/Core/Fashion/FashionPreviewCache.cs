using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// FashionMain 专属模型预组装缓存。41300 快照落地后提前组装默认衣服、染色、头饰、武器和
    /// always 特效；View 打开时只接管完整实例并交给共享 UIModelStage，不让共享台承担页面缓存职责。
    /// </summary>
    internal static class FashionPreviewCache
    {
        internal sealed class Request
        {
            internal string Key;
            internal int ColorId;
            internal string RequestedTextureName;
            internal RoleModelSpec Spec;
        }

        private const int DefaultPosId = 1;
        private static int _generation;
        private static int _defaultRequestId;
        private static string _key = "";
        private static Task<GameObject> _buildTask;
        private static GameObject _readyModel;

        internal static void Reset()
        {
            ++_defaultRequestId;
            InvalidateCache();
        }

        /// <summary>
        /// 权威外观/时装快照变化时同步作废未被 View 接管的备用实例，再按最新默认选择预组装。
        /// 已经交给 UIModelStage 的页面实例不归本缓存持有，不会被这里误销毁。
        /// </summary>
        internal static void RefreshDefault()
        {
            ++_defaultRequestId;
            InvalidateCache();
            PrewarmDefault();
        }

        /// <summary>41300 与 FashionMain.OnHide 调用；重复 key 不会重复组装。</summary>
        internal static void PrewarmDefault()
        {
            int requestId = ++_defaultRequestId;
            _ = PrewarmDefaultAsync(requestId);
        }

        private static async Task PrewarmDefaultAsync(int requestId)
        {
            try
            {
                await FashionConfigs.EnsureLoaded();
                if (requestId != _defaultRequestId) return;

                int fashionId = ResolveDefaultFashionId(DefaultPosId);
                if (fashionId <= 0) return;
                int colorId = ResolveDefaultColor(DefaultPosId, fashionId);
                Request request = await CreateRequestAsync(DefaultPosId, fashionId, colorId);
                if (requestId != _defaultRequestId || request == null) return;
                StartPrewarm(request);
            }
            catch (System.Exception exception)
            {
                if (requestId == _defaultRequestId)
                    GameLog.Warn("Fashion", "FashionMain 默认模型预组装启动失败: {0}", exception.Message);
            }
        }

        internal static async Task<Request> CreateRequestAsync(int posId, int fashionId, int colorId)
        {
            if (posId <= 0 || fashionId <= 0) return null;
            await FashionConfigs.EnsureLoaded();
            await LoginConfigs.EnsureLoaded();

            RoleModel role = RoleModel.Instance;
            FigureProto figure = role.Figure;
            int career = figure != null && figure.career > 0 ? figure.career : Mathf.Max(1, role.Career);
            int sex = figure != null && figure.sex > 0 ? figure.sex : ((career == 2 || career == 4) ? 2 : 1);
            LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
            FashionConfigs.ModelRow selected = FashionConfigs.GetModelRow(posId, fashionId, career, sex, colorId);
            if (selected == null || selected.ModelId <= 0) return null;

            int currentClothe = figure != null && figure.ClotheModelId > 0
                ? figure.ClotheModelId : (defaults != null ? defaults.RoleRes : 0);
            int currentHead = figure != null && figure.HeadModelId > 0
                ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0);
            int weapon = figure != null && figure.WeaponModelId > 0
                ? figure.WeaponModelId : (defaults != null ? defaults.WeaponRes : 0);
            int clothe = posId == 1 ? selected.ModelId : currentClothe;
            int head = posId == 3 ? selected.ModelId : currentHead;
            int clotheChartlet = posId == 1 ? colorId : (figure?.ClotheChartletId ?? 0);
            int headChartlet = posId == 3 ? colorId : (figure?.HeadChartletId ?? 0);
            if (clothe <= 0) return null;

            return new Request
            {
                Key = string.Join("|", posId, fashionId, colorId, career, sex,
                    clothe, clotheChartlet, head, headChartlet, weapon),
                ColorId = colorId,
                RequestedTextureName = colorId > 0
                    ? (posId == 1 ? "model_clothe_" + clothe : "model_head_" + head) + "_" + colorId
                    : "",
                Spec = new RoleModelSpec
                {
                    Career = career,
                    ClotheRes = clothe,
                    ClotheChartletId = clotheChartlet,
                    HeadRes = head,
                    HeadChartletId = headChartlet,
                    WeaponRes = weapon,
                    Actions = LoginConfigs.RoleUIActions("FashionMainView"),
                },
            };
        }

        internal static async Task<GameObject> TakeOrBuildAsync(Request request)
        {
            if (request == null) return null;
            if (_key != request.Key || (_readyModel == null && _buildTask == null))
                return await RoleModelAssembler.BuildOldModelAsync(request.Spec);

            int generation = _generation;
            Task<GameObject> task = _buildTask;
            GameObject model = _readyModel != null ? _readyModel : await task;
            if (generation != _generation || _key != request.Key || model == null || model != _readyModel)
                return null;

            _readyModel = null;
            _buildTask = null;
            _key = "";
            return model;
        }

        private static void StartPrewarm(Request request)
        {
            if (request == null) return;
            if (_key == request.Key && (_readyModel != null || _buildTask != null)) return;

            InvalidateCache();
            int generation = _generation;
            _key = request.Key;
            _buildTask = BuildCachedAsync(request, generation);
        }

        private static async Task<GameObject> BuildCachedAsync(Request request, int generation)
        {
            GameObject model;
            try
            {
                model = await RoleModelAssembler.BuildOldModelAsync(request.Spec);
            }
            catch (System.Exception exception)
            {
                if (generation == _generation && _key == request.Key)
                {
                    _buildTask = null;
                    _key = "";
                }
                GameLog.Warn("Fashion", "FashionMain 默认模型预组装失败 key={0}: {1}",
                    request.Key, exception.Message);
                return null;
            }
            if (model == null)
            {
                if (generation == _generation && _key == request.Key)
                {
                    _buildTask = null;
                    _key = "";
                }
                return null;
            }

            if (generation != _generation || _key != request.Key)
            {
                Object.Destroy(model);
                return null;
            }

            // 缓存实例不占用 UIModelStage，也不在主城场景继续播放；View 接管后再激活并首帧渲染。
            model.SetActive(false);
            _readyModel = model;
            GameLog.Info("Fashion", "FashionMain 默认模型预组装完成 key={0}", request.Key);
            return model;
        }

        private static void InvalidateCache()
        {
            ++_generation;
            _key = "";
            _buildTask = null;
            if (_readyModel != null) Object.Destroy(_readyModel);
            _readyModel = null;
        }

        internal static int ResolveDefaultFashionId(int posId)
        {
            IReadOnlyList<int> ids = FashionConfigs.GetFashionIds(posId);
            for (int i = 0; i < ids.Count; i++)
                if (ComputeItemRed(posId, ids[i])) return ids[i];
            return ids.Count > 0 ? ids[0] : 0;
        }

        internal static int ResolveDefaultColor(int posId, int fashionId)
        {
            if (fashionId <= 0) return 0;
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(posId, fashionId);
            if (entry == null) return 0;
            IReadOnlyList<int> colorIds = FashionConfigs.GetColorIds(posId, fashionId);
            for (int i = 0; i < colorIds.Count; i++)
            {
                int colorId = colorIds[i];
                if (!entry.IsColorUnlocked(colorId) && ComputeColorRed(posId, fashionId, colorId, entry))
                    return colorId;
            }
            if (ComputeBaseRed(posId, fashionId)) return 0;
            for (int i = 0; i < colorIds.Count; i++)
            {
                int colorId = colorIds[i];
                if (entry.IsColorUnlocked(colorId) && ComputeColorRed(posId, fashionId, colorId, entry))
                    return colorId;
            }
            return 0;
        }

        internal static bool ComputeItemRed(int posId, int fashionId)
        {
            if (ComputeBaseRed(posId, fashionId)) return true;
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(posId, fashionId);
            IReadOnlyList<int> colorIds = FashionConfigs.GetColorIds(posId, fashionId);
            for (int i = 0; i < colorIds.Count; i++)
                if (ComputeColorRed(posId, fashionId, colorIds[i], entry)) return true;
            return false;
        }

        internal static bool ComputeBaseRed(int posId, int fashionId)
        {
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(posId, fashionId);
            int order = entry?.GetStarLv(0) ?? 0;
            FashionConfigs.Row next = FashionConfigs.GetBaseRow(posId, fashionId, order + 1);
            return next.Found && HasEnoughCost(order == 0 ? next.ActiveCostJson : next.StarCostJson);
        }

        internal static bool ComputeColorRed(int posId, int fashionId, int colorId,
            FashionModel.FashionEntry entry)
        {
            int order = Mathf.Max(0, entry?.GetStarLv(colorId) ?? 0);
            FashionConfigs.Row next = FashionConfigs.GetColorRow(posId, fashionId, colorId, order + 1);
            return next.Found && HasEnoughCost(order == 0 ? next.ActiveCostJson : next.StarCostJson);
        }

        internal static bool HasEnoughCost(string json)
        {
            List<(int type, int typeId, long num)> costs = FashionConfigs.ParseCostList(json);
            if (costs.Count == 0) return false;
            for (int i = 0; i < costs.Count; i++)
                if (BagModel.Instance.GetTypeGoodsNum(costs[i].typeId) < costs[i].num) return false;
            return true;
        }
    }
}
