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
            showFavExp = plugin.Config.Bind("界面改进", "显示好友好感度与礼物好感度", false, "在好友界面显示当前好感度/总需好感度 在送礼和商店界面显示礼物可提高的好感度");
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

        //在送礼界面显示礼物好感度
        [HarmonyPostfix, HarmonyPatch(typeof(Props), "PropsEffectDescription", MethodType.Getter)]
        public static void ShowRelationship_Props(Props __instance, ref string __result)
        {
            if (!showFavExp.Value || __instance.PropsType != PropsType.Present)
                return;
            List<string> strFav = new List<string>();
            foreach (var propsEffect in __instance.PropsEffect)
            {
                if (propsEffect is PropsFavorable pf)
                {
                    strFav.Add( string.Format("{0}{1}+{2}", Game.GameData.Exterior[pf.Npcid].FullName(), Game.Data.Get<StringTable>("General_Favorability").Text, pf.Value));
                }
            }
            __result += string.Join("，", strFav);
        }

    }
}
