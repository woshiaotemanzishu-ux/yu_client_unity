using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.LimitLevelShop;
using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class LimitLevelShopGiftConfigCase
    {
        const BindingFlags IF=BindingFlags.Instance|BindingFlags.NonPublic, SF=BindingFlags.Static|BindingFlags.NonPublic;
        sealed class W { public ushort G,R,D; public string A,B,C,E,F,H; }
        public static Task<int> Run(){try{return Task.FromResult(RunSync());}catch(Exception e){Debug.LogError("CLIVERIFY limit-shop-config EXCEPTION "+e);return Task.FromResult(3);}}
        static int RunSync()
        {
            var c=LimitLevelShopController.Instance; var m=LimitLevelShopModel.Instance; bool oi=c.IsInitialized;
            var map=(Dictionary<(ushort,ushort),LimitLevelShopModel.GiftConfigSnapshot>)typeof(LimitLevelShopModel).GetField("_giftConfigs",IF).GetValue(m); var om=new Dictionary<(ushort,ushort),LimitLevelShopModel.GiftConfigSnapshot>(map);
            var gifts=new List<LimitLevelShopModel.GiftEntry>(m.Gifts);
            var sentinelGifts=new List<LimitLevelShopModel.GiftEntry>{new LimitLevelShopModel.GiftEntry(
                32000,32001,123456,
                Array.Empty<LimitLevelShopModel.GradeState>(),Array.Empty<LimitLevelShopModel.GradeState>(),
                "",0,LimitLevelShopModel.ICON_TYPE)};
            var fi=typeof(LimitLevelShopController).GetField("s_giftConfigOutboundIntercept",SF); object old=fi.GetValue(null);
            var hs=typeof(NetManager).GetField("_handlers",SF).GetValue(null) as IDictionary; bool he=hs.Contains(Proto.LIMITLEVELSHOP_GIFT_CONFIG); object oh=he?hs[Proto.LIMITLEVELSHOP_GIFT_CONFIG]:null; bool pass=false,restored=false;
            try{
                map.Clear(); m.SetGiftList(sentinelGifts); var on=typeof(LimitLevelShopController).GetMethod("On61203",IF); pass=Proto.LIMITLEVELSHOP_GIFT_CONFIG==61203&&on!=null&&(!oi||he); Check(ref pass,"seams/registration",pass);
                var fs=new List<byte[]>(); fi.SetValue(null,new Func<byte[],bool>(x=>{fs.Add(x);return true;})); c.RequestGiftConfig(ushort.MaxValue,0,ushort.MaxValue); Check(ref pass,"exact 12B request",fs.Count==1&&Frame(fs[0]));
                m.ApplyGiftConfig(1,2,new List<LimitLevelShopModel.GiftConfigEntry>{new LimitLevelShopModel.GiftConfigEntry(1,"a","b","c","d","e","f",1,1)}); var seed=Get(m,1,2); c.RequestGiftConfig(1,2,1); Check(ref pass,"no response keeps",ReferenceEquals(Get(m,1,2),seed));
                var many=new[]{new W{G=ushort.MaxValue,A="",B="中",C="raw",E="页",F="奖",H="条",R=ushort.MaxValue,D=0},new W{G=0,A="a",B="b",C="c",E="d",F="e",H="f",R=0,D=ushort.MaxValue},new W{G=ushort.MaxValue,A="x",B="y",C="z",E="",F="",H="",R=7,D=8}};
                Check(ref pass,"multi bounds duplicate raw order/read-tail",Feed(on,c,0,1,many)&&Snap(m,0,1,many)); var multi=Get(m,0,1);
                var one=new[]{new W{G=3,A="",B="",C="",E="",F="",H="",R=4,D=5}}; Check(ref pass,"same bucket multi-to-single",Feed(on,c,0,1,one)&&Snap(m,0,1,one)&&!ReferenceEquals(Get(m,0,1),multi));
                var other=new[]{new W{G=9,A="a",B="b",C="c",E="d",F="e",H="f",R=1,D=2}}; Check(ref pass,"different subtype isolated",Feed(on,c,0,2,other)&&Snap(m,0,2,other)&&Snap(m,0,1,one)); var before=Get(m,0,1);
                var otherSnapshot=Get(m,0,2); m.SetGiftList(new List<LimitLevelShopModel.GiftEntry>{new LimitLevelShopModel.GiftEntry(
                    9,10,11,
                    Array.Empty<LimitLevelShopModel.GradeState>(),Array.Empty<LimitLevelShopModel.GradeState>(),
                    "",0,LimitLevelShopModel.ICON_TYPE)}); Check(ref pass,"61200 model write leaves 61203 snapshot",ReferenceEquals(Get(m,0,2),otherSnapshot)); m.SetGiftList(sentinelGifts);
                Check(ref pass,"single-to-empty new loaded snapshot",Feed(on,c,0,1,Array.Empty<W>())&&Get(m,0,1).Loaded&&Get(m,0,1).Entries.Count==0&&!ReferenceEquals(Get(m,0,1),before));
                var readonlyEntries=Get(m,0,2).Entries; bool addThrows=false; try { ((IList<LimitLevelShopModel.GiftConfigEntry>)readonlyEntries).Add(null); } catch (NotSupportedException) { addThrows=true; }
                Check(ref pass,"entries truly read-only",addThrows);
                m.ClearGiftConfigs(); Check(ref pass,"clear only config/61200 isolate",!m.TryGetGiftConfig(0,1,out _)&&SameGifts(m.Gifts,sentinelGifts));
                m.ApplyGiftConfig(8,8,new List<LimitLevelShopModel.GiftConfigEntry>()); m.Reset(); Check(ref pass,"reset clears 61200 and 61203",!m.TryGetGiftConfig(8,8,out _)&&m.Gifts.Count==0); m.SetGiftList(sentinelGifts);
                Check(ref pass,"ambient",c.IsInitialized==oi&&hs.Contains(Proto.LIMITLEVELSHOP_GIFT_CONFIG)==he&&(!he||ReferenceEquals(hs[Proto.LIMITLEVELSHOP_GIFT_CONFIG],oh))); Debug.Log("CLIVERIFY limit-shop-config VERDICT pass="+pass);
            }finally{map.Clear();foreach(var p in om)map.Add(p.Key,p.Value);m.SetGiftList(gifts);fi.SetValue(null,old);restored=c.IsInitialized==oi&&Same(map,om)&&SameGifts(m.Gifts,gifts)&&hs.Contains(Proto.LIMITLEVELSHOP_GIFT_CONFIG)==he&&(!he||ReferenceEquals(hs[Proto.LIMITLEVELSHOP_GIFT_CONFIG],oh))&&ReferenceEquals(fi.GetValue(null),old);Debug.Log("CLIVERIFY limit-shop-config restored="+restored);} return pass&&restored?0:3;
        }
        static LimitLevelShopModel.GiftConfigSnapshot Get(LimitLevelShopModel m,ushort t,ushort s){m.TryGetGiftConfig(t,s,out var x);return x;}
        static bool Feed(MethodInfo on,LimitLevelShopController c,ushort t,ushort s,W[] a){var p=new CliVerify.Pkt().H(t).H(s).H(a.Length);foreach(var x in a)p.H(x.G).S(x.A).S(x.B).S(x.C).S(x.E).S(x.F).S(x.H).H(x.R).H(x.D);var b=p.Bytes();var r=new NetReader(b,0,b.Length);on.Invoke(c,new object[]{r});return r.Remaining==0;}
        static bool Frame(byte[] b)=>b!=null&&b.Length==12&&b[0]==0&&b[1]==12&&b[2]==3&&b[3]==232&&b[4]==239&&b[5]==19&&b[6]==255&&b[7]==255&&b[8]==0&&b[9]==0&&b[10]==255&&b[11]==255;
        static bool Snap(LimitLevelShopModel m,ushort t,ushort s,W[] a){var x=Get(m,t,s);if(x==null||!x.Loaded||x.Type!=t||x.Subtype!=s||x.Entries.Count!=a.Length)return false;for(int i=0;i<a.Length;i++){var e=x.Entries[i];if(e.Grade!=a[i].G||e.RechargeId!=a[i].R||e.Discount!=a[i].D||e.NormalCost!=a[i].A||e.Cost!=a[i].B||e.Show!=a[i].C||e.PageString!=a[i].E||e.Reward!=a[i].F||e.Condition!=a[i].H)return false;}return true;}
        static bool Same(Dictionary<(ushort,ushort),LimitLevelShopModel.GiftConfigSnapshot>a,Dictionary<(ushort,ushort),LimitLevelShopModel.GiftConfigSnapshot>b){if(a.Count!=b.Count)return false;foreach(var p in b)if(!a.TryGetValue(p.Key,out var x)||!ReferenceEquals(x,p.Value))return false;return true;} static bool SameGifts(IReadOnlyList<LimitLevelShopModel.GiftEntry>a,List<LimitLevelShopModel.GiftEntry>b){if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(!a[i].Equals(b[i]))return false;return true;} static void Check(ref bool p,string n,bool ok){Debug.Log("CLIVERIFY limit-shop-config "+n+" ok="+ok);p&=ok;}
    }
}
