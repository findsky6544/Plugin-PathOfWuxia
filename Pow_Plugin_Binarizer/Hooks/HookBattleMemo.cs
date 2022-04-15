﻿using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using HarmonyLib;
using Heluo.UI;
using Heluo.Battle;
using Heluo.Data;
using UnityEngine.UI;
using Heluo.FSM.Battle;
using Heluo.Flow.Battle;

namespace PathOfWuxia
{

[DisplayName( "战斗记录" )]
[Description( "添加类似于前作的战斗信息记录,默认关闭.点击屏幕右上角箭头开关." )]
public class HookBattleMemo : IHook
{
    static ConfigEntry<int> board_style;
    static ConfigEntry<int> scroll_sensitivity;
    static ConfigEntry<bool> autoUpdateMsg;
    static ConfigEntry<bool> showTurnZero;
    static ConfigEntry<bool> showAura;
    static ConfigEntry<int> height;
    static int last_style = 0;
    static int last_height = 0;
    static int repeated = 1;
    static int turn = 0;
    static bool ispred = false;
    static int HpBeforeDmg;
    static int MpBeforeDmg;
    static int auraCount = 0;
    static bool cb = false;
    enum AttackType {
        Summon,
        Heal,
        Buff,
        Normal,
        Batter,
        Persuit,
        Counter,
        Preemptive,
        Support,
        Null

    }

    //static Stack<AttackType> AttackTypeStack = new Stack<AttackType>();
    static AttackType newAttackType = AttackType.Null;

    static List<String> msgs = new List<String>();
    static Queue<string> description = new Queue<string>();
    //static int msgCount = 0;
    const int maxCount = 5000;
    static GameObject curContent = new GameObject();
    static GameObject curArrow = new GameObject(); //方便找
    //哎反正占不了多大空间
    //补充说明栏的对象
    static GameObject curDetails = new GameObject();
    //Do I really need to do such a stupid thing?
    public class CompForSavingSingleStr : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        bool isHovering = false;
        float timeLeft;
        public string just_a_string;
        public void OnPointerEnter( PointerEventData eventData )
        {
            timeLeft = 0.5f;
            isHovering = true;
        }
        public void OnPointerExit( PointerEventData eventData )
        {
            isHovering = false;
            if( curDetails != null ) {
                curDetails.GetComponent<Text>().text = "";
                curDetails.SetActive( false );
                //Console.WriteLine("pointer exit");
            }
        }
        void Update()
        {
            if( isHovering ) {
                timeLeft -= Time.deltaTime;
            }
            if( timeLeft < 0 ) {
                if( curDetails != null && curDetails.GetComponent<Text>() != null ) {
                    curDetails.SetActive( true );
                    curDetails.GetComponent<Text>().text = just_a_string;
                    //Console.WriteLine("show details"+ just_a_string);
                }
                timeLeft = 0.5f;
                isHovering = false;
            }
        }
    }

    public void OnRegister( PluginBinarizer plugin )
    {
        Console.WriteLine( "加载战斗记录" );

        board_style = plugin.Config.Bind( "战斗记录", "战斗面板样式", 0,
                                          new ConfigDescription( "扣了几张图当背景", new AcceptableValueRange<int>( 0, 6 ) ) );
        scroll_sensitivity = plugin.Config.Bind( "战斗记录", "战斗面板滚轮灵敏度", 1,
                             new ConfigDescription( "滚轮灵敏度", new AcceptableValueRange<int>( 1, 50 ) ) );
        autoUpdateMsg = plugin.Config.Bind( "战斗记录", "面板信息自动滚动刷新", true,
                                            "如果取消此选项，那么当新信息被记录时不会自动滚动，请手动浏览记录" );
        showTurnZero = plugin.Config.Bind( "战斗记录", "显示第一回合开始前的buff", true,
                                           "如果取消此选项，那么记录会从玩家的第一个回合开始" );
        showAura = plugin.Config.Bind( "战斗记录", "显示光环相关的buff", false,
                                       "如果开启此选项，你的眼睛会瞎" );
        height = plugin.Config.Bind( "战斗记录", "记录牌高度", 5,
                                     new ConfigDescription( "调整高度", new AcceptableValueRange<int>( 0, 15 ) ) );
        plugin.onUpdate += OnUpdate;
        Console.WriteLine( "加载战斗记录结束" );
    }

    //即时调整
    public void OnUpdate()
    {
        if( curContent && curContent.gameObject && curContent.gameObject.activeInHierarchy ) {
            UILoopVerticalScrollRect scr = curContent.transform.parent.GetComponent<UILoopVerticalScrollRect>();
            if( scr != null ) {
                scr.scrollSensitivity = scroll_sensitivity.Value;
                if( scr.gameObject.GetComponent<RectTransform>() != null && height.Value != last_height ) {
                    last_height = height.Value;
                    scr.gameObject.GetComponent<RectTransform>().sizeDelta =  new Vector2( 800f,
                            600f + ( height.Value - 5 ) * 60 );
                    update_msg();
                }
            }

        }
        if( board_style.Value != last_style ) {

            last_style = board_style.Value;
            if( curContent && curContent.gameObject && curContent.gameObject.activeInHierarchy ) {

                if( getpath() != null ) {
                    curContent.transform.parent.gameObject.GetComponent<Image>().sprite =
                        Heluo.Game.Resource.Load<Sprite>( getpath() );
                } else {
                    curContent.transform.parent.gameObject.GetComponent<Image>().sprite = null;
                }
            }


            for( int i = 0; i < curContent.GetComponent<RectTransform>().childCount; i++ ) {
                curContent.GetComponent<RectTransform>().GetChild( i ).GetComponent<Text>().color =
                    ( board_style.Value == 3 || board_style.Value > 4 ||
                      board_style.Value < 0 ? new Color( 0, 0, 0, 1 ) : new Color( 1, 1, 1, 1 ) );
            }
        }

    }
    static string getpath()
    {
        switch( board_style.Value ) {
            case 0:
                return "assets/image/ui/uibattle/battle_ability_base.png";
            case 1:
                return "assets/image/ui/uiadjustment/team_board.png";
            case 2:
                return "assets/image/ui/uitarget/target_03.png";
            case 3:
                return "assets/image/ui/uicharacter/form_subscreen.png";
            case 4:
                return "assets/image/ui/uimain/mod_form.png";
            case 5:
                return "assets/image/ui/uiending/01/0100.png";
            default:
                return null;
        }
    }

