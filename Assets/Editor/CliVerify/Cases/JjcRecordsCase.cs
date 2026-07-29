using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Jjc;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class JjcRecordsCase
    {
        const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance, SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY jjcrecords " + e); return Task.FromResult(3); } }
        static int RunSync()
        {
            var c = JjcController.Instance; bool was = c.IsInitialized; c.Init(); var m = JjcModel.Instance;
            var old = new List<JjcModel.RecordVo>(m.ChallengeRecords); bool oldLoaded = m.HasChallengeRecords; int oldErr = m.RecordsErrCode;
            var oldBreaks = new List<int>(m.BreakIdList); var oldRivals = new List<JjcModel.RivalVo>(m.Rivals); var oldResult = new List<JjcModel.RivalVo>(m.LastChallengeRoleList);
            bool oldInfo=m.HasInfo, oldRivalsFlag=m.HasRivals, oldResultFlag=m.HasChallengeResult, oldTimes=m.HasTimesInfo, oldWin=m.LastChallengeWin, oldReward=m.IsReward;
            int rank=m.Rank,history=m.HistoryRank,reward=m.RewardRank,hp=m.Hp,num=m.Num,refresh=m.NumRefresh,honour=m.Honour,pet=m.PetId,timesErr=m.TimesErrCode; long combat=m.Combat; ushort left=m.LeftNum,can=m.CanBuyNum; uint at=m.TimesRefreshAt;
            var oldError=m.Error; var oldHonourQuery=m.HonourQuery; var oldParticipants=m.BattleParticipants; var oldStage=m.BattleStage;
            try
            {
                MethodInfo on = c.GetType().GetMethod("On28009", F); FieldInfo fi = c.GetType().GetField("s_outboundIntercept", SF);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = on != null && fi != null && handlers != null;
                void Check(string n, bool ok) { Debug.Log("CLIVERIFY jjcrecords " + n + " ok=" + ok); if (!ok) pass = false; }
                for (int p = 28000; p <= 28018; p++) Check("registration " + p, handlers.Contains(p) == IsRegistered(p));
                object prev = fi.GetValue(null); var frames = new List<byte[]>();
                try {
                    fi.SetValue(null, new Func<byte[], bool>(x => { frames.Add(x); return true; })); m.Clear(); c.RequestChallengeRecords(); Check("empty 28009", Frames(frames, 28009));
                    m.Apply28001(1,2,3,4,5,6,7,8,true,9,new List<int>()); m.Apply28004(10,11,12,13); m.Apply28009(3, new List<JjcModel.RecordVo>{ new JjcModel.RecordVo{RoleId=1} });
                    m.ReplaceError(1); m.ReplaceHonourQuery(2,3); m.ReplaceBattleParticipants(4,5,6,7); m.ReplaceBattleStage(8,9);
                    frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START); Check("game start clears/no 28009", !m.HasInfo&&!m.HasTimesInfo&&!m.HasChallengeRecords
                        &&m.Error==null&&m.HonourQuery==null&&m.BattleParticipants==null&&m.BattleStage==null&&Frames(frames,28004,28001));
                    Feed(on,c,new CliVerify.Pkt().I(0).H(0).Bytes(),out var zero); Check("empty loaded/read-end",zero.Remaining==0&&m.HasChallengeRecords&&m.ChallengeRecords.Count==0);
                    var p = new CliVerify.Pkt().I(uint.MaxValue).H(2); Item(p,ulong.MaxValue,"中文",uint.MaxValue,"",255,254,253,252,ushort.MaxValue,ulong.MaxValue,251,250,uint.MaxValue,uint.MaxValue); Item(p,ulong.MaxValue,"",0,"x",1,2,3,4,5,6,7,8,9,10);
                    Feed(on,c,p.Bytes(),out var many); var a=m.ChallengeRecords[0]; Check("all fields/order/duplicates",many.Remaining==0&&m.RecordsErrCode==-1&&m.ChallengeRecords.Count==2&&a.RoleId==-1&&a.Picture=="中文"&&a.PictureVer==uint.MaxValue&&a.Name==""&&a.Career==255&&a.Sex==254&&a.Turn==253&&a.VipLv==252&&a.Lv==ushort.MaxValue&&a.CombatPower==-1&&a.Result==251&&a.State==250&&a.RankRange==uint.MaxValue&&a.Time==uint.MaxValue&&m.ChallengeRecords[1].RoleId==-1);
                    Check("second preserves order/empty strings",m.ChallengeRecords.Count==2&&m.ChallengeRecords[1].RoleId==-1&&m.ChallengeRecords[1].Picture==""&&m.ChallengeRecords[1].Name=="x"&&m.ChallengeRecords[1].Time==10);
                    m.Apply28001(21,2,3,4,5,26,27,8,true,9,new List<int>()); m.Apply28004(28,29,30,31);
                    Feed(on,c,new CliVerify.Pkt().I(2).H(1).L(3).S("p").I(4).S("n").C(5).C(6).C(7).C(8).H(9).L(10).C(11).C(12).I(13).I(14).Bytes(),out var one); Check("multi-single replace/isolation",one.Remaining==0&&m.ChallengeRecords.Count==1&&m.ChallengeRecords[0].RoleId==3&&m.Rank==21&&m.Num==26&&m.LeftNum==29&&m.TimesRefreshAt==30);
                    Feed(on,c,new CliVerify.Pkt().I(3).H(0).Bytes(),out var empty); Check("single-empty loaded",empty.Remaining==0&&m.HasChallengeRecords&&m.RecordsErrCode==3&&m.ChallengeRecords.Count==0);
                    m.Apply28009(4,new List<JjcModel.RecordVo>{new JjcModel.RecordVo{RoleId=88,Time=77}}); Check("no response keeps",m.ChallengeRecords.Count==1&&m.ChallengeRecords[0].RoleId==88);
                    Feed(c.GetType().GetMethod("On28001",F),c,new CliVerify.Pkt().I(1).I(2).I(3).L(4).I(5).H(6).I(7).I(8).C(1).I(9).H(0).Bytes(),out var i01); Feed(c.GetType().GetMethod("On28004",F),c,new CliVerify.Pkt().I(1).H(2).I(3).H(4).Bytes(),out var i04); Check("reverse isolation",i01.Remaining==0&&i04.Remaining==0&&m.ChallengeRecords.Count==1&&m.ChallengeRecords[0].RoleId==88&&m.ChallengeRecords[0].Time==77);
                    c.Dispose(); frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START); bool removed=true; for(int id=28000;id<=28018;id++)if(IsRegistered(id))removed&=!handlers.Contains(id); Check("dispose",removed&&frames.Count==0&&!m.HasInfo&&!m.HasRivals&&!m.HasChallengeResult&&!m.HasTimesInfo&&!m.HasChallengeRecords
                        &&m.Error==null&&m.HonourQuery==null&&m.BattleParticipants==null&&m.BattleStage==null);
                } finally { fi.SetValue(null,prev); }
                return pass?0:3;
            } finally { if(c.IsInitialized)c.Dispose(); m.Clear(); if(oldInfo)m.Apply28001(rank,history,reward,combat,hp,num,refresh,honour,oldReward,pet,oldBreaks); if(oldRivalsFlag)m.Apply28002(oldRivals); if(oldResultFlag)m.Apply28003(oldWin?1:0,oldResult); if(oldTimes)m.Apply28004(timesErr,left,at,can); if(oldLoaded)m.Apply28009(oldErr,old);
                SetAuto(m,"Error",oldError); SetAuto(m,"HonourQuery",oldHonourQuery); SetAuto(m,"BattleParticipants",oldParticipants); SetAuto(m,"BattleStage",oldStage); if(was)c.Init(); }
        }
        static void Item(CliVerify.Pkt p,ulong id,string pic,uint pv,string n,int ca,int se,int tu,int vip,int lv,ulong cp,int re,int st,uint rr,uint ti){p.L(unchecked((long)id)).S(pic).I(pv).S(n).C(ca).C(se).C(tu).C(vip).H(lv).L(unchecked((long)cp)).C(re).C(st).I(rr).I(ti);}
        static void Feed(MethodInfo m,JjcController c,byte[] b,out NetReader r){r=new NetReader(b,0,b.Length);m.Invoke(c,new object[]{r});}
        static bool Frames(IReadOnlyList<byte[]> f,params int[] ids){if(f.Count!=ids.Length)return false;for(int i=0;i<ids.Length;i++){var x=f[i];if(x==null||x.Length!=6||x[0]!=0||x[1]!=6||x[2]!=3||x[3]!=232||x[4]!=(byte)(ids[i]>>8)||x[5]!=(byte)ids[i])return false;}return true;}
        static bool IsRegistered(int p)=>p==28000||(p>=28001&&p<=28004)||p==28009||p==28010||p==28013||p==28014;
        static void SetAuto(JjcModel m,string p,object v){var f=typeof(JjcModel).GetField("<"+p+">k__BackingField",F);if(f==null)throw new MissingFieldException(typeof(JjcModel).FullName,p);f.SetValue(m,v);}
    }
}
