using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职配置访问层(对标老端 transferJob/TransferJobController.ts::TransferJobCardController,
    /// 照 <see cref="Skill.SkillConfigs"/> 模式:静态 EnsureLoaded 懒加载,缺表降级不炸)。两张表:
    ///   config_career(服务端表,"career@sex" 具名键 → {career_id,career_name,sex}):仅用其【键集合】
    ///     枚举全部合法(career,sex)组合(对标老端 Object.keys(careerCfg)),值本身不使用。
    ///   ClientTransfer(客户端表,TransferCareerMsg 数组,按 career 索引):取 name/desc1/desc2 展示文案。
    ///
    /// ⚠两表均尚未同步进 Assets/GameRes/resource/config(表在 GameRes 下自查确认缺失;已把
    /// "config_career"/"ClientTransfer" 补登记进 ClientConfigSync 的同步清单,下次跑
    /// "神霄/配表/同步客户端配置" 菜单 + 「神霄/资源/Addressable 自动分组」即可落地生效)。本类按标准
    /// Addressable 路径尝试加载,缺失时 IsLoaded=false,GetTransferTargets 返回空表(降级,不臆造数据)。
    /// </summary>
    public static class TransferJobModel
    {
        /// <summary>一个合法的(career,sex)转职目标组合(对标 config_career 具名键 "career@sex")。</summary>
        public sealed class CareerEntry
        {
            public int Career;
            public int Sex;
        }

        /// <summary>展示文案(对标 ClientTransfer.json TransferCareerMsg 数组单项)。</summary>
        public sealed class CareerMsg
        {
            public int Career;
            public string Name = "";
            public string Desc1 = "";
            public string Desc2 = "";
        }

        private static JObject _careerCfg;    // 服务端 config_career.json("career@sex" 具名键)
        private static bool _transferLoaded;
        private static readonly Dictionary<int, CareerMsg> _transferMsg = new Dictionary<int, CareerMsg>();

        public static bool IsLoaded => _careerCfg != null && _transferLoaded;

        public static async Task EnsureLoaded()
        {
            if (IsLoaded) return;

            if (_careerCfg == null)
            {
                string careerKey = GameResPath.GetServerConfigPath("config_career");
                TextAsset careerAsset = await ResManager.LoadOptionalAsync<TextAsset>(careerKey);
                if (careerAsset == null)
                {
                    GameLog.Warn("TransferJob", "缺 config_career: {0}(跑 神霄/配表/同步客户端配置 后重进游戏)", careerKey);
                    _careerCfg = new JObject();
                }
                else
                {
                    _careerCfg = JObject.Parse(careerAsset.text);
                    ResManager.Release(careerAsset);
                }
            }

            if (!_transferLoaded)
            {
                string transferKey = GameResPath.GetClientConfigPath("clienttransfer");
                TextAsset transferAsset = await ResManager.LoadOptionalAsync<TextAsset>(transferKey);
                if (transferAsset == null)
                {
                    GameLog.Warn("TransferJob", "缺 ClientTransfer: {0}(跑 神霄/配表/同步客户端配置 后重进游戏)", transferKey);
                }
                else
                {
                    JObject root = JObject.Parse(transferAsset.text);
                    if (root["TransferCareerMsg"] is JArray arr)
                    {
                        foreach (JToken tok in arr)
                        {
                            if (!(tok is JObject o)) continue;
                            int career = o.Value<int?>("career") ?? 0;
                            if (career <= 0) continue;
                            _transferMsg[career] = new CareerMsg
                            {
                                Career = career,
                                Name = o.Value<string>("name") ?? "",
                                Desc1 = o.Value<string>("desc1") ?? "",
                                Desc2 = o.Value<string>("desc2") ?? "",
                            };
                        }
                    }
                    ResManager.Release(transferAsset);
                }
                _transferLoaded = true;
            }
        }

        /// <summary>目标转职卡列表(除自身职业外,对标老端 careerCfg keys filter,按 career 升序)。
        /// 表缺失时返回空列表(降级,不臆造)。</summary>
        public static List<CareerEntry> GetTransferTargets(int excludeCareer)
        {
            var list = new List<CareerEntry>();
            if (_careerCfg == null) return list;
            foreach (JProperty prop in _careerCfg.Properties())
            {
                string[] parts = prop.Name.Split('@');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int career) || !int.TryParse(parts[1], out int sex)) continue;
                if (career == excludeCareer) continue;
                list.Add(new CareerEntry { Career = career, Sex = sex });
            }
            list.Sort((a, b) => a.Career - b.Career);
            return list;
        }

        public static CareerMsg GetCareerMsg(int career)
            => _transferMsg.TryGetValue(career, out CareerMsg m) ? m : null;
    }
}