    //功能性的函数
    //把存储的消息同步到屏幕
    static public void update_msg()
    {
        //Console.WriteLine( "同步消息开始." );
        if( !curContent ) {
            //Console.WriteLine( "没找到对象" );
            return;
        }

        while( curContent.GetComponent<RectTransform>().childCount < msgs.Count ) {
            //Console.WriteLine( "childCount" + curContent.GetComponent<RectTransform>().childCount +
            //                   "    msgs.Count" + msgs.Count );
            if( curContent.GetComponent<RectTransform>().childCount > 1 &&
                msgs[curContent.GetComponent<RectTransform>().childCount].Equals(
                    msgs[curContent.GetComponent<RectTransform>().childCount - 1] ) ) {

                repeated++;
                if( curContent.GetComponent<RectTransform>().GetChild(
                        curContent.GetComponent<RectTransform>().childCount - 1 ) ) {
                    //Console.WriteLine( "object exists" + curContent.GetComponent<RectTransform>().GetChild(
                    //                       curContent.GetComponent<RectTransform>().childCount - 1 ).name );
                }
                Text ttt = curContent.GetComponent<RectTransform>().GetChild(
                               curContent.GetComponent<RectTransform>().childCount - 1 ).gameObject.GetComponent<Text>();
                //Console.WriteLine( "flaggg text length" + ttt.text.Length );
                ttt.text = ttt.text.Remove( ttt.text.Length - 3 );
                //Console.WriteLine( "flagaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" );
                ttt.text += "×" + repeated;
                //Console.WriteLine( ttt.text );
                if( msgs[curContent.GetComponent<RectTransform>().childCount].StartsWith( "•" ) ||
                    msgs[curContent.GetComponent<RectTransform>().childCount].StartsWith( "+" ) ||
                    msgs[curContent.GetComponent<RectTransform>().childCount].StartsWith( "-" ) ) {
                    HookBattleMemo.description.Dequeue();
                }
                msgs.RemoveAt( curContent.GetComponent<RectTransform>().childCount );

                if( curContent.GetComponent<RectTransform>().childCount == msgs.Count ) {
                    break;
                }
                //Console.WriteLine( "flagbbbbbbbbbbbbbbbbbbbbbbb " );

            } else {
                repeated = 1;
            }

            //Console.WriteLine( "flagccccccccccccccccccccccccccc " );
            GameObject curBlock = new GameObject( "msgBlock", typeof( RectTransform ), typeof( Text ),
                                                  typeof( LayoutElement ) );
            //Text类的似乎不需要设置添加LayoutElement？反正先随便设置一下有问题再删
            curBlock.GetComponent<LayoutElement>().flexibleWidth = 0f;
            curBlock.GetComponent<LayoutElement>().preferredWidth = 750f;
            curBlock.GetComponent<LayoutElement>().flexibleHeight = 0f;
            curBlock.GetComponent<LayoutElement>().preferredHeight = 30f;
            //Console.WriteLine( "flagdddddddddddddddddddddddddddddd " );


            if( curContent != null ) {
                //Console.WriteLine( "flag childCount " + curContent.GetComponent<RectTransform>().childCount +
                //                   "msgs count" + msgs.Count );
                //Console.WriteLine( "设定文本." + msgs[curContent.GetComponent<RectTransform>().childCount] );
            }
            curBlock.GetComponent<Text>().font = Heluo.Game.Resource.Load<Font>( "Assets/Font/kaiu.ttf" );
            curBlock.GetComponent<Text>().fontSize = 20;
            curBlock.GetComponent<Text>().text = msgs[curContent.GetComponent<RectTransform>().childCount] +
                                                 "      ";
            if( curBlock.GetComponent<Text>().text.StartsWith( "•" ) ) {
                curBlock.GetComponent<Text>().fontStyle = FontStyle.Bold;
                //补充说明
                curBlock.AddComponent<CompForSavingSingleStr>().just_a_string =
                    HookBattleMemo.description.Dequeue();
            }
            if( curBlock.GetComponent<Text>().text.StartsWith( "+" ) ||
                curBlock.GetComponent<Text>().text.StartsWith( "-" ) ) {
                //补充说明
                curBlock.AddComponent<CompForSavingSingleStr>().just_a_string =
                    HookBattleMemo.description.Dequeue();

            }
            if( curBlock.GetComponent<Text>().text.StartsWith( "=" ) ) {
                curBlock.GetComponent<Text>().fontStyle = FontStyle.Italic;
            }

            curBlock.GetComponent<Text>().color = ( board_style.Value == 3 || board_style.Value > 4 ||
                                                    board_style.Value < 0 ? new Color( 0, 0, 0, 1 ) : new Color( 1, 1, 1, 1 ) );


            /*
            Console.WriteLine("添加parent.");
            Console.WriteLine("active? " + (curContent.GetComponent<VerticalLayoutGroup>().IsActive()? "true" : "false") );
            Console.WriteLine("Font"+ curBlock.GetComponent<Text>().font.name);
            Console.WriteLine("Fontsize" + curBlock.GetComponent<Text>().fontSize);
            Console.WriteLine("colour r" + curBlock.GetComponent<Text>().color.r + "colour g"+ curBlock.GetComponent<Text>().color.g + "colour b"+curBlock.GetComponent<Text>().color.b);
            */
            curBlock.GetComponent<RectTransform>().sizeDelta = new Vector2( 750f, 30f );
            curBlock.GetComponent<RectTransform>().SetParent( curContent.GetComponent<RectTransform>(), false );
            //Console.WriteLine( "debug: " );
            //Console.WriteLine( "anchormin" + curBlock.GetComponent<RectTransform>().anchorMin );
            //Console.WriteLine( "anchormax" + curBlock.GetComponent<RectTransform>().anchorMax );
            //Console.WriteLine( "pivot" + curBlock.GetComponent<RectTransform>().pivot );
            //Console.WriteLine( "localposition" + curBlock.GetComponent<RectTransform>().localPosition );
            //Console.WriteLine( "anchoredPosition" + curBlock.GetComponent<RectTransform>().anchoredPosition );
            // Console.WriteLine( "sizeDelta" + curBlock.GetComponent<RectTransform>().sizeDelta );
            //Console.WriteLine( "text:  " + curBlock.GetComponent<Text>().text );
            //Console.WriteLine( "添加parent 结束." );
            //Console.WriteLine( "child count " + curContent.GetComponent<RectTransform>().childCount );
            curBlock.SetActive( true );
        }

        while( msgs.Count > maxCount ) {
            //Console.WriteLine( "删除队首元素" );
            GameObject.Destroy( curContent.GetComponent<RectTransform>().GetChild( 0 ) );
            curContent.GetComponent<RectTransform>().GetChild( 0 ).SetParent( null );
            //Console.WriteLine( "child count " + curContent.GetComponent<RectTransform>().childCount );
            msgs.RemoveAt( 0 );
            // Console.WriteLine( "msg length " + msgs.Count );
        }


        //Console.WriteLine( "同步消息结束." );
        //Console.WriteLine( "reposition" );


        //msgs.Add("-------------------------------------------------------");
        if( !autoUpdateMsg.Value ) {
            return;
        }
        //位置都是乱设的，居然还能用
        Vector2 localpos = curContent.GetComponent<RectTransform>().localPosition;
        localpos.y = + Math.Max( msgs.Count - 20 - 2 * ( height.Value - 5 ), 0 ) * 30;
        curContent.GetComponent<RectTransform>().localPosition = localpos;
        //Console.WriteLine( "end reposition" );
        LayoutRebuilder.ForceRebuildLayoutImmediate( curContent.GetComponent<RectTransform>() );

    }
    //*************************************************
    // 下面是补丁
    //****************************************************

