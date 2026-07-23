using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.FriendInvite;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class FriendInviteLevelCase
    {
        private const BindingFlags NP = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SNP = BindingFlags.Static | BindingFlags.NonPublic;
        public static Task<int> Run() { try { return Task.FromResult(RunCore()); } catch (Exception e) { Debug.LogError(e); return Task.FromResult(3); } }
        private static int RunCore()
        {
            bool pass = false;
            var c = FriendInviteController.Instance; var m = FriendInviteModel.Instance; bool was = c.IsInitialized;
            var role = RoleModel.Instance; int oldRoleLevel = role.Level;
            var roleReady = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", NP); object oldRoleReady = roleReady?.GetValue(role);
            int oldGet = m.GetStatus, oldRecover = m.RecoverTime, oldDaily = m.DailyCount, oldTotal = m.TotalCount;
            bool oldHas = m.HasLevelInfo, oldHelp = m.HasHelpInfo, oldWelfare = m.HasWelfareInfo, oldShare = FriendInviteModel.ShareOpen;
            ushort oldHelpCount = m.HelpCount;
            var oldHelpRewards = CloneRewards(m.HelpRewards); var oldHelpEntries = Clone(m.HelpInviteEntries);
            byte oldWelfareType = m.WelfareInfoType; var oldWelfareRewards = CloneRewards(m.WelfareRewards);
            var oldEntries = Clone(m.LevelInviteEntries);
            var lastLevel = typeof(FriendInviteController).GetField("_lastLevel", NP); object oldLast = lastLevel?.GetValue(c);
            var intercept = typeof(FriendInviteController).GetField("s_outboundIntercept", SNP); object oldIntercept = intercept?.GetValue(null);
            var eventHandlers = typeof(EventDispatcher).GetField("_handlers", SNP)?.GetValue(null) as IDictionary;
            bool oldRoleEvent = eventHandlers != null && eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            var oldRoleSubscribers = oldRoleEvent ? new List<Delegate>((List<Delegate>)eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE]) : new List<Delegate>();
            var iconMgr = ActivityIconManager.Instance;
            var iconField = typeof(ActivityIconManager).GetField("_iconInfoByType", NP);
            var boxField = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", NP);
            var icons = iconField?.GetValue(iconMgr) as IDictionary; var boxes = boxField?.GetValue(iconMgr) as IDictionary;
            bool oldIcon = icons != null && icons.Contains("340"), oldBox = boxes != null && boxes.Contains("340");
            object oldIconRef = oldIcon ? icons["340"] : null, oldBoxRef = oldBox ? boxes["340"] : null;
            var handlers = typeof(NetManager).GetField("_handlers", SNP)?.GetValue(null) as IDictionary;
            bool old34000 = handlers != null && handlers.Contains(34000), old34001 = handlers != null && handlers.Contains(34001), old34005 = handlers != null && handlers.Contains(34005), old34006 = handlers != null && handlers.Contains(34006), old34012 = handlers != null && handlers.Contains(34012);
            object old34000Ref = old34000 ? handlers[34000] : null, old34001Ref = old34001 ? handlers[34001] : null, old34005Ref = old34005 ? handlers[34005] : null, old34006Ref = old34006 ? handlers[34006] : null, old34012Ref = old34012 ? handlers[34012] : null;
            try
            {
                c.Init(); m.Reset(); var on = typeof(FriendInviteController).GetMethod("On34006", NP); var onHelp = typeof(FriendInviteController).GetMethod("On34005", NP); var onWelfare = typeof(FriendInviteController).GetMethod("On34012", NP);
                var onInfo = typeof(FriendInviteController).GetMethod("On34001", NP);
                pass = on != null && onHelp != null && onWelfare != null && onInfo != null && intercept != null && handlers != null
                    && lastLevel != null && roleReady != null && eventHandlers != null && icons != null && boxes != null;
                for (int p = 34000; p <= 34012; p++) pass &= handlers.Contains(p) == (p == 34000 || p == 34001 || p == 34005 || p == 34006 || p == 34012);
                var frames = new List<byte[]>(); intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                c.RequestLevelInfo(); pass &= Frames(frames, 34006); frames.Clear(); c.RequestHelpInfo(); pass &= Frames(frames, 34005); frames.Clear(); c.RequestWelfareInfo(3); pass &= Frames(frames, 34012); frames.Clear(); c.RequestStartup(); pass &= Frames(frames, 34001, 34012, 34005, 34006);
                Invoke(on, c, PacketSingle(), out int keepRemain);
                FriendInviteModel.ShareOpen = false;
                Invoke(onInfo, c, InfoPacket(), out int infoRemain);
                pass &= keepRemain == 0 && infoRemain == 0 && m.GetStatus == 9 && m.RecoverTime == 8 && m.DailyCount == 7 && m.TotalCount == 6 && m.LevelInviteEntries.Count == 1;
                Invoke(onHelp, c, HelpMultiPacket(), out int helpRemain);
                pass &= helpRemain == 0 && m.HasHelpInfo && m.HelpCount == ushort.MaxValue && m.HelpRewards.Count == 2
                    && m.HelpRewards[0].RewardId == 255 && m.HelpRewards[0].Status == 254 && m.HelpRewards[1].RewardId == 255 && m.HelpRewards[1].Status == 1
                    && m.HelpInviteEntries.Count == 2 && Entry(m.HelpInviteEntries[0], ulong.MaxValue, 255, "中文", ushort.MaxValue, 254, 253)
                    && Entry(m.HelpInviteEntries[1], ulong.MaxValue, 255, "", 0, 1, 2);
                Invoke(onWelfare, c, WelfareMultiPacket(), out int welfareRemain);
                pass &= welfareRemain == 0 && WelfareMultiState(m) && m.HasHelpInfo && m.HelpCount == ushort.MaxValue && m.HasLevelInfo && m.LevelInviteEntries.Count == 1;
                Invoke(onInfo, c, InfoPacket(), out infoRemain);
                pass &= infoRemain == 0 && m.HasHelpInfo && m.HelpCount == ushort.MaxValue && m.HelpRewards.Count == 2 && m.HelpInviteEntries.Count == 2
                    && m.HasLevelInfo && m.LevelInviteEntries.Count == 1;
                role.MarkBaseInfoReady(); role.Level = 654321; lastLevel.SetValue(c, 0); frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE); pass &= Frames(frames, 34001, 34012, 34005, 34006);
                Invoke(on, c, PacketMulti(), out int remain);
                pass &= remain == 0 && m.HasLevelInfo && m.LevelInviteEntries.Count == 2 && m.LevelInviteEntries[0].InviteeId == ulong.MaxValue
                    && m.LevelInviteEntries[0].Pos == 255 && m.LevelInviteEntries[0].Name == "邀请中文" && m.LevelInviteEntries[0].Level == ushort.MaxValue
                    && m.LevelInviteEntries[0].Career == 254 && m.LevelInviteEntries[0].Status == 253 && m.LevelInviteEntries[1].InviteeId == ulong.MaxValue
                    && m.LevelInviteEntries[1].Pos == 255 && m.LevelInviteEntries[1].Name == "" && m.LevelInviteEntries[1].Level == 0
                    && m.LevelInviteEntries[1].Career == 1 && m.LevelInviteEntries[1].Status == 2;
                pass &= HelpMultiState(m) && WelfareMultiState(m);
                m.SetInfo(9, 8, 7, 6);
                Invoke(on, c, PacketSingle(), out remain); pass &= remain == 0 && m.LevelInviteEntries.Count == 1 && m.LevelInviteEntries[0].Name == "";
                pass &= m.GetStatus == 9 && m.RecoverTime == 8 && m.DailyCount == 7 && m.TotalCount == 6;
                frames.Clear(); c.RequestLevelInfo(); pass &= Frames(frames, 34006) && m.LevelInviteEntries.Count == 1 && m.LevelInviteEntries[0].InviteeId == 1;
                Invoke(onHelp, c, HelpSinglePacket(), out helpRemain);
                pass &= helpRemain == 0 && m.HasHelpInfo && m.HelpCount == 7 && m.HelpRewards.Count == 1 && m.HelpRewards[0].RewardId == 8 && m.HelpRewards[0].Status == 9
                    && m.HelpInviteEntries.Count == 1 && Entry(m.HelpInviteEntries[0], 10, 11, "single", 12, 13, 14)
                    && m.GetStatus == 9 && m.RecoverTime == 8 && m.DailyCount == 7 && m.TotalCount == 6 && m.LevelInviteEntries.Count == 1;
                frames.Clear(); c.RequestHelpInfo();
                pass &= Frames(frames, 34005) && m.HasHelpInfo && m.HelpCount == 7 && m.HelpRewards.Count == 1 && m.HelpInviteEntries.Count == 1 && Entry(m.HelpInviteEntries[0], 10, 11, "single", 12, 13, 14);
                Invoke(onWelfare, c, WelfareSinglePacket(), out welfareRemain);
                pass &= welfareRemain == 0 && m.HasWelfareInfo && m.WelfareInfoType == 3 && m.WelfareRewards.Count == 1 && m.WelfareRewards[0].RewardId == 8 && m.WelfareRewards[0].Status == 9;
                frames.Clear(); c.RequestWelfareInfo(3); pass &= Frames(frames, 34012) && m.WelfareRewards.Count == 1;
                Invoke(on, c, new CliVerify.Pkt().H(0).Bytes(), out remain);
                pass &= remain == 0 && m.HasLevelInfo && m.LevelInviteEntries.Count == 0 && m.HasHelpInfo && m.HelpCount == 7 && m.HelpRewards.Count == 1 && m.HelpInviteEntries.Count == 1;
                Invoke(onHelp, c, new CliVerify.Pkt().H(0).H(0).H(0).Bytes(), out helpRemain); pass &= helpRemain == 0 && m.HasHelpInfo && m.HelpCount == 0 && m.HelpRewards.Count == 0 && m.HelpInviteEntries.Count == 0 && m.HasLevelInfo && m.LevelInviteEntries.Count == 0 && m.WelfareRewards.Count == 1;
                Invoke(onWelfare, c, new CliVerify.Pkt().C(255).H(0).Bytes(), out welfareRemain); pass &= welfareRemain == 0 && m.HasWelfareInfo && m.WelfareInfoType == 255 && m.WelfareRewards.Count == 0 && m.HasHelpInfo && m.HasLevelInfo;
                // Dispose 必须从四个非空 slice 出发验证全清，不能借前面的空快照假绿。
                Invoke(onInfo, c, InfoPacket(), out infoRemain);
                Invoke(on, c, PacketSingle(), out remain);
                Invoke(onHelp, c, HelpSinglePacket(), out helpRemain);
                Invoke(onWelfare, c, WelfareSinglePacket(), out welfareRemain);
                pass &= infoRemain == 0 && remain == 0 && helpRemain == 0 && welfareRemain == 0 && m.HasLevelInfo && m.LevelInviteEntries.Count == 1
                    && m.HasHelpInfo && m.HelpCount == 7 && m.HelpRewards.Count == 1 && m.HelpInviteEntries.Count == 1
                    && m.HasWelfareInfo && m.WelfareInfoType == 3 && m.WelfareRewards.Count == 1;
                c.Dispose(); frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                pass &= !handlers.Contains(34000) && !handlers.Contains(34001) && !handlers.Contains(34005) && !handlers.Contains(34006) && !handlers.Contains(34012) && !m.HasLevelInfo && !m.HasHelpInfo && !m.HasWelfareInfo && m.LevelInviteEntries.Count == 0
                    && m.HelpCount == 0 && m.HelpRewards.Count == 0 && m.HelpInviteEntries.Count == 0
                    && m.WelfareInfoType == 0 && m.WelfareRewards.Count == 0
                    && m.GetStatus == 0 && m.RecoverTime == 0 && m.DailyCount == 0 && m.TotalCount == 0 && frames.Count == 0;
                Debug.Log("CLIVERIFY friendinvitelevel pass=" + pass);
            }
            finally
            {
                if (c.IsInitialized) c.Dispose(); m.Reset();
                m.SetInfo(oldGet, oldRecover, oldDaily, oldTotal); if (oldHas) m.ReplaceLevelInfo(oldEntries);
                if (oldHelp) m.ReplaceHelpInfo(oldHelpCount, oldHelpRewards, oldHelpEntries);
                if (oldWelfare) m.ReplaceWelfareInfo(oldWelfareType, oldWelfareRewards);
                FriendInviteModel.ShareOpen = oldShare;
                role.Level = oldRoleLevel; if (roleReady != null) roleReady.SetValue(role, oldRoleReady);
                if (was) c.Init(); if (lastLevel != null) lastLevel.SetValue(c, oldLast);
                if (handlers != null)
                {
                    handlers.Remove(34000); handlers.Remove(34001); handlers.Remove(34005); handlers.Remove(34006); handlers.Remove(34012);
                    if (old34000) handlers[34000] = old34000Ref;
                    if (old34001) handlers[34001] = old34001Ref;
                    if (old34005) handlers[34005] = old34005Ref;
                    if (old34006) handlers[34006] = old34006Ref;
                    if (old34012) handlers[34012] = old34012Ref;
                }
                if (icons != null) { icons.Remove("340"); if (oldIcon) icons["340"] = oldIconRef; }
                if (boxes != null) { boxes.Remove("340"); if (oldBox) boxes["340"] = oldBoxRef; }
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (eventHandlers != null)
                {
                    eventHandlers.Remove(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                    if (oldRoleEvent) eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] = new List<Delegate>(oldRoleSubscribers);
                }
                bool restored = c.IsInitialized == was && m.GetStatus == oldGet && m.RecoverTime == oldRecover && m.DailyCount == oldDaily && m.TotalCount == oldTotal
                    && m.HasLevelInfo == oldHas && SameEntries(m.LevelInviteEntries, oldEntries) && m.HasHelpInfo == oldHelp && m.HelpCount == oldHelpCount && SameRewards(m.HelpRewards, oldHelpRewards) && SameEntries(m.HelpInviteEntries, oldHelpEntries) && m.HasWelfareInfo == oldWelfare && m.WelfareInfoType == oldWelfareType && SameRewards(m.WelfareRewards, oldWelfareRewards) && FriendInviteModel.ShareOpen == oldShare
                    && (lastLevel == null || Equals(lastLevel.GetValue(c), oldLast)) && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept))
                    && role.Level == oldRoleLevel && (roleReady == null || Equals(roleReady.GetValue(role), oldRoleReady))
                    && (icons == null || (icons.Contains("340") == oldIcon && (!oldIcon || ReferenceEquals(icons["340"], oldIconRef))))
                    && (boxes == null || (boxes.Contains("340") == oldBox && (!oldBox || ReferenceEquals(boxes["340"], oldBoxRef))));
                if (handlers != null) restored &= handlers.Contains(34000) == old34000 && handlers.Contains(34001) == old34001 && handlers.Contains(34005) == old34005 && handlers.Contains(34006) == old34006 && handlers.Contains(34012) == old34012
                    && (!old34000 || ReferenceEquals(handlers[34000], old34000Ref)) && (!old34001 || ReferenceEquals(handlers[34001], old34001Ref)) && (!old34005 || ReferenceEquals(handlers[34005], old34005Ref)) && (!old34006 || ReferenceEquals(handlers[34006], old34006Ref)) && (!old34012 || ReferenceEquals(handlers[34012], old34012Ref));
                if (eventHandlers != null) restored &= eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE) == oldRoleEvent
                    && (!oldRoleEvent || SameDelegates((List<Delegate>)eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE], oldRoleSubscribers));
                pass &= restored;
                Debug.Log("CLIVERIFY friendinvitelevel restored=" + restored + " VERDICT=" + (pass ? "PASS" : "FAIL"));
            }
            return pass ? 0 : 3;
        }
        private static List<FriendInviteModel.LevelInviteEntry> Clone(List<FriendInviteModel.LevelInviteEntry> src) { var r = new List<FriendInviteModel.LevelInviteEntry>(); foreach (var e in src) r.Add(new FriendInviteModel.LevelInviteEntry { InviteeId=e.InviteeId, Pos=e.Pos, Name=e.Name, Level=e.Level, Career=e.Career, Status=e.Status }); return r; }
        private static bool SameEntries(List<FriendInviteModel.LevelInviteEntry> a, List<FriendInviteModel.LevelInviteEntry> b) { if(a.Count!=b.Count)return false; for(int i=0;i<a.Count;i++){var x=a[i];var y=b[i];if(x.InviteeId!=y.InviteeId||x.Pos!=y.Pos||x.Name!=y.Name||x.Level!=y.Level||x.Career!=y.Career||x.Status!=y.Status)return false;} return true; }
        private static bool SameDelegates(List<Delegate> a, List<Delegate> b) { if(a.Count!=b.Count)return false; for(int i=0;i<a.Count;i++)if(!ReferenceEquals(a[i],b[i]))return false; return true; }
        private static bool Entry(FriendInviteModel.LevelInviteEntry e, ulong id, byte pos, string name, ushort level, byte career, byte status) => e.InviteeId == id && e.Pos == pos && e.Name == name && e.Level == level && e.Career == career && e.Status == status;
        private static bool HelpMultiState(FriendInviteModel m) => m.HasHelpInfo && m.HelpCount == ushort.MaxValue
            && m.HelpRewards.Count == 2 && m.HelpRewards[0].RewardId == 255 && m.HelpRewards[0].Status == 254
            && m.HelpRewards[1].RewardId == 255 && m.HelpRewards[1].Status == 1 && m.HelpInviteEntries.Count == 2
            && Entry(m.HelpInviteEntries[0], ulong.MaxValue, 255, "中文", ushort.MaxValue, 254, 253)
            && Entry(m.HelpInviteEntries[1], ulong.MaxValue, 255, "", 0, 1, 2);
        private static bool WelfareMultiState(FriendInviteModel m) => m.HasWelfareInfo && m.WelfareInfoType == 255 && m.WelfareRewards.Count == 2
            && m.WelfareRewards[0].RewardId == 255 && m.WelfareRewards[0].Status == 254 && m.WelfareRewards[1].RewardId == 255 && m.WelfareRewards[1].Status == 1;
        private static List<FriendInviteModel.RewardState> CloneRewards(List<FriendInviteModel.RewardState> src) { var r=new List<FriendInviteModel.RewardState>(); foreach(var e in src)r.Add(new FriendInviteModel.RewardState{RewardId=e.RewardId,Status=e.Status}); return r; }
        private static bool SameRewards(List<FriendInviteModel.RewardState> a,List<FriendInviteModel.RewardState>b){if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(a[i].RewardId!=b[i].RewardId||a[i].Status!=b[i].Status)return false;return true;}
        private static void Invoke(MethodInfo h, FriendInviteController c, byte[] b, out int remain) { var r = new NetReader(b, 0, b.Length); h.Invoke(c, new object[] { r }); remain = r.Remaining; }
        private static bool Frames(List<byte[]> f, params int[] ps) { if (f.Count != ps.Length) return false; for (int i = 0; i < ps.Length; i++) { int len = ps[i] == 34012 ? 7 : 6; if (f[i].Length != len || f[i][0] != 0 || f[i][1] != len || f[i][2] != 3 || f[i][3] != 232 || f[i][4] != (byte)(ps[i] >> 8) || f[i][5] != (byte)ps[i] || (ps[i] == 34012 && f[i][6] != 3)) return false; } return true; }
        private static byte[] PacketMulti() => new CliVerify.Pkt().H(2).L(-1).C(255).S("邀请中文").H(65535).C(254).C(253).L(-1).C(255).S("").H(0).C(1).C(2).Bytes();
        private static byte[] PacketSingle() => new CliVerify.Pkt().H(1).L(1).C(2).S("").H(3).C(4).C(5).Bytes();
        private static byte[] InfoPacket() => new CliVerify.Pkt().C(9).I(8).C(7).I(6).H(0).Bytes();
        private static byte[] HelpMultiPacket() => new CliVerify.Pkt().H(65535).H(2).C(255).C(254).C(255).C(1).H(2).L(-1).C(255).S("中文").H(65535).C(254).C(253).L(-1).C(255).S("").H(0).C(1).C(2).Bytes();
        private static byte[] HelpSinglePacket() => new CliVerify.Pkt().H(7).H(1).C(8).C(9).H(1).L(10).C(11).S("single").H(12).C(13).C(14).Bytes();
        private static byte[] WelfareMultiPacket() => new CliVerify.Pkt().C(255).H(2).C(255).C(254).C(255).C(1).Bytes();
        private static byte[] WelfareSinglePacket() => new CliVerify.Pkt().C(3).H(1).C(8).C(9).Bytes();
    }
}
