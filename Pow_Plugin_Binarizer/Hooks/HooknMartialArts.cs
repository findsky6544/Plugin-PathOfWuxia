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
using UnityEngine.UI;

namespace PathOfWuxia
{
    // 战斗中获得招式经验、招式等级自定义
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
        private static SkillData skill;
        private static CtrlHome controller;
        [HarmonyPostfix, HarmonyPatch(typeof(UIMartialArts), "Show")]
        public static void ShowPatch_nonbattleChangeElement(ref UIMartialArts __instance)
        {
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
            specialButton2.onClick.AddListener(delegate() { openElementUI(home); });
        }
        
        public static void openElementUI(UIHome home)
        {
            if (nonbattleChangeElement.Value)
            {
                //show结束时ctrlMartialArts还没当前角色数据，需要从ctrlhome处获得
                controller = Traverse.Create(home).Field("controller").GetValue<CtrlHome>();

                List<CharacterMapping> characterMapping = Traverse.Create(controller).Field("characterMapping").GetValue<List<CharacterMapping>>();
                int communityIndex = Traverse.Create(controller).Field("communityIndex").GetValue<int>();

                Heluo.Logger.LogError("communityIndex是多少：" + communityIndex);
                CharacterMapping mapping = characterMapping[communityIndex];
                Heluo.Logger.LogError("mapping.InfoId是多少：" + mapping.InfoId);
                characterInfoData = Game.GameData.Character[mapping.InfoId];
                skill = characterInfoData.GetSkill(characterInfoData.SpecialSkill);

                //不是切换功体技能
                if (skill == null || skill.Item.DamageType != DamageType.ChangeElement)
                {
                    return;
                }
                //mp不足
                if (characterInfoData.MP < skill.Item.RequestMP)
                {
                    string text2 = Game.Data.Get<StringTable>("SecondaryInterface1207").Text;
                    Game.UI.AddMessage(text2, UIPromptMessage.PromptType.Special);
                    return;
                }
                Game.MusicPlayer.Current_Volume = 0.5f;

                //从uibattle处获得五行盘ui
                UIBattle uiBattle= Game.UI.Open<UIBattle>();
                UIAttributeList attr_list = Traverse.Create(uiBattle).Field("attr_list").GetValue<UIAttributeList>();

                //图层设置为最前，否则会被挡住
                Game.UI.SetParent(attr_list.transform, UIForm.Depth.Front);
                attr_list.transform.SetAsLastSibling();

                attr_list.Show();
                attr_list.SetOriginElement((int)characterInfoData.Element, new Action<int>(OnElementSelect), delegate
                {
                    Game.MusicPlayer.Current_Volume = 1f;
                });

            }
        }

        //点击五行按钮后的callback，实际切换操作在这里进行
        public static void OnElementSelect(int element)
        {
            Game.MusicPlayer.Current_Volume = 1f;
            //修改功体，扣除mp，更新界面信息
            characterInfoData.Element = (Element)element;
            characterInfoData.MP -= skill.Item.RequestMP;
            controller.UpdateBasicInfo(true);
            controller.UpdateCharacterProperty(true);
            //水功体的回血回蓝功能要不要加上呢……如果加上不就等于白嫖了么
        }
    }
}
