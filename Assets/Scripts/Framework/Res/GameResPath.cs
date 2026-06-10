namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Factory for Addressable keys. All resource path strings used in business code
    /// MUST come from this class. Do not concatenate paths inline.
    ///
    /// Mirrors the LayaAir client's GameResPath (yu_client/h5/src/util/GameResPath.ts)
    /// 1:1 so that ts-to-cs translations of view code Just Work without rewriting paths.
    /// Trailing extensions are kept consistent with the ts version: .png / .jpg / .lh / .json.
    /// ResourcePath.Normalize() is used at load time to strip extensions for Addressables.
    /// </summary>
    public static class GameResPath
    {
        // ---------- Configs ----------

        public static string GetClientConfigPath(string cfg)
            => "resource/config/client/" + cfg + ".json";

        public static string GetServerConfigPath(string cfg)
            => "resource/config/server/" + cfg + ".json";

        // ---------- Scenes / 3D ----------

        public static string GetScenePath() => "resource/game/scene/";
        public static string GetSceneObjPath() => "resource/game/scene/sceneobj/";
        public static string GetSceneWeaponPath() => "resource/game/scene/weapon/";
        public static string GetSceneWingPath() => "resource/game/scene/wing/";
        public static string GetSceneHorsePath() => "resource/game/scene/horse/";

        public static string GetObjectPath(string mName, string resName)
            => "resource/object/" + mName + "/objs/" + resName + ".lh";

        // ---------- Effects ----------

        public static string GetEffectPath(string effectType, string resName)
            => "resource/effect/objs/" + effectType + "/" + resName + ".lh";

        public static string GetUIEffectPath(string resName)
            => "resource/effect/objs/ui_effect/" + resName + ".lh";

        // ---------- Fonts ----------

        public static string GetFontPath(string font)
            => "resource/font/" + font + ".fnt";

        public static string GetFont(string resName)
            => "resource/font/" + resName + ".fnt";

        // ---------- UI / icons ----------

        public static string GetIcon(string module, string resName)
            => "resource/game/" + module + "/texture/" + resName + ".png";

        public static string GetIconJpg(string module, string resName)
            => "resource/game/" + module + "/texture/" + resName + ".jpg";

        public static string GetIconOtherPath(string module, string resName)
            => "resource/game/" + module + "/other/" + resName + ".png";

        public static string GetIconJpgOtherPath(string module, string resName)
            => "resource/game/" + module + "/other/" + resName + ".jpg";

        public static string GetGoodsIconPath(string name)
            => "resource/game/goodsIcon/" + name + ".png";

        public static string GetSkillIcon(string resName)
            => "resource/game/skillIcon/" + resName + ".png";

        public static string GetSkillIconPath(string skillId)
            => "resource/game/skillIcon/" + skillId + ".png";

        public static string GetHeadPath(string res)
            => "resource/game/head/texture/" + res + ".png";

        public static string GetMonsterHeadPath(string res)
            => "resource/game/monsterHead/" + res + ".png";

        public static string GetMonsterBigHeadPath(string res)
            => "resource/game/monsterBigHead/" + res + ".png";

        public static string GetDailyIconPath(string res)
            => "resource/game/dailyIcon/texture/" + res + ".png";

        public static string GetFuncOpenIconPath(string res)
            => "resource/game/functionOpen/texture/" + res + ".png";

        public static string GetMonBookImgPath(string res)
            => "resource/game/monBook/other/" + res + ".png";

        public static string GetBigBgPath(string res)
            => "resource/game/bigBg/" + res;

        public static string GetAreaMapImg(int sceneId)
            => "resource/game/map/area_map_img/" + sceneId + ".jpg";

        public static string GetDesignImage(string resName)
            => "resource/game/dsgtIcon/" + resName + ".png";

        public static string GetPetPath(string resName)
            => "resource/game/pet/texture/" + resName + ".png";

        public static string GetTsPath(string module, string resName)
            => "resource/game/tsCrack/" + module + "/" + resName + ".png";

        public static string GetFilePath(string fileName, string resName)
            => "resource/game/" + fileName + "/" + resName + ".png";

        public static string GetVideoPath(string fileName)
            => "resource/video/" + fileName + ".mp4";

        public static string GetSkillNamePath(int skillId)
            => "resource/game/skillName/" + skillId + ".png";

        public static string GetFashionPath(string resName)
            => "resource/object/fashion/" + resName + ".jpg";

        // ---------- Sounds ----------

        public static string GetSoundPath(string type, string name)
            => "resource/sound/" + type + "/" + name;

        // ---------- UI prefabs (Unity-only, not in ts) ----------

        public static string GetUIPrefab(string module, string viewName)
            => "prefabs/ui/" + module.ToLowerInvariant() + "/" + viewName.ToLowerInvariant();
    }
}