    [HarmonyPrefix, HarmonyPatch( typeof( AssignAuraPromoteAction ),
                                  nameof( AssignAuraPromoteAction.GetValue ) )]
    public static void preAssignAura()
    {
        auraCount ++;
    }
    [HarmonyPostfix, HarmonyPatch( typeof( AssignAuraPromoteAction ),
                                   nameof( AssignAuraPromoteAction.GetValue ) )]
    public static void postAssignAura()
    {
        auraCount --;
    }
    [HarmonyPrefix, HarmonyPatch( typeof( SingleAuraPromoteAction ),
                                  nameof( SingleAuraPromoteAction.GetValue ) )]
    public static void preSingleAura()
    {
        auraCount++;
    }
    [HarmonyPostfix, HarmonyPatch( typeof( SingleAuraPromoteAction ),
                                   nameof( SingleAuraPromoteAction.GetValue ) )]
    public static void postSingleAura()
    {
        auraCount--;
    }
    [HarmonyPrefix, HarmonyPatch( typeof( AuraPromoteAction ), nameof( AuraPromoteAction.GetValue ) )]
    public static void preAura()
    {
        auraCount++;
    }
    [HarmonyPostfix, HarmonyPatch( typeof( AuraPromoteAction ), nameof( AuraPromoteAction.GetValue ) )]
    public static void postAura()
    {
        auraCount--;
    }


