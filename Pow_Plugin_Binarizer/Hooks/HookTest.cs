using BepInEx;
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

namespace PathOfWuxia
{
    [DisplayName("TestName")]
    [Description("TestDescription")]
    public class HookTest : IHook
    {
        static ConfigEntry<int> board_style;
        static ConfigEntry<int> scroll_sensitivity;
        static int last_style = 0;
        static int repeated = 1;
        static int turn = 0;
        static bool ispred = false;
        static int HpBeforeDmg;
        static int MpBeforeDmg;

        static List<String> msgs = new List<String>();
        //static int msgCount = 0;
        const int maxCount = 5000;
        static GameObject curContent = new GameObject();

        public void OnRegister(PluginBinarizer plugin)
        {
            Console.WriteLine("加载test");

            board_style = plugin.Config.Bind("界面改进", "战斗面板样式", 0, new ConfigDescription("扣了几张图当背景", new AcceptableValueRange<int>(0, 5)));
            scroll_sensitivity = plugin.Config.Bind("界面改进", "滚轮灵敏度", 1, new ConfigDescription("滚轮灵敏度", new AcceptableValueRange<int>(1, 30)));
            plugin.onUpdate += OnUpdate;
            Console.WriteLine("加载test结束");
        }

        public void OnUpdate()
        {
            if (board_style.Value != last_style)
            {

                last_style = board_style.Value;
                if (curContent && curContent.gameObject && curContent.gameObject.activeInHierarchy)
                {

                    if (getpath() != null)
                    {
                        curContent.transform.parent.gameObject.GetComponent<Image>().sprite = Heluo.Game.Resource.Load<Sprite>(getpath());
                    }
                    else
                    {
                        curContent.transform.parent.gameObject.GetComponent<Image>().sprite = Heluo.Game.Resource.Load<Sprite>("assets/image/ui/uiending/01/0100.png");
                    }
                }


                for (int i = 0; i < curContent.GetComponent<RectTransform>().childCount; i++)
                {
                    curContent.GetComponent<RectTransform>().GetChild(i).GetComponent<Text>().color = (board_style.Value == 3 || board_style.Value > 4 || board_style.Value < 0 ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1));
                }
            }
        }
        static string getpath()
        {
            switch (board_style.Value)
            {
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
                default:
                    return null;
            }
        }
        /* obsolete
         * static String TypeToMsg(BillboardArg arg)
        {
            int num = arg.Numb;
            switch (arg.MessageType)
            {
                case MessageType.Crit:
                case MessageType.CritNumber:
                case MessageType.CounterAttack:
                case MessageType.Parry:
                case MessageType.Dodge:
                case MessageType.Dizzi:
                case MessageType.Weak:
                case MessageType.Pursuit:
                case MessageType.Support:
                case MessageType.Invincibility:
                case MessageType.Restrict:
                case MessageType.Miss:
                case MessageType.Batter:
                case MessageType.Protect:
                case MessageType.Preemptive:
                case MessageType.Continuous:
                case MessageType.Encourage:
                case MessageType.Dyspnea:
                case MessageType.Unbreakable:
                case MessageType.ClearMind:
                case MessageType.Guts:
                case MessageType.Phantom:
                    return null;
                case MessageType.Normal:
                    return "攻击" + num;
                case MessageType.Heal:
                    return "治疗" + num;
                case MessageType.Poison:
                case MessageType.Therapy:
                default:
                    return "测试消息" + num;

            }

        }
        */
        static public void update_msg()
        {
            Console.WriteLine("同步消息开始.");
            if (!curContent)
            {
                Console.WriteLine("没找到对象");
                return;
            }


            while (curContent.GetComponent<RectTransform>().childCount < msgs.Count)
            {
                Console.WriteLine("childCount" + curContent.GetComponent<RectTransform>().childCount + "    msgs.Count" + msgs.Count);
                if (curContent.GetComponent<RectTransform>().childCount >1 && msgs[curContent.GetComponent<RectTransform>().childCount].Equals(msgs[curContent.GetComponent<RectTransform>().childCount-1]))
                {
                    
                    repeated++;
                    if(curContent.GetComponent<RectTransform>().GetChild(curContent.GetComponent<RectTransform>().childCount - 1))
                    {
                        Console.WriteLine("object exists"+ curContent.GetComponent<RectTransform>().GetChild(curContent.GetComponent<RectTransform>().childCount - 1).name);
                    }
                    Text ttt = curContent.GetComponent<RectTransform>().GetChild(curContent.GetComponent<RectTransform>().childCount-1).gameObject.GetComponent<Text>();
                    Console.WriteLine("flaggg text length"+ ttt.text.Length);
                    ttt.text = ttt.text.Remove(ttt.text.Length-3);
                    Console.WriteLine("flagaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    ttt.text += "×" + repeated;
                    Console.WriteLine(ttt.text);
                    msgs.RemoveAt(curContent.GetComponent<RectTransform>().childCount);
                    
                }
                else
                {
                    repeated = 1;
                }


                GameObject curBlock = new GameObject("msgBlock", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                curBlock.GetComponent<LayoutElement>().flexibleWidth = 0f;
                curBlock.GetComponent<LayoutElement>().preferredWidth = 600f;
                curBlock.GetComponent<LayoutElement>().flexibleHeight = 0f;
                curBlock.GetComponent<LayoutElement>().preferredHeight = 40f;



                Console.WriteLine("设定文本." + msgs[curContent.GetComponent<RectTransform>().childCount]);
                curBlock.GetComponent<Text>().font = Heluo.Game.Resource.Load<Font>("Assets/Font/kaiu.ttf");
                curBlock.GetComponent<Text>().fontSize = 20;
                curBlock.GetComponent<Text>().text = msgs[curContent.GetComponent<RectTransform>().childCount]+ "      ";
                if (board_style.Value == 3)
                {
                    curBlock.GetComponent<Text>().color = new Color(0, 0, 0, 1);
                }

                /*
                Console.WriteLine("添加parent.");
                Console.WriteLine("active? " + (curContent.GetComponent<VerticalLayoutGroup>().IsActive()? "true" : "false") );
                Console.WriteLine("Font"+ curBlock.GetComponent<Text>().font.name);
                Console.WriteLine("Fontsize" + curBlock.GetComponent<Text>().fontSize);
                Console.WriteLine("colour r" + curBlock.GetComponent<Text>().color.r + "colour g"+ curBlock.GetComponent<Text>().color.g + "colour b"+curBlock.GetComponent<Text>().color.b);
                */
                curBlock.GetComponent<RectTransform>().sizeDelta = new Vector2(600f,30f);
                curBlock.GetComponent<RectTransform>().SetParent(curContent.GetComponent<RectTransform>(), false);
                Console.WriteLine("debug: ");
                Console.WriteLine("anchormin" + curBlock.GetComponent<RectTransform>().anchorMin);
                Console.WriteLine("anchormax" + curBlock.GetComponent<RectTransform>().anchorMax);
                Console.WriteLine("pivot" + curBlock.GetComponent<RectTransform>().pivot);
                Console.WriteLine("localposition" + curBlock.GetComponent<RectTransform>().localPosition);
                Console.WriteLine("anchoredPosition" + curBlock.GetComponent<RectTransform>().anchoredPosition);
                Console.WriteLine("sizeDelta" + curBlock.GetComponent<RectTransform>().sizeDelta);
                Console.WriteLine("text:  " + curBlock.GetComponent<Text>().text);
                Console.WriteLine("添加parent 结束.");
                Console.WriteLine("child count " + curContent.GetComponent<RectTransform>().childCount);
                curBlock.SetActive(true);
            }

            while (msgs.Count > maxCount)
            {
                Console.WriteLine("删除队首元素");
                GameObject.Destroy(curContent.GetComponent<RectTransform>().GetChild(0));
                curContent.GetComponent<RectTransform>().GetChild(0).SetParent(null);
                Console.WriteLine("child count " + curContent.GetComponent<RectTransform>().childCount);
                msgs.RemoveAt(0);
                Console.WriteLine("msg length " + msgs.Count);
            }
            Console.WriteLine("同步消息结束.");
            Console.WriteLine("reposition");
            Vector2 localpos = curContent.GetComponent<RectTransform>().localPosition;
            localpos.y = Math.Max(msgs.Count - 15, 0) * 40;
            curContent.GetComponent<RectTransform>().localPosition = localpos;
            Console.WriteLine("end reposition");
            LayoutRebuilder.ForceRebuildLayoutImmediate(curContent.GetComponent<RectTransform>());

        }


        /*
         * 自带的消息只跳数字没啥用
        [HarmonyPostfix,HarmonyPatch(typeof(WuxiaBattleManager), nameof(WuxiaBattleManager.SendBillboard))]
        public static void SendMessage(BillboardArg arg)
        {
            Console.WriteLine("执行通用信息传递");
            String cur = TypeToMsg(arg);

            msgs.Add(cur);
            update_msg();


        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(WuxiaBattleBuffer), nameof(WuxiaBattleBuffer.AddBuffer), new Type[] { typeof (WuxiaUnit), typeof (Heluo.Data.Buffer), typeof(BufferType) })]
        public static void addBuffer(WuxiaUnit unit, Heluo.Data.Buffer buffer, BufferType type)
        {
            Console.WriteLine("addbuffer patch");
            if(unit == null && buffer == null)
            {
                return;
            }
            if (buffer.Times == 0)
            {
                return;
            }
            string str = unit.FullName + "受到效果 " + buffer.Name + " 持续" + buffer.Times + "回合" +", 来源: ";
            Console.WriteLine(str);
            switch (type)
            {
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
                    str += "特技";
                    break;
                case BufferType.Trait:
                    str += "天赋";
                    break;
                case BufferType.None:
                default:
                    str += "无";
                    break;
            }
            msgs.Add(str);
            update_msg();
            Console.WriteLine("addbuffer patch end");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WuxiaBattleBuffer), nameof(WuxiaBattleBuffer.RemoveBuffer))]
        public static void removeBuffer(WuxiaBattleBuffer __instance, List<BufferInfo> ___BufferList, WuxiaUnit _unit, string _bufferId)
        {

            Console.WriteLine("removebuffer patch");
            if (__instance.IsExist(_unit.UnitID, _bufferId))
            {
                Console.WriteLine("addbuffer patch end");
                return;
            }
   
            BufferInfo bufferInfo = ___BufferList.Find((BufferInfo i) => i.UnitId == _unit.UnitID && i.BufferId == _bufferId);
            if (bufferInfo == null)
            {
                Console.WriteLine("addbuffer patch end");
                return;
            }
            string str = _unit.FullName + "失去效果 " + bufferInfo.Table.Name;
            msgs.Add(str);
            update_msg();
            Console.WriteLine("addbuffer patch end");
        }
        [HarmonyPostfix, HarmonyPatch(typeof(WGBattleTurn), nameof(WGBattleTurn.NextTurnAsync))]
        public static void beginTurn(bool isPlayer)
        {   if (!isPlayer)
                return;
            turn++;
            msgs.Add("己方回合开始, 当前回合"+turn);
        }
        [HarmonyPostfix, HarmonyPatch(typeof(BattleComputer), nameof(BattleComputer.Calculate_Basic_Dart_Damage))]
        public static void preddart(bool _is_random)
        {
            if (!_is_random)
            {
                ispred = true;
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(BattleComputer), nameof(BattleComputer.Calculate_Basic_Damage))]
        public static void predbasic(bool _is_random)
        {
            if (!_is_random)
            {
                ispred = true;
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(WuxiaUnit), nameof(WuxiaUnit.DamageMP))]
        public static void beforeMPdmg(WuxiaUnit __instance, int value, Heluo.Battle.MessageType type)
        {
            MpBeforeDmg = __instance[BattleProperty.MP];
        }
        [HarmonyPrefix, HarmonyPatch(typeof(WuxiaUnit), nameof(WuxiaUnit.DamageHP))]
        public static void beforedmg(WuxiaUnit __instance, int value, Heluo.Battle.MessageType type)
        {
            HpBeforeDmg = __instance[BattleProperty.HP];
        }
        [HarmonyPostfix, HarmonyPatch(typeof(WuxiaUnit), nameof(WuxiaUnit.DamageMP))]
        public static void damageMp(WuxiaUnit __instance, int value, Heluo.Battle.MessageType type)
        {
            string des;
            if (type == Heluo.Battle.MessageType.Therapy)
            {
                des = "回复";
            }
            else
            {
                des = "损失";
                value = -value;
            }
            msgs.Add(__instance.FullName + des + value +"点内力" + ",  MP:" + MpBeforeDmg + " -> " + __instance[BattleProperty.MP]);
        }
        [HarmonyPostfix, HarmonyPatch(typeof(WuxiaUnit),nameof(WuxiaUnit.DamageHP))]
        public static void damageHp(WuxiaUnit __instance,int value, Heluo.Battle.MessageType type)
        {
            string des;
            if (type == Heluo.Battle.MessageType.Heal)
            {
                des = "点治疗";
            }
            else
            {
                des = "点伤害";
                value = -value;
            }
            msgs.Add(__instance.FullName + "受到" + value + des + ",  HP:"+ HpBeforeDmg +" -> " +__instance[BattleProperty.HP]);
        }
        [HarmonyPostfix, HarmonyPatch(typeof(BattleComputer),nameof(BattleComputer.Calculate_Final_Damage))]
        public static void calcDam(BattleComputer __instance, Damage damage, SkillData skill)
        {
            if(ispred)
            {
                ispred = false;
                return;
            }
            string description="";

            string str = damage.Attacker.FullName + " 对 " + damage.Defender.FullName + " 使用 "; 

            description += skill.Item.Name;

            if (damage.direction == DamageDirection.Back)
                description += " 背刺";

            if (damage.direction == DamageDirection.Side)
                description += " 侧袭";
            if (damage.IsDodge)
            { 
                msgs.Add(str+ " 被闪避");
                return;
            }
            if (!damage.IsHit)
            {
                description += " 失手";
            }

            if (damage.IsParry)
            {
                description += " 被招架";
            }
            else if(damage.IsCritical)
            {
                description += " 暴击";
            }



            if (damage.IsProtect)
            {
                str = damage.Defender.FullName + " 守护队友. " + str;
                description = "";
            }
            if (damage.IsCounterAttack)
            {
                str = damage.Attacker.FullName + " 反击 " + damage.Defender.FullName;
                description = "";
            }
            if (damage.IsSuperiority)
            {
                description += " 效果拔群 ";
            }
            //if(damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] > 0)
            //{
            //    description += " 被真气抵挡%%" + damage.Defender[BattleProperty.HP_Change_MP_OF_HP_Ratio] + "伤害";
            //}
            if (damage.IsHeal)
            {
                str += "回复" + damage.final_damage + (skill.Item.DamageType == DamageType.Heal ? "气血" : "内力");
            }
            else
            {
                str += description;
                str += "造成" + damage.final_damage + "伤害";
            }
            msgs.Add(str);
            str = "***增伤减伤: 攻击增加×攻击减少×受伤增加×受伤减少 = " + (1 + (float)(damage.Attacker[BattleProperty.Attack_Damage_Add]) / 100) + "×" + (1 - (float)damage.Attacker[BattleProperty.Attack_Damage_Sub] / 100) + "×" + (1 + (float)damage.Attacker[BattleProperty.Defend_Damage_Add] / 100) + "×" + (1 - (float)damage.Attacker[BattleProperty.Attack_Damage_Sub] / 100);
            msgs.Add(str);
            str = damage.Attacker.FullName;
            if (damage.Attacker[BattleProperty.Damage_HP_Recover] > 0)
            {
                str += "吸取" + damage.final_damage * damage.Attacker[BattleProperty.Damage_HP_Recover]/100+"气血";
            }
            if (damage.Attacker[BattleProperty.Damage_MP_Recover] > 0)
            {
               str += "吸取" + damage.final_damage * damage.Attacker[BattleProperty.Damage_MP_Recover]/100 + "内力";
            }
            if (damage.Defender[BattleProperty.DamageBack] > 0)
            {
                str += "受到" + damage.final_damage * damage.Defender[BattleProperty.DamageBack]/100 + "反弹伤害";
            }
            msgs.Add(str);
            update_msg();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIBattle), nameof(UIBattle.Initialize))]
        public static void AddMessageBoard(UIBattle __instance, BattleStateMachine fsm)
        {
            //初始化
            msgs.Clear();
            repeated = 1;
            turn = 0;
            ispred = false;
            curContent = null;

            Console.WriteLine("执行中");
            //创建新对象
            GameObject MsgScroll = new GameObject("MsgScroll");
            //GameObject ScrollCell = new GameObject("ScrollCell");
            GameObject Content = new GameObject("Content");
            //给内容块上个Layout
            //装上Layout后自动加rectTransform，不用也不能再手动上
            VerticalLayoutGroup vLayout = Content.AddComponent<VerticalLayoutGroup>();
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlWidth = false;
            vLayout.childControlWidth = false;

            
            //加个fitter，随内容扩大
            ContentSizeFitter fitter = Content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            HookTest.curContent = Content;

            




            //先禁用再启用，否则脚本不执行
            MsgScroll.SetActive(false);
            Content.SetActive(false);
            
            //找到UI右上角的箭头位置
            WGBtn arrow = __instance.gameObject.transform.Find("RoundCanvas/Round/Arrow").gameObject.GetComponent<WGBtn>();

            //自己加个小一点的按钮，懒得研究其结构了直接复制
            GameObject small_button = GameObject.Instantiate(arrow.gameObject);

            small_button.AddComponent<AudioSource>().clip = Heluo.Game.Resource.Load<AudioClip>("assets/audio/sound/ui/uise_select01.wav");
            small_button.GetComponent<AudioSource>().playOnAwake = false;
            //把下面那一坨东西扔了
            GameObject.Destroy(small_button.transform.GetChild(0).gameObject);

            //图标也扔了
            //把图片禁用了按钮也没用了？？？WTF Unity 就不能直接用rectTransform的边界吗？
            //弄了一晚上，最后只好把图片整成透明的 - -!
            small_button.gameObject.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            


            /* 用图片测试一下位置 
            Texture2D tempTex3 = new Texture2D(2, 2);
            tempTex3.LoadImage(File.ReadAllBytes("D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\UISprite.png"));
            Sprite sprite3 = Sprite.Create(tempTex3, new Rect(0f, 0f, (float)tempTex3.width, (float)tempTex3.height), new Vector2(0.5f, 0.5f));
            small_button.GetComponent<Image>().sprite = sprite3;
            */


            //调整一下大小
            RectTransform sb = small_button.GetComponent<RectTransform>();
            Console.WriteLine("flag1");
            sb.SetParent(arrow.gameObject.GetComponent<RectTransform>());
            sb.pivot = new Vector2(0.5f, 0.5f);
            sb.localPosition = new Vector3(0f, 5f, 0f);
            sb.sizeDelta = new Vector2(50f, 100f);
            WGBtn Small_button = small_button.GetComponent<WGBtn>();

            
            small_button.SetActive(true);
            Console.WriteLine("flag2,,," + (Small_button.IsActive()? "true": "false"));

            /* 测试用
            Console.WriteLine("debug: " );
            Console.WriteLine("anchormin"  + arrow.gameObject.GetComponent<RectTransform>().anchorMin);
            Console.WriteLine("anchormax" + arrow.gameObject.GetComponent<RectTransform>().anchorMax);
            Console.WriteLine("pivot" + arrow.gameObject.GetComponent<RectTransform>().pivot);
            Console.WriteLine("localposition" + arrow.gameObject.GetComponent<RectTransform>().localPosition);
            Console.WriteLine("anchoredPosition" + arrow.gameObject.GetComponent<RectTransform>().anchoredPosition);
            Console.WriteLine("sizeDelta" + arrow.gameObject.GetComponent<RectTransform>().sizeDelta);
            */

            Console.WriteLine("flag3" );
            //把内容块的位置随便设定一下


            Console.WriteLine("flag4");

            /* 内容块的图片(测试用)
            Image im = Content.AddComponent<Image>();
            Texture2D tempTex = new Texture2D(2, 2);
            tempTex.LoadImage(File.ReadAllBytes("D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\Note_Form.png"));
            Sprite sprite = Sprite.Create(tempTex, new Rect(0f, 0f, (float)tempTex.width, (float)tempTex.height), new Vector2(0.5f, 0.5f));
            im.sprite = sprite;
            */
            Console.WriteLine("flag5");
            //给对象装上脚本
            UILoopVerticalScrollRect msgScroll = MsgScroll.AddComponent<UILoopVerticalScrollRect>();
            //随便初始化一下，能用就行
            msgScroll.scrollSensitivity = scroll_sensitivity.Value;
            msgScroll.content = Content.GetComponent<RectTransform>();
            msgScroll.vertical = true;
            msgScroll.horizontal = false;

            Console.WriteLine("flag6");
            //注册按钮
            void ToggleMsgScroll(BaseEventData ev)
            {
                small_button.GetComponent<AudioSource>().Play();
                if (msgScroll.IsActive())
                {
                    MsgScroll.SetActive(false);
                    Content.SetActive(false);
                    arrow.gameObject.transform.GetChild(0).gameObject.SetActive(true);

                }
                else
                {
                    MsgScroll.SetActive(true);
                    Content.SetActive(true);
                    //关闭战斗目标说明
                    arrow.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                }
                Canvas.ForceUpdateCanvases();
            }
            Small_button.PointerClick.AddListener(new UnityAction<BaseEventData>(ToggleMsgScroll));

            Console.WriteLine("flag7");

            //滚动框也随便初始化一下
            RectTransform rect2 = MsgScroll.GetComponent<RectTransform>();
            rect2.SetParent(arrow.gameObject.GetComponent<RectTransform>());
            rect2.pivot = new Vector2(0.65f, 1f);
            rect2.sizeDelta = new Vector2(800f, 600f);
            rect2.localPosition = new Vector3(0f, -20f, 0f);

            


            Image im2 = MsgScroll.AddComponent<Image>();
            Texture2D tempTex2 = new Texture2D(2, 2);
            tempTex2.LoadImage(File.ReadAllBytes("D:\\NewGitDirectory\\Plugin-PathOfWuxia\\Pow_Plugin_Binarizer\\resourses\\Target_03.png"));
            Sprite sprite2 = Sprite.Create(tempTex2, new Rect(0f, 0f, (float)tempTex2.width, (float)tempTex2.height), new Vector2(0.5f, 0.5f));
            im2.sprite = sprite2;
            
            //应用插件设置
            if (getpath() != null )
            {
                im2.sprite = Heluo.Game.Resource.Load<Sprite>(getpath());
            }
            else
            {
                im2.sprite = Heluo.Game.Resource.Load<Sprite>("assets/image/ui/uiending/01/0100.png");
            }

            //把内容块设为滚动框的child
            RectTransform rect = Content.GetComponent<RectTransform>();
            rect.SetParent(rect2);
            rect.pivot = new Vector2(0.65f, 1f);
            rect.sizeDelta = new Vector2(700f, 1000f);
            rect.localPosition = new Vector3(0f, -100f, 0f);
            
 

            //给滚动框加个挡板
            Mask mask = MsgScroll.AddComponent<Mask>();

            


            //Text

            //MsgScroll.SetActive(true);
            //Content.SetActive(true);
            Console.WriteLine("执行完毕");



        }


    }
  
}