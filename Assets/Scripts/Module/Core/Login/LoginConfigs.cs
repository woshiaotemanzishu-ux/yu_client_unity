using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录链路的客户端配置访问(老客户端 preload_client_config 的对等物):
    /// ConfigLogin(创角/选角)、ConfigModelAni(UI 模型动作)、ConfigRandomName(随机名)。
    /// JSON 由编辑器菜单「神霄/配表/同步客户端配置(JSON)」从 yu_client 同步进 GameRes。
    /// 结构不规则,用 JObject 直读;等配表线强类型方案覆盖到客户端表再迁移。
    /// </summary>
    public static class LoginConfigs
    {
        public sealed class CareerOption
        {
            public int Career;
            public int Sex;
            public bool Open;
            public int RandomWeight;
            public string Name;
            public string SelectIcon;
            public string UnselectIcon;
            public string Img1;
            public string Img2;
            public string Img3;
        }

        public sealed class CareerRes
        {
            public int RoleRes;
            public int WeaponRes;
            public int HeadRes;
        }

        private static JObject _login;
        private static JObject _modelAni;
        private static JObject _randomName;
        private static JObject _dress;

        public static bool IsLoaded => _login != null && _modelAni != null && _randomName != null && _dress != null;

        public static async Task EnsureLoaded()
        {
            // 失败不缓存(保持 null),下次进页面还会重试;访问器全部 null 安全
            if (_login == null) _login = await LoadJson("resource/config/client/configlogin");
            if (_modelAni == null) _modelAni = await LoadJson("resource/config/client/configmodelani");
            if (_randomName == null) _randomName = await LoadJson("resource/config/client/configrandomname");
            if (_dress == null) _dress = await LoadJson("resource/config/server/config_dress_up_cfg");
        }

        private static async Task<JObject> LoadJson(string key)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Config", "客户端配置缺失:{0}(进 Play 前会自动同步;手动菜单 神霄/配表/同步客户端配置)", key);
                return null;
            }
            var jo = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return jo;
        }

        // ---------------- ConfigLogin.CreateRole ----------------

        /// <summary>创角页职业项(过滤 open_state=false)。</summary>
        public static List<CareerOption> CreateRoleOptions()
        {
            var list = new List<CareerOption>();
            if (!(_login?["CreateRole"]?["UI"] is JArray arr)) return list;
            foreach (JToken t in arr)
            {
                if (!(t is JObject o) || !o.Value<bool>("open_state")) continue;
                list.Add(new CareerOption
                {
                    Career = o.Value<int>("career"),
                    Sex = o.Value<int>("sex"),
                    Open = true,
                    RandomWeight = o.Value<int>("random_weight"),
                    Name = o.Value<string>("name"),
                    SelectIcon = o.Value<string>("select_icon"),
                    UnselectIcon = o.Value<string>("unselect_icon"),
                    Img1 = o.Value<string>("img1"),
                    Img2 = o.Value<string>("img2"),
                    Img3 = o.Value<string>("img3"),
                });
            }
            return list;
        }

        /// <summary>默认装(CreateRole.Res["career@sex"]):role_res/weapon_res/head_res。</summary>
        public static CareerRes GetCreateRes(int career, int sex)
        {
            JToken r = _login?["CreateRole"]?["Res"]?[$"{career}@{sex}"];
            if (r == null) return null;
            return new CareerRes
            {
                RoleRes = r.Value<int>("role_res"),
                WeaponRes = r.Value<int>("weapon_res"),
                HeadRes = r.Value<int>("head_res"),
            };
        }

        /// <summary>模型展示位移:ModelPos + PosOffset["career@sex"](x 右正,y 上正)。</summary>
        public static Vector2 GetModelPos(string section, int career, int sex)
        {
            JToken s = _login?[section];
            float x = s?["ModelPos"]?.Value<float>("x") ?? 0f;
            float y = s?["ModelPos"]?.Value<float>("y") ?? 0f;
            JToken off = s?["PosOffset"]?[$"{career}@{sex}"];
            if (off != null)
            {
                x += off.Value<float>("x");
                y += off.Value<float>("y");
            }
            return new Vector2(x, y);
        }

        /// <summary>选角页固定槽位数(SelectRole.TotalCount,不足补「创建角色」空槽)。</summary>
        public static int SelectRoleTotalCount()
        {
            return _login?["SelectRole"]?.Value<int>("TotalCount") ?? 4;
        }

        // ---------------- ConfigModelAni ----------------

        /// <summary>UI 角色模型动作清单:role[layout].default → role.UI.default → ["idle"]。</summary>
        public static string[] RoleUIActions(string layoutFile)
        {
            JToken role = _modelAni?["role"];
            JToken list = role?[layoutFile]?["default"] ?? role?["UI"]?["default"];
            if (list is JArray arr && arr.Count > 0)
            {
                var result = new string[arr.Count];
                for (int i = 0; i < arr.Count; i++) result[i] = arr[i].Value<string>();
                return result;
            }
            return new[] { "idle" };
        }

        // ---------------- config_dress_up_cfg(头像)----------------

        // DressModel.ts:DressType.Head=5,getDressIdByTurn 默认 90,条件 [["turn",n]] 匹配转生数;
        // screen 字段 [{"0":career,"1":icon}],图标在 head/texture/{icon}.png
        private const int DRESS_TYPE_HEAD = 5;
        private const int DEFAULT_HEAD_DRESS_ID = 90;

        /// <summary>角色默认头像图标路径(对标 CustomHeadItem.SetDefaultHead;自定义头像 picture 待头像线)。</summary>
        public static string HeadIconPath(int career, int turn)
        {
            int dressId = DEFAULT_HEAD_DRESS_ID;
            if (_dress != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _dress)
                {
                    if (!(kv.Value is JObject o)) continue;
                    if (o.Value<int>("type") != DRESS_TYPE_HEAD || o.Value<int>("level") != 1) continue;
                    string cond = o.Value<string>("condition");
                    if (string.IsNullOrEmpty(cond) || cond == "[]") continue;
                    try
                    {
                        var arr = JArray.Parse(cond);
                        if (arr.Count > 0 && arr[0] is JArray c && c.Count >= 2
                            && c[0].Value<string>() == "turn" && c[1].Value<int>() == turn)
                        {
                            dressId = o.Value<int>("id");
                            break;
                        }
                    }
                    catch { /* 条件格式异类,跳过 */ }
                }
            }
            JToken entry = _dress?[$"{DRESS_TYPE_HEAD}@{dressId}@1"];
            string screen = entry?.Value<string>("screen");
            if (string.IsNullOrEmpty(screen) || screen == "[]") return null;
            try
            {
                foreach (JToken t in JArray.Parse(screen))
                {
                    if (t.Value<int>("0") == career)
                        return $"resource/game/head/texture/{t.Value<int>("1")}.png";
                }
            }
            catch { }
            return null;
        }

        // ---------------- ConfigRandomName ----------------

        /// <summary>随机名 = 随机姓 + 按性别随机名(对标老客户端 RandomName)。</summary>
        public static string RandomRoleName(int sex)
        {
            string surname = PickRandom(_randomName?["surname"] as JArray);
            string forename = PickRandom(
                (sex == 2 ? _randomName?["forename_woman"] : _randomName?["forename_man"]) as JArray);
            if (surname == null || forename == null) return "无名" + Random.Range(100, 999);
            return surname + forename;
        }

        private static string PickRandom(JArray arr)
        {
            if (arr == null || arr.Count == 0) return null;
            return arr[Random.Range(0, arr.Count)].Value<string>();
        }
    }
}