    [HarmonyPostfix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.Die ) )]
    public static void died( WuxiaUnit __instance )
    {
        msgs.Add( "= " + __instance.FullName + " 失去战斗能力并离开战场" );
        update_msg();
    }
    [HarmonyPostfix, HarmonyPatch( typeof( WuxiaBattleBuffer ), nameof( WuxiaBattleBuffer.AddBuffer ),
                                   new Type[] { typeof( WuxiaUnit ), typeof( Heluo.Data.Buffer ), typeof( BufferType ) } )]
    public static void addBuffer( WuxiaUnit unit, Heluo.Data.Buffer buffer, BufferType type )
    {

        if( auraCount > 0 && !showAura.Value ) {
            return;
        }
        if( turn == 0 && !showTurnZero.Value ) {
            return;
        }
        if( unit == null && buffer == null ) {
            //Console.WriteLine( "addbuffer patch end" );
            return;
        }

        string str = "+ " + unit.FullName + "受到效果 " + buffer.Name +
                     ( buffer.Times > 0 ? ( " 持续" + buffer.Times + " 回合" ) : "" ) + ", 来源: ";
        //Console.WriteLine( str );
        switch( type ) {
            case BufferType.Difficulty:
                str += "难度";
                break;
            case BufferType.Drama:
                str += "场景";
                break;
            case BufferType.Equip:
                str += "装备";
                break;
            case BufferType.Mantra:
                str += "心法";
                break;
            case BufferType.Medicine:
                str += "丹药";
                break;
            case BufferType.Skill:
                str += "招式";
                break;
            case BufferType.Special:
                str += "特殊";
                break;
            case BufferType.Trait:
                str += "天赋";
                break;
            case BufferType.None:
            default:
                str += "无";
                break;
        }
        msgs.Add( str );
        description.Enqueue( buffer.Desc );
        update_msg();
        //Console.WriteLine( "addbuffer patch end" );
    }

    [HarmonyPrefix, HarmonyPatch( typeof( WuxiaBattleBuffer ), nameof( WuxiaBattleBuffer.RemoveBuffer ),
                                  new Type[] {typeof( WuxiaUnit ), typeof( string )} )]
    public static bool removeBuffer( WuxiaBattleBuffer __instance, List<BufferInfo> ___BufferList,
                                     WuxiaUnit _unit, string _bufferId )
    {

        //Console.WriteLine( "removebuffer patch" );
        if( auraCount > 0 && !showAura.Value ) {
            return true;
        }
        if( turn == 0 && !showTurnZero.Value ) {
            return true;
        }
        if( string.IsNullOrEmpty( _bufferId ) ) {
            return true;
        }
        if( _unit == null ) {
            return true;
        }
        if( !__instance.IsExist( _unit.UnitID, _bufferId ) ) {
            //Console.WriteLine( "removebuffer patch error" );
            return true;
        }

        BufferInfo bufferInfo = ___BufferList.Find( ( BufferInfo i ) => i.UnitId == _unit.UnitID &&
                                i.BufferId == _bufferId );
        if( bufferInfo == null ) {
            //Console.WriteLine( "removebuffer erro, list length =" + ___BufferList.Count );
            return true;
        } else {
            //Console.WriteLine( "buffer info found" );
        }

        if( bufferInfo.Table == null || bufferInfo.Table.Name == null ) {
            return true;
        }
        string str = "- " + _unit.FullName + " 失去效果 " + bufferInfo.Table.Name;
        msgs.Add( str );
        description.Enqueue( bufferInfo.Table.Desc );
        //Console.WriteLine( "added remove msg:" + str );
        update_msg();

        //Console.WriteLine( "removebuffer patch end" );
        return true;
    }


    [HarmonyPostfix, HarmonyPatch( typeof( WuxiaBattleManager ),
                                   nameof( WuxiaBattleManager.OnBattleEvent ) )]
    public static void postBattleEvent( WuxiaBattleManager __instance, BattleEventToggleTime time,
                                        params object[] args )
    {
        if( __instance.IsEvent ) {
            return;
        }
        if( time == BattleEventToggleTime.BeginTurn ) {
            if( __instance.BattleFSM.CurrentFaction == Faction.Player ) {
                HookBattleMemo.turn++;
                if( HookBattleMemo.turn > 0 ) {
                    msgs.Add( "============================第 " + HookBattleMemo.turn +
                              " 回合 ==================================" );
                    msgs.Add( "============================己方阵营回合开始============================" );

                }

            } else if( __instance.BattleFSM.CurrentFaction == Faction.Teamofplayer ) {
                msgs.Add( "============================友军阵营回合开始============================" );
            } else if( __instance.BattleFSM.CurrentFaction == Faction.AbsolutelyNeutral ) {
                msgs.Add( "============================中立阵营回合开始============================" );
            }
        } else if( time == BattleEventToggleTime.BeginAITurn ) {
            if( __instance.BattleFSM.CurrentFaction == Faction.Enemy ) {
                msgs.Add( "============================敌方阵营回合开始============================" );
            } else {
                msgs.Add( "============================混乱阵营回合开始============================" );
            }

        }
        update_msg();
    }


    [HarmonyPrefix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.OnBufferEvent ) )]
    public static void preBufferEvent( WuxiaUnit __instance, BufferTiming time )
    {
        if( time == BufferTiming.EndUnit ) {
            msgs.Add( "= " + __instance.FullName + " 结束行动" );
            if( __instance[BattleLiberatedState.InfiniteAction] > 0 ||
                __instance[BattleLiberatedState.Encourage] > 0 ) {
                msgs.Add( "= " + __instance.FullName + " 再次行动" );
            }
            if( cb == true ) {
                msgs.Add( "= 连斩! " + __instance.FullName + " 再次行动" );
                cb = false;
            }
        }
        if( time == BufferTiming.Continuous_Beheading ) {
            cb = true;
        }
        update_msg();
    }

    [HarmonyPostfix, HarmonyPatch( typeof( BattleComputer ),
                                   nameof( BattleComputer.Calculate_Basic_Dart_Damage ) )]
    public static void preddart( bool _is_random )
    {
        if( !_is_random ) {
            ispred = true;
        }
    }
    [HarmonyPostfix, HarmonyPatch( typeof( BattleComputer ),
                                   nameof( BattleComputer.Calculate_Basic_Damage ) )]
    public static void predbasic( bool _is_random )
    {
        if( !_is_random ) {
            ispred = true;
        }
    }
    [HarmonyPrefix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.DamageMP ) )]
    public static void beforeMPdmg( WuxiaUnit __instance, int value, Heluo.Battle.MessageType type )
    {
        MpBeforeDmg = __instance[BattleProperty.MP];
    }
    [HarmonyPrefix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.DamageHP ) )]
    public static void beforedmg( WuxiaUnit __instance, int value, Heluo.Battle.MessageType type )
    {
        HpBeforeDmg = __instance[BattleProperty.HP];
    }
    [HarmonyPostfix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.DamageMP ) )]
    public static void damageMp( WuxiaUnit __instance, int value, Heluo.Battle.MessageType type )
    {
        if( value == 0 ) {
            return;
        }
        string des;
        if( type == Heluo.Battle.MessageType.Therapy ) {
            des = "回复";
        } else {
            des = "损失";
        }
        string str1;
        string str2;
        des = "= " + __instance.FullName + des + value + "点内力";

        des = des.PadRight( 25 - __instance.FullName.Length );
        str1 = "MP:" + MpBeforeDmg.ToString().PadLeft( 6 )  + " -> ";
        str1 = str1.PadLeft( 20 );
        str2 = __instance[BattleProperty.MP].ToString().PadRight( 6 ) + "  /" +
               __instance[BattleProperty.Max_MP].ToString().PadRight( 6 );

        msgs.Add( des + str1 + str2 );
        update_msg();
    }
    [HarmonyPostfix, HarmonyPatch( typeof( WuxiaUnit ), nameof( WuxiaUnit.DamageHP ) )]
    public static void damageHp( WuxiaUnit __instance, int value, Heluo.Battle.MessageType type )
    {
        if( value == 0 ) {
            return;
        }
        string des;
        if( type == Heluo.Battle.MessageType.Heal ) {
            des = "点治疗";
        } else {
            des = "点伤害";
        }
        string str1;
        string str2;
        des = "= " + __instance.FullName + "受到" + value + des;

        des = des.PadRight( 25 - __instance.FullName.Length );
        str1 = "HP:" + HpBeforeDmg.ToString().PadLeft( 6 )  + " -> ";
        str1 = str1.PadLeft( 20 );
        str2 = __instance[BattleProperty.HP].ToString().PadRight( 6 ) + "  /" +
               __instance[BattleProperty.Max_HP].ToString().PadRight( 6 );

        msgs.Add( des + str1 + str2 );
        update_msg();
    }
    //攻击类型和伤害类型分开，我试图确定伤害的攻击方式
    //很多async， 蛋疼，不太确定具体执行顺序
    [HarmonyPrefix, HarmonyPatch( typeof( AttackProcessStrategy ),
                                  nameof( AttackProcessStrategy.Process ), new Type[] {typeof( BattleEventArgs ) } )]
    public static void preAttack( BattleEventArgs arg )
    {
        AttackEventArgs e = arg as AttackEventArgs;
        //AttackTypeStack.Push(AttackType.Normal);
        if( e.IsBatter ) {
            newAttackType = AttackType.Batter;
        } else {
            newAttackType = AttackType.Normal;
        }
        //Console.WriteLine( "加入普攻事件" );
    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(AttackProcessStrategy), nameof(AttackProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postAttack(BattleEventArgs arg)
    {
        //AttackTypeStack.Pop();
        newAttackType = AttackType.Null;
        Console.WriteLine("弹出普攻");
    }
    */
    [HarmonyPrefix, HarmonyPatch( typeof( PursuitProcessStrategy ),
                                  nameof( PursuitProcessStrategy.Process ), new Type[] { typeof( BattleEventArgs ) } )]
    public static void prePursuit( BattleEventArgs arg )
    {
        newAttackType = AttackType.Persuit;
        //Console.WriteLine( "加入追击事件" );
    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(PursuitProcessStrategy), nameof(PursuitProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postPursuit(BattleEventArgs arg)
    {
        newAttackType = AttackType.Null;

        Console.WriteLine("弹出追击");
    }
    */
    [HarmonyPrefix, HarmonyPatch( typeof( BuffProcessStrategy ), nameof( BuffProcessStrategy.Process ),
                                  new Type[] { typeof( BattleEventArgs ) } )]
    public static void preBuff( BattleEventArgs arg )
    {
        newAttackType = AttackType.Buff;

        //Console.WriteLine( "加入buff事件" );
    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(BuffProcessStrategy), nameof(BuffProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postBuff(BattleEventArgs arg)
    {
        newAttackType = AttackType.Null;
        Console.WriteLine("弹出buff");
    }
    */
    [HarmonyPrefix, HarmonyPatch( typeof( HealProcessStrategy ), nameof( HealProcessStrategy.Process ),
                                  new Type[] { typeof( BattleEventArgs ) } )]
    public static void preHeal( BattleEventArgs arg )
    {
        newAttackType = AttackType.Heal;
        //Console.WriteLine( "加入heal事件" );
    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(HealProcessStrategy), nameof(HealProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postHeal(BattleEventArgs arg)
    {
        newAttackType = AttackType.Null;
        Console.WriteLine("弹出heal");
    }
    */
    [HarmonyPrefix, HarmonyPatch( typeof( SummonProcessStrategy ),
                                  nameof( SummonProcessStrategy.Process ), new Type[] { typeof( BattleEventArgs ) } )]
    public static void preSummonl( BattleEventArgs arg )
    {
        newAttackType = AttackType.Summon;
        //Console.WriteLine( "加入summon事件" );
    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(SummonProcessStrategy), nameof(SummonProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postSummonl(BattleEventArgs arg)
    {
        newAttackType = AttackType.Null;
        Console.WriteLine("弹出summon");

    }
    */

    [HarmonyPrefix, HarmonyPatch( typeof( CounterProcessStrategy ),
                                  nameof( CounterProcessStrategy.Process ), new Type[] { typeof( BattleEventArgs ) } )]
    public static void preCounter( BattleEventArgs arg )
    {
        CounterEventArgs temparg = arg as CounterEventArgs;
        switch( temparg.Type ) {
            case CounterEventArgs.CounterType.Counter:
                newAttackType = AttackType.Counter;
                //Console.WriteLine( "加入反击事件" );
                break;
            case CounterEventArgs.CounterType.Preemptive:
                newAttackType = AttackType.Preemptive;

                //Console.WriteLine( "加入先制事件" );
                break;
            case CounterEventArgs.CounterType.Support:
                newAttackType = AttackType.Support;
                //Console.WriteLine( "加入援护事件" );
                break;
        }

    }
    /*
    [HarmonyPostfix, HarmonyPatch(typeof(CounterProcessStrategy), nameof(CounterProcessStrategy.Process), new Type[] { typeof(BattleEventArgs) })]
    public static void postCounter(BattleEventArgs arg)
    {
        newAttackType = AttackType.Null;
        Console.WriteLine("弹出反击");
    }
    */
    [HarmonyPostfix, HarmonyPatch( typeof( BattleComputer ),
                                   nameof( BattleComputer.Calculate_Final_Damage ) )]
    public static void calcDam( BattleComputer __instance, Damage damage, SkillData skill )
    {

        //如果是估计伤害则不计入消息
        if( ispred ) {
            ispred = false;
            return;
        }
        AttackType atp = newAttackType;
        //Console.WriteLine( "进入伤害计算的补丁" );
        if( BattleGlobalVariable.CurrentDamage == null ) {
            //Console.WriteLine( "没有实际攻击事件，提前退出伤害计算的补丁" );
            return;
        }
        //AttackType atp = AttackTypeStack.Pop();
        //Console.WriteLine("弹出最近加入的攻击行为");


        if( atp == AttackType.Summon ) {
            // 不知道是干嘛的，还没玩第三年
            return;
        }
        //尽量语句通顺吧，代码过于混乱- -
        string details = "影响伤害的因素: 初始伤害=" + damage.basic_damage + ", 技能系数=" +
                         damage.skill_coefficient.ToString( "0.00" ) + ", ";
        string description = "";
        string str = damage.Attacker.FullName + "对 " + damage.Defender.FullName +
                     ( skill.Item.Name != null ? " 使用" : " 攻击 " );

        description += skill.Item.Name;

        if( atp == AttackType.Batter ) {
            str = "连击! " + str;
        }
        if( damage.direction == DamageDirection.Back ) {
            description += " 背刺";
            details += "背刺系数: " + __instance.Coefficient_Of_Direction( damage );
        }

        if( damage.direction == DamageDirection.Side ) {
            description += " 侧袭";
            details += "背刺系数: " + __instance.Coefficient_Of_Direction( damage );
        }
        if( damage.IsDodge ) {
            msgs.Add( "•" + str + skill.Item.Name + " 被闪避" );
            HookBattleMemo.description.Enqueue( "你在找啥？被闪避了给出伤害系数也没意义..." );
            update_msg();
            return;
        }
        if( !damage.IsHit ) {
            description += " 失手";
            details += " 失手系数=0.7";
        }

        if( damage.IsParry ) {
            description += " 被招架";
            details += " 招架系数=" + ( ( 1f - __instance.ChangeValueToCoefficient(
                                                  damage.Defender[BattleProperty.Parry_Damage_Sub] ) ) / 3 ).ToString( "0.00" ) + ", ";
        } else if( damage.IsCritical ) {
            description += " 暴击";
            details += " 暴击系数=" + ( damage.IsCritical ? __instance.Coefficient_Of_Critical() :
                                            1f ).ToString( "0.00" ) + ", ";
        }

        if( atp == AttackType.Counter ) {
            str =  damage.Attacker.FullName + ( skill.Item.Name != null ? "使用 " : "" ) + skill.Item.Name +
                   " 反击 " + damage.Defender.FullName;
            description = "";

        }
        if( atp == AttackType.Support ) {
            str = damage.Attacker.FullName + ( skill.Item.Name != null ? "使用 " : "" ) + skill.Item.Name +
                  " 援护反击 " + damage.Defender.FullName;
            description = "";

        }
        if( atp == AttackType.Preemptive ) {
            str = damage.Attacker.FullName + ( skill.Item.Name != null ? "使用 " : "" ) + skill.Item.Name +
                  " 先制反击 " + damage.Defender.FullName;
            description = "";

        }
        if( atp == AttackType.Persuit ) {
            str = damage.Attacker.FullName + ( skill.Item.Name != null ? "使用 " : "" ) + skill.Item.Name +
                  " 协助追击 " + damage.Defender.FullName;
            description = "";
        }
        if( damage.IsProtect ) {
            str = damage.Defender.FullName + " 守护队友! " + str + skill.Item.Name;
            description = "";
        }

        if( damage.IsSuperiority ) {
            description += " 效果拔群! ";
            details += " 属性相克=" + ( damage.IsSuperiority ? __instance.Coefficient_Of_Element() :
                                            1f ).ToString( "0.00" ) + ", ";
        }
        //if( damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] > 0 ) {
        //     description += damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] + "%" + " 被真气抵挡";
        //}


        msgs.Add( "•" + str + description + "造成" + damage.final_damage + "伤害" );


        float coe = ( 1 + ( float )damage.Attacker[BattleProperty.Attack_Damage_Add] / 100 ) * ( 1 -
                    ( float )damage.Attacker[BattleProperty.Attack_Damage_Sub] / 100 ) * ( 1 +
                            ( float )damage.Defender[BattleProperty.Defend_Damage_Add] / 100 ) * ( 1 -
                                    ( float )damage.Defender[BattleProperty.Defend_Damage_Sub] / 100 );
        details += "伤害增减系数=" + coe.ToString( "0.00" );
        String str2 = "";

        //移到补充说明
        str2 = "* 增减伤公式: (1+加攻) × (1-降攻) × (1+增伤) × (1-减伤) ";
        //msgs.Add( str2 );
        details += "\n\n\n" + str2;
        String str4 = "* 增减伤 = (1+(" + damage.Attacker[BattleProperty.Attack_Damage_Add].ToString() +
                      "%)) × " + " (1-(" + damage.Attacker[BattleProperty.Attack_Damage_Sub].ToString() + "%)) × (1+("
                      + ( damage.Defender[BattleProperty.Defend_Damage_Add] ).ToString() + "%)) × (1-(" +
                      ( damage.Defender[BattleProperty.Defend_Damage_Sub] ).ToString() + "%)) =" + coe.ToString( "0.00" );

        //msgs.Add( str4 );
        details += "\n\n" + str4;
        if( damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] > 0 ) {
            details += "\n\n内力抵消伤害" + __instance.ChangeValueToCoefficient(
                           damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] ).ToString( "0.00" );
        }

        String str3 = "";
        if( damage.Attacker[BattleProperty.Damage_HP_Recover] > 0 ) {
            str3 += " 吸取 " + damage.final_damage * damage.Attacker[BattleProperty.Damage_HP_Recover] / 100 +
                    " 气血";
        }
        if( damage.Attacker[BattleProperty.Damage_MP_Recover] > 0 ) {
            str3 += " 吸取 " + damage.final_damage * damage.Attacker[BattleProperty.Damage_MP_Recover] / 100 +
                    " 内力";
        }
        if( damage.Defender[BattleProperty.DamageBack] > 0 ) {
            str3 += " 受到" + damage.final_damage * damage.Defender[BattleProperty.DamageBack] / 100 +
                    " 反弹伤害";
        }
        //移到补充说明
        if( !str3.Equals( "" ) ) {
            //msgs.Add(  damage.Attacker.FullName + str3 );
            details += "\n\n" + str3;
        }

        HookBattleMemo.description.Enqueue( details );
        update_msg();
    }

    //手写个UI,感觉组件都是现成的

    // ...
    // ...
    // ...

    //然后就被Unity教育了.
    //官方的文档几乎全部都是基于UI的，连构造器都没有，根本没法用
    //不想画滚动条了，能用就行，反正滚动条也基本是个摆设

    //突然发现C#居然都是直接传递引用的，全靠系统控制，没指针怎么玩？
    [HarmonyPostfix, HarmonyPatch( typeof( UIBattle ), nameof( UIBattle.Initialize ) )]
    public static void AddMessageBoard( UIBattle __instance, BattleStateMachine fsm )
    {
        //初始化（清理）
        msgs.Clear();
        repeated = 1;
        turn = 0;
        ispred = false;
        curContent = null;
        //AttackTypeStack.Clear();

        //Console.WriteLine( "执行中" );
        //创建新对象
        GameObject MsgScroll = new GameObject( "MsgScroll" );
        //GameObject ScrollCell = new GameObject("ScrollCell");
        GameObject Content = new GameObject( "Content" );
        //给内容块上个Layout
        //装上Layout后自动加rectTransform，不能再手动上了
        VerticalLayoutGroup vLayout = Content.AddComponent<VerticalLayoutGroup>();
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlWidth = false;
        vLayout.childControlWidth = false;


        //加个fitter，随内容扩大
        ContentSizeFitter fitter = Content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        curContent = Content;






        //先禁用再启用，否则脚本似乎不执行
        MsgScroll.SetActive( false );
        Content.SetActive( false );

        //找到UI右上角的箭头位置
        WGBtn arrow =
            __instance.gameObject.transform.Find( "RoundCanvas/Round/Arrow" ).gameObject.GetComponent<WGBtn>();
        curArrow = arrow.gameObject;
        //自己加个小一点的按钮，懒得研究其结构了直接复制
        GameObject small_button = GameObject.Instantiate( arrow.gameObject );

        small_button.AddComponent<AudioSource>().clip =
            Heluo.Game.Resource.Load<AudioClip>( "assets/audio/sound/ui/uise_select01.wav" );
        small_button.GetComponent<AudioSource>().playOnAwake = false;
        //把下面那一坨东西扔了
        GameObject.Destroy( small_button.transform.GetChild( 0 ).gameObject );

        //图标也扔了
        //把图片禁用了按钮也没用了？？？WTF Unity 就不能直接用rectTransform的边界吗？
        //弄了一晚上，最后只好把图片整成透明的 - -!
        small_button.gameObject.GetComponent<Image>().color = new Color( 0, 0, 0, 0 );



        /* 用图片测试一下位置
        Texture2D tempTex3 = new Texture2D(2, 2);
        tempTex3.LoadImage(File.ReadAllBytes("D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\UISprite.png"));
        Sprite sprite3 = Sprite.Create(tempTex3, new Rect(0f, 0f, (float)tempTex3.width, (float)tempTex3.height), new Vector2(0.5f, 0.5f));
        small_button.GetComponent<Image>().sprite = sprite3;
        */


        //调整一下大小
        RectTransform sb = small_button.GetComponent<RectTransform>();
        //Console.WriteLine( "flag1" );
        sb.SetParent( arrow.gameObject.GetComponent<RectTransform>() );
        sb.pivot = new Vector2( 0.5f, 0.5f );
        sb.localPosition = new Vector3( 0f, 5f, 0f );
        sb.sizeDelta = new Vector2( 50f, 100f );
        WGBtn Small_button = small_button.GetComponent<WGBtn>();


        small_button.SetActive( true );
        //Console.WriteLine( "flag2,,," + ( Small_button.IsActive() ? "true" : "false" ) );

        /* 测试用
        Console.WriteLine("debug: " );
        Console.WriteLine("anchormin"  + arrow.gameObject.GetComponent<RectTransform>().anchorMin);
        Console.WriteLine("anchormax" + arrow.gameObject.GetComponent<RectTransform>().anchorMax);
        Console.WriteLine("pivot" + arrow.gameObject.GetComponent<RectTransform>().pivot);
        Console.WriteLine("localposition" + arrow.gameObject.GetComponent<RectTransform>().localPosition);
        Console.WriteLine("anchoredPosition" + arrow.gameObject.GetComponent<RectTransform>().anchoredPosition);
        Console.WriteLine("sizeDelta" + arrow.gameObject.GetComponent<RectTransform>().sizeDelta);
        */

        //Console.WriteLine( "flag3" );



        //Console.WriteLine( "flag4" );

        /* 内容块的图片(测试用)
        Image im = Content.AddComponent<Image>();
        Texture2D tempTex = new Texture2D(2, 2);
        tempTex.LoadImage(File.ReadAllBytes("D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\Note_Form.png"));
        Sprite sprite = Sprite.Create(tempTex, new Rect(0f, 0f, (float)tempTex.width, (float)tempTex.height), new Vector2(0.5f, 0.5f));
        im.sprite = sprite;
        */
        //Console.WriteLine( "flag5" );
        //给对象装上脚本
        UILoopVerticalScrollRect msgScroll = MsgScroll.AddComponent<UILoopVerticalScrollRect>();
        //随便初始化一下，能用就行
        msgScroll.movementType = UILoopScrollRect.MovementType.Clamped;
        msgScroll.scrollSensitivity = scroll_sensitivity.Value;
        msgScroll.content = Content.GetComponent<RectTransform>();
        msgScroll.vertical = true;
        msgScroll.horizontal = false;

        //Console.WriteLine( "flag6" );

        //注册按钮
        void ToggleMsgScroll( BaseEventData ev ) {
            small_button.GetComponent<AudioSource>().Play();
            if( msgScroll.IsActive() ) {
                MsgScroll.SetActive( false );
                Content.SetActive( false );
                curDetails.SetActive( false );
                arrow.gameObject.transform.GetChild( 0 ).gameObject.SetActive( true );

            } else {
                MsgScroll.SetActive( true );
                Content.SetActive( true );
                //关闭战斗目标说明
                arrow.gameObject.transform.GetChild( 0 ).gameObject.SetActive( false );
            }
            Canvas.ForceUpdateCanvases();
        }
        Small_button.PointerClick.AddListener( new UnityAction<BaseEventData>( ToggleMsgScroll ) );

        //Console.WriteLine( "flag7" );

        //滚动框随便初始化一下
        RectTransform rect2 = MsgScroll.GetComponent<RectTransform>();
        rect2.SetParent( arrow.gameObject.GetComponent<RectTransform>() );
        rect2.pivot = new Vector2( 0.65f, 1f );
        rect2.sizeDelta = new Vector2( 800f, 600f + ( height.Value - 5 ) * 60 );
        rect2.localPosition = new Vector3( 0f, -20f, 0f );

        //加个补充说明栏
        GameObject details = new GameObject( "BlockDetails", typeof( RectTransform ), typeof( Text ) );
        curDetails = details;
        details.GetComponent<RectTransform>().SetParent( arrow.gameObject.GetComponent<RectTransform>() );
        details.GetComponent<RectTransform>().pivot = new Vector2( 1f, 1f );
        details.GetComponent<RectTransform>().sizeDelta = new Vector2( 300f, 0f );
        details.GetComponent<RectTransform>().localPosition = new Vector3( -400f, -20f, 0f );
        details.GetComponent<Text>().color = Color.white;
        details.GetComponent<Text>().font = Heluo.Game.Resource.Load<Font>( "Assets/Font/kaiu.ttf" );
        details.GetComponent<Text>().fontSize = 20;
        details.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        details.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
        details.SetActive( false );

        Image im2 = MsgScroll.AddComponent<Image>();
        Texture2D tempTex2 = new Texture2D( 2, 2 );
        tempTex2.LoadImage(
            File.ReadAllBytes( "D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\Target_03.png" ) );
        Sprite sprite2 = Sprite.Create( tempTex2, new Rect( 0f, 0f, ( float )tempTex2.width,
                                        ( float )tempTex2.height ), new Vector2( 0.5f, 0.5f ) );
        im2.sprite = sprite2;

        //应用插件设置
        if( getpath() != null ) {
            im2.sprite = Heluo.Game.Resource.Load<Sprite>( getpath() );
        } else {
            im2.sprite = null;
        }

        //把内容块设为滚动框的child
        RectTransform rect = Content.GetComponent<RectTransform>();
        rect.SetParent( rect2 );
        rect.pivot = new Vector2( 0.65f, 1f );
        rect.sizeDelta = new Vector2( 750f, 1000f );
        rect.localPosition = new Vector3( 0f, -100f, 0f );



        //给滚动框加个挡板
        Mask mask = MsgScroll.AddComponent<Mask>();




        //Text

        //MsgScroll.SetActive(true);
        //Content.SetActive(true);
        //Console.WriteLine( "执行完毕" );



    }
}
}




