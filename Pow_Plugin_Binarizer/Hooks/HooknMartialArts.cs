using System;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using Heluo;
using Heluo.UI;
using Heluo.Data;
using Heluo.Flow;
using Heluo.Utility;
using Heluo.Battle;
using Heluo.FSM;
using Heluo.FSM.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PathOfWuxia
{
    public class HooknMartialArts : IHook
    {
        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }
        public void OnRegister(BaseUnityPlugin plugin)
        {
            nonbattleUseHealSkill = plugin.Config.Bind("扩展功能", "非战斗时使用恢复技能", false, "");
            nonbattleChangeElement = plugin.Config.Bind("扩展功能", "非战斗时使用五炁朝元", false, "");
        }

        public void OnUpdate()
        {
        }

        //private static ConfigEntry<bool> nonbattle;
        private static ConfigEntry<bool> nonbattleChangeElement;
        private static ConfigEntry<bool> nonbattleUseHealSkill;


        //非战斗时使用五炁朝元
        private static CharacterInfoData characterInfoData;
        private static SkillData clickSkill;
        private static CtrlHome homeController;
        [HarmonyPostfix, HarmonyPatch(typeof(UIMartialArts), "Show")]
        public static void ShowPatch_nonbattleChangeElement(ref UIMartialArts __instance)
        {
            Heluo.Logger.LogError("ShowPatch_nonbattleChangeElement start");
            //获得特技按钮
            WGMartialArtsBtn[] martialArts = Traverse.Create(__instance).Field("martialArts").GetValue<WGMartialArtsBtn[]>();
            WGMartialArtsBtn specialButton = martialArts[5];
            Button specialButton2 = specialButton.GetComponent<Button>();
            if (specialButton2 == null)
            {
                specialButton2 = specialButton.gameObject.AddComponent<Button>();
            }
            //添加点击事件
            UIHome home = Traverse.Create(__instance).Field("home").GetValue<UIHome>();
            homeController = Traverse.Create(home).Field("controller").GetValue<CtrlHome>();
            specialButton2.onClick.AddListener(delegate () { openElementUI(); });
        }

        public static void openElementUI()
        {
            Heluo.Logger.LogError("openElementUI start");
            if (nonbattleChangeElement.Value)
            {
                //show结束时ctrlMartialArts还没当前角色数据，需要从ctrlhome处获得

                List<CharacterMapping> characterMapping = Traverse.Create(homeController).Field("characterMapping").GetValue<List<CharacterMapping>>();
                int communityIndex = Traverse.Create(homeController).Field("communityIndex").GetValue<int>();

                CharacterMapping mapping = characterMapping[communityIndex];
                characterInfoData = Game.GameData.Character[mapping.InfoId];
                clickSkill = characterInfoData.GetSkill(characterInfoData.SpecialSkill);

                //不是切换功体技能
                if (clickSkill == null || clickSkill.Item.DamageType != DamageType.ChangeElement)
                {
                    return;
                }
                //mp不足
                if (characterInfoData.MP < clickSkill.Item.RequestMP)
                {
                    string text2 = Game.Data.Get<StringTable>("SecondaryInterface1207").Text;
                    Game.UI.AddMessage(text2, UIPromptMessage.PromptType.Normal);
                    return;
                }
                Game.MusicPlayer.Current_Volume = 0.5f;

                //从uibattle处获得五行盘ui
                UIBattle uiBattle = Game.UI.Open<UIBattle>();
                WgBattleRound battle_round = uiBattle.battle_round;
                battle_round.gameObject.SetActive(false);//隐藏右上角的回合数
                UIAttributeList attr_list = Traverse.Create(uiBattle).Field("attr_list").GetValue<UIAttributeList>();

                //图层设置为最前，否则会被挡住
                Game.UI.SetParent(attr_list.transform, UIForm.Depth.Front);
                attr_list.transform.SetAsLastSibling();

                attr_list.Show();
                attr_list.SetOriginElement((int)characterInfoData.Element, new Action<int>(OnElementSelect), delegate
                {
                    Game.MusicPlayer.Current_Volume = 1f;
                });

                WGAbilityInfo abilityInfo = attr_list.abilityInfo;
                abilityInfo.gameObject.SetActive(false);//隐藏无用的buff提示
            }
        }

        //隐藏无用的buff提示
        [HarmonyPostfix, HarmonyPatch(typeof(UIAttributeList), "OnElementHover")]
        public static void OnElementHoverPatch_nonbattleChangeElement(ref UIAttributeList __instance)
        {
            Heluo.Logger.LogError("OnElementHoverPatch_nonbattleChangeElement start");

            WGAbilityInfo abilityInfo = __instance.abilityInfo;
            abilityInfo.gameObject.SetActive(false);
            Heluo.Logger.LogError("OnElementHoverPatch_nonbattleChangeElement end");
        }

        //点击五行按钮后的callback，实际切换操作在这里进行
        public static void OnElementSelect(int element)
        {
            Heluo.Logger.LogError("OnElementSelect start");
            Game.MusicPlayer.Current_Volume = 1f;
            //修改功体，扣除mp，更新界面信息
            characterInfoData.Element = (Element)element;
            characterInfoData.MP -= clickSkill.Item.RequestMP;
            homeController.UpdateBasicInfo(true);
            homeController.UpdateCharacterProperty(true);
            //水功体的回血回蓝功能要不要加上呢……如果加上不就等于白嫖了么
            Heluo.Logger.LogError("OnElementSelect end");
        }


        //非战斗时使用恢复技能
        public static UITeamMember uiTeamMember;
        public static SkillData selectSkill;
        //技能主界面的选择技能
        [HarmonyPostfix, HarmonyPatch(typeof(CtrlMartialArts), "UpdateIntroduction")]
        public static void UpdateIntroductionPatch_nonbattleUseHealSkill(ref CtrlMartialArtsWindow __instance, ref int index)
        {
            Heluo.Logger.LogError("UpdateIntroductionPatch_nonbattleUseHealSkill start");


            if (nonbattleUseHealSkill.Value)
            {
                //获得当前鼠标指向技能
                CharacterMapping mapping = Traverse.Create(__instance).Field("mapping").GetValue<CharacterMapping>();

                CharacterInfoData source = Game.GameData.Character[mapping.InfoId];
                selectSkill = source.GetSkill((SkillColumn)index);

                showUITeamMember(source, selectSkill);
            }
            Heluo.Logger.LogError("UpdateIntroductionPatch_nonbattleUseHealSkill end");
        }

        //技能选择窗口的选择技能
        [HarmonyPostfix, HarmonyPatch(typeof(CtrlMartialArtsWindow), "UpdateIntroduction")]
        public static void UpdateIntroductionPatch2_nonbattleUseHealSkill(ref CtrlMartialArtsWindow __instance,ref int index)
        {
            Heluo.Logger.LogError("UpdateIntroductionPatch2_nonbattleUseHealSkill start");


            if (nonbattleUseHealSkill.Value)
            {
                //获得当前鼠标指向技能
                CharacterMapping mapping = Traverse.Create(__instance).Field("mapping").GetValue<CharacterMapping>();

                CharacterInfoData source = Game.GameData.Character[mapping.InfoId];


                CtrlMartialArts.UISkillColumn skillColumn = Traverse.Create(__instance).Field("skillColumn").GetValue<CtrlMartialArts.UISkillColumn>();
                List<SkillData> sortSkills = Traverse.Create(__instance).Field("sortSkills").GetValue<List<SkillData>>();
                if (skillColumn == CtrlMartialArts.UISkillColumn.Mantra)
                {
                    return;
                }
                else
                {
                    if (sortSkills.Count <= 0 || index >= sortSkills.Count)
                    {
                        Debug.LogError(string.Format("MartialArts 的 Scroll 給出的Index出現問題, Index: {0}", index));
                        return;
                    }
                    selectSkill = sortSkills[index];
                }

                showUITeamMember(source,selectSkill);
            }
            Heluo.Logger.LogError("UpdateIntroductionPatch2_nonbattleUseHealSkill end");
        }

        //显示左侧队友UI
        public static void showUITeamMember(CharacterInfoData source,SkillData selectSkill)
        {

            //先销毁原UI
            if (uiTeamMember != null)
            {
                UnityEngine.Object.DestroyImmediate(uiTeamMember.gameObject);
            }

            //如果是恢复技能且伤害公式不为0
            if ((selectSkill.Item.DamageType == DamageType.Heal || selectSkill.Item.DamageType == DamageType.MpRecover) && selectSkill.Item.Damage != "nodamage" && selectSkill.Item.Damage != "0damage")
            {
                List<CharacterInfoData> list = new List<CharacterInfoData>();
                //暂时不做养成界面的非队友治疗，角色太多左边UI放不下，界面更新也麻烦
                //养成界面队伍中只有自己，但应该可以互相治疗
                /* if (Game.GameData.Round.CurrentStage == Heluo.Manager.TimeStage.Free)
                 {
                     foreach (KeyValuePair<string,CommunityData> community in Game.GameData.Community)
                     {
                         CharacterInfoData target = Game.GameData.Character[community.Key];
                         list.Add(target);
                     }
                 }
                 //大地图的话只有队伍中能互相治疗
                 else
                 {*/
                //获得当前队友
                foreach (string text in Game.GameData.Party)
                {
                    CharacterInfoData target = Game.GameData.Character[text];
                    list.Add(target);
                }
                //}

                //如果当前角色不在队伍中则不能用恢复技能，防止远程治疗
                if (!list.Contains(source))
                {
                    return;
                }


                //展示左侧的队友列表UI
                uiTeamMember = Game.UI.Open<UITeamMember>();

                //给每个队友的头像加上按钮和点击事件
                List<UITeamMemberInfo> infos = Traverse.Create(uiTeamMember).Field("infos").GetValue<List<UITeamMemberInfo>>();
                for (int i = 0; i < infos.Count; i++)
                {
                    GameObject buttonGO = new GameObject("buttonGO");
                    buttonGO.transform.SetParent(infos[i].Protrait.transform, false);
                    Button button = buttonGO.AddComponent<Button>();

                    //给按钮随便添加一个透明的图，否则没法点击
                    Image image = buttonGO.AddComponent<Image>();
                    image.sprite = Game.Resource.Load<Sprite>("Image/UI/UIAlchemy/alchemy_stove.png");
                    image.color = new Color(255, 255, 255, 0);

                    int partyIndex = i;//临时变量存储，否则下一步addListener传不过去

                    button.onClick.AddListener(delegate {
                        nonbattleUseHealSkillAction(uiTeamMember, source, list, partyIndex, selectSkill);
                    });

                }
            }
        }

        //关闭技能选择页面时销毁左侧队友UI
        [HarmonyPostfix, HarmonyPatch(typeof(UIMartialArtsWindow), "Close")]
        public static void ClosePatch_nonbattleUseHealSkill(ref UIMartialArtsWindow __instance)
        {
            Heluo.Logger.LogError("ClosePatch_nonbattleUseHealSkill start");
            UnityEngine.Object.Destroy(uiTeamMember.gameObject);
        }

        public static void nonbattleUseHealSkillAction(UITeamMember uiTeamMember, CharacterInfoData attacker, List<CharacterInfoData> defender, int index,SkillData skill)
        {
            Heluo.Logger.LogError("nonbattleUseHealSkillAction start");

            //mp不足
            if (attacker.MP < skill.Item.RequestMP)
            {
                string text2 = Game.Data.Get<StringTable>("SecondaryInterface1207").Text;
                Game.UI.AddMessage(text2, UIPromptMessage.PromptType.Normal);
                return;
            }

            //最小距离大于0则不能给自己治疗
            if (selectSkill.Item.MinRange > 0 && defender[index] == attacker)
            {
                Game.UI.AddMessage("该技能不能给自己治疗", UIPromptMessage.PromptType.Normal);
                return;
            }

            //最大距离等于0则只能给自己治疗
            if (selectSkill.Item.MaxRange == 0 && defender[index] != attacker)
            {
                Game.UI.AddMessage("该技能只能给自己治疗", UIPromptMessage.PromptType.Normal);
                return;
            }


            //是否群体回复
            int startIndex = index;
            int endIndex = index+1;
            if (skill.Item.TargetArea == TargetArea.Fan || skill.Item.TargetArea == TargetArea.LineGroup || skill.Item.TargetArea == TargetArea.RingGroup)
            {
                startIndex = 0;
                endIndex = defender.Count;
            }

            //所有加血对象是否已满血
            bool isAllFullHp = true;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (defender[i].HP < defender[i].Property[CharacterProperty.Max_HP].Value)
                {
                    isAllFullHp = false;
                    break;
                }
            }
            if (isAllFullHp)
            {
                Game.UI.AddMessage("HP已满", UIPromptMessage.PromptType.Normal);
                return;
            }
            
            BattleComputer battleComputer = Singleton<BattleComputer>.Instance;
            BattleComputerFormula BattleComputerFormula = Traverse.Create(battleComputer).Field("BattleComputerFormula").GetValue<BattleComputerFormula>();
            BattleFormulaProperty BattleComputerProperty = Traverse.Create(battleComputer).Field("BattleComputerProperty").GetValue<BattleFormulaProperty>();

            attacker.MP -= skill.Item.RequestMP;

            for (int i = startIndex; i < endIndex; i++)
            {
                //原代码用WuxiaUnit参与计算，涉及Grid格子数据，直接调用比较麻烦，所以我这边就把逻辑抄一遍
                //只留下和回复技能有关的数据(删来删去发现好像只剩下暴击了)，如果有遗漏以后再补（真的会有人发现么）
                battleComputer.Initialize();

                AttachBattleComputerProperty(attacker, defender[i]);//记录攻击者和防御者的属性

                float basic_damage = battleComputer.Calculate_Basic_Damage(true);//基础回复值


                float num = BattleComputerFormula["critical_rate"].Evaluate(BattleComputerProperty.GetDictionary());
                num = Mathf.Clamp(num, 0f, 100f);
                int probability = UnityEngine.Random.Range(1, 100); ;
                bool IsCritical = probability <= num;//是否暴击


                float skill_coefficient = battleComputer.Calculate_Skill_Coefficient(skill);//技能倍率

                float num2 = IsCritical ? BattleComputerFormula["damage_coefficient_of_critical"].Evaluate(BattleComputerProperty.GetDictionary()) : 1f;

                float num7 = basic_damage * num2;

                int final_damage = 1;
                //实际上回血技能应该都是+倍率
                if (skill.Item.Algorithm == Algorithm.Addition)
                {
                    final_damage = (int)(num7 + skill_coefficient);
                }
                else
                {
                    final_damage = (int)(num7 * skill_coefficient);
                }

                if (selectSkill.Item.DamageType == DamageType.Heal)
                {
                    defender[i].HP += final_damage;
                }
                else if(selectSkill.Item.DamageType == DamageType.MpRecover)
                {
                    defender[i].MP += final_damage;
                }
                 
            }


            //刷新左侧ui
            CtrlTeamMember controller = Traverse.Create(uiTeamMember).Field("controller").GetValue<CtrlTeamMember>();
            controller.OnShow();

            //刷新主界面角色信息
            homeController.UpdateCharacterProperty(true);

            Heluo.Logger.LogError("nonbattleUseHealSkillAction end");
        }

        //记录攻击者和防御者的属性
        public static void AttachBattleComputerProperty(CharacterInfoData attacker, CharacterInfoData defender)
        {
            Heluo.Logger.LogError("AttachBattleComputerProperty start");
            BattleComputer battleComputer = Singleton<BattleComputer>.Instance;
            BattleFormulaProperty BattleComputerProperty = Traverse.Create(battleComputer).Field("BattleComputerProperty").GetValue<BattleFormulaProperty>();
            BattleComputerFormula BattleComputerFormula = Traverse.Create(battleComputer).Field("BattleComputerFormula").GetValue<BattleComputerFormula>();

            BattleComputerProperty.Clear();
            foreach (object obj in Enum.GetValues(typeof(NurturanceProperty)))
            {
                NurturanceProperty prop = (NurturanceProperty)obj;
                string key = string.Format("attacker_{0}", prop.ToString().ToLower());
                int value = attacker.GetUpgradeableProperty((CharacterUpgradableProperty)obj);
                string key2 = string.Format("defender_{0}", prop.ToString().ToLower());
                int value2 = defender.GetUpgradeableProperty((CharacterUpgradableProperty)obj);
                BattleComputerProperty[key] = value;
                BattleComputerProperty[key2] = value2;
                BattleComputerProperty[prop.ToString().ToLower()] = value;
            }
            foreach (object obj2 in Enum.GetValues(typeof(BattleProperty)))
            {
                BattleProperty battleProperty = (BattleProperty)obj2;
                string format = "attacker_{0}";
                BattleProperty battleProperty2 = battleProperty;
                string key3 = string.Format(format, battleProperty2.ToString().ToLower());
                int value3 = attacker.Property[(CharacterProperty)obj2].Value;
                string format2 = "defender_{0}";
                battleProperty2 = battleProperty;
                string key4 = string.Format(format2, battleProperty2.ToString().ToLower());
                int value4 = defender.Property[(CharacterProperty)obj2].Value;
                BattleComputerProperty[key3] = value3;
                BattleComputerProperty[key4] = value4;
                if (battleProperty == BattleProperty.Move)
                {
                    break;
                }
            }

            BattleComputerProperty["attacker_element"] = (int)attacker.Element;
            BattleComputerProperty["defender_element"] = (int)defender.Element;

            BattleComputerProperty["defender_max_hp"] = defender.Property[CharacterProperty.Max_HP].Value;
            float num = BattleComputerFormula["basic_attack_of_counter"].Evaluate(BattleComputerProperty.GetDictionary());
            BattleComputerProperty.Add("basic_attack_of_counter", (int)num);

            Heluo.Logger.LogError("AttachBattleComputerProperty end");
        }

    }
}
