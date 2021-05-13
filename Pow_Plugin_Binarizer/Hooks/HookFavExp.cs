using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Heluo;
using Heluo.UI;
using Heluo.Data;
using UnityEngine.UI;

namespace PathOfWuxia
{
    class HookFavExp : IHook
    {
        private static ConfigEntry<bool> showFavExp;

        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }

        public void OnRegister(BaseUnityPlugin plugin)
        {
            showFavExp = plugin.Config.Bind("游戏设定", "显示好友好感度与礼物好感度", false, "在好友界面显示当前好感度/总需好感度，在送礼界面显示礼物可提高的好感度");
        }

        public void OnUpdate()
        {
            
        }


        //显示好友好感度
        [HarmonyPostfix, HarmonyPatch(typeof(UIRelationship), "UpdateRelationship")]
        public static void UpdateRelationshipPatch_showFavExp(ref UIRelationship __instance)
        {
            Text expText;
            Slider expbar = Traverse.Create(__instance).Field("expbar").GetValue<Slider>();
            var trans = expbar.transform.Find("expText");
            if (trans == null)
            {
                    GameObject gameObject = new GameObject("expText");
                    gameObject.transform.SetParent(expbar.transform, false);
                    expText = gameObject.AddComponent<Text>();
                    expText.font = Game.Resource.Load<Font>("Assets/Font/kaiu.ttf");
                    expText.fontSize = 25;
                    expText.alignment = TextAnchor.MiddleLeft;
                    expText.rectTransform.sizeDelta = new Vector2(120f, 40f);
                    expText.transform.localPosition = new Vector3(-5f, 50f, 0f);
            }
            else
            {
                expText = trans.gameObject.GetComponent<Text>();
            }
            string currentId = Traverse.Create(__instance).Field("currentId").GetValue<string>();
            FavorabilityData favorability = Game.GameData.Community[currentId].Favorability;
            expText.text = favorability.Exp + " / " + favorability.GetMaxExpByLevel(favorability.Level);
            expText.gameObject.SetActive(showFavExp.Value);
        }

        //显示礼物好感度
        [HarmonyPostfix, HarmonyPatch(typeof(CtrlFormInventory), "UpdateIntroduction")]
        public static void UpdateIntroductionPatch_showFavExp(ref CtrlFormInventory __instance)
        {
            List < PropsInfo > sort = Traverse.Create(__instance).Field("sort").GetValue<List<PropsInfo>>();
            int propsIndex = Traverse.Create(__instance).Field("propsIndex").GetValue<int>();
            CharacterMapping mapping = Traverse.Create(__instance).Field("mapping").GetValue<CharacterMapping>();


            Props item = sort[propsIndex].Item;

            if (showFavExp.Value && item.PropsEffect != null)
            {
                for (int i = 0; i < item.PropsEffect.Count; i++)
                {
                    PropsEffect propsEffect = item.PropsEffect[i];
                    if (propsEffect is PropsFavorable)
                    {
                        PropsFavorable propsFavorable = propsEffect as PropsFavorable;
                        if (mapping != null && mapping.InfoId != null && !mapping.InfoId.Equals(string.Empty) && propsFavorable.Npcid == mapping.InfoId)
                        {
                            string effectDescriptionStr = "";
                            if(item.PropsEffectDescription != null && !item.PropsEffectDescription.Equals(string.Empty))
                            {
                                effectDescriptionStr = item.PropsEffectDescription + ",";
                            }
                            effectDescriptionStr += "好感+" + propsFavorable.Value;

                            UIFormInventory view = Traverse.Create(__instance).Field("view").GetValue<UIFormInventory>();
                            WGPropsIntroduction propsIntroduction = Traverse.Create(view).Field("propsIntroduction").GetValue<WGPropsIntroduction>();
                            WGText effectDescription = Traverse.Create(propsIntroduction).Field("effectDescription").GetValue<WGText>();
                            effectDescription.Text = effectDescriptionStr;
                        }
                    }
                }
            }
        }
    }
}
