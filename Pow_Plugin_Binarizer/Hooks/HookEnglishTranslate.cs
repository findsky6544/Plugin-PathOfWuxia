﻿using HarmonyLib;
using Heluo.UI;
using UnityEngine;
using UnityEngine.UI;
using Heluo.Platform;
using System.ComponentModel;

namespace PathOfWuxia
{
    [System.ComponentModel.DisplayName("英语翻译")]
    [Description("英语翻译")]
    public class HookEnglishTranslate : IHook
    {
        public void OnRegister(PluginBinarizer plugin)
        {
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGText), "Text", MethodType.Setter)]
        public static void WGText_Postfix(ref WGText __instance, ref Text ___text)
        {
            ___text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ___text.fontSize = 20;
            if (___text.text.Length <= 20)
            {
                ___text.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else
            {
                ___text.horizontalOverflow = HorizontalWrapMode.Wrap;
                ___text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGStringTable), "UpdateString")]
        public static void WGStringTable_Postfix(ref WGStringTable __instance)
        {
            __instance.GetComponent<Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (__instance.GetComponent<Text>().text.Length <= 20)
            {
                __instance.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else
            {
                __instance.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
                __instance.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIBattleStatus), "UpdateView")]
        public static void Postfix(ref UIBattleStatus __instance)
        {
            __instance.FinalCounterAttack.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            __instance.FinalCrit.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            __instance.FinalDodge.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            __instance.FinalHit.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            __instance.FinalParry.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIQuest), "SetTrackedQuest")]
        public static void SetTrackedQuest_Postfix(ref UIQuest __instance, ref WGText ___questName, ref WGText ___description)
        {
            ___questName.GetComponent<Text>().fontSize = 18;
            ___description.GetComponent<Text>().fontSize = 16;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGBattleInfo), "UpdateInfo")]
        public static void UpdateInfo_Postfix(ref WGBattleInfo __instance, ref WGText ___Name)
        {
            ___Name.GetComponent<Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ___Name.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGPaoMaDengText), "AddMessage")]
        public static void WGPaoMaDengText_Postfix(ref WGPaoMaDengText __instance, ref WGText ___pmdText)
        {
            ___pmdText.GetComponent<Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGNurturanceBtn), "UpdateButton")]
        public static void WGNurturanceBtn_Postfix(ref WGNurturanceBtn __instance, ref WGText ___Btn_Name)
        {
            __instance.Btn_Name.GetComponent<Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            __instance.Btn_Name.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
            __instance.Btn_Name.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIRegistration), "NameCheck")]
        public static bool UIRegistration_Prefix()
        {
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SteamPlatform), "CheckWordLegality")]
        public static bool Prefix(string word, ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGBookBtn), "UpdateWidget")]
        public static void WGBookBtn_Postfix(ref WGText ___itemName)
        {
            if (___itemName.GetComponent<Text>().text.Length <= 20)
            {
                ___itemName.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else
            {
                ___itemName.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
                ___itemName.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGTraitBtn), "UpdateWidget")]
        public static void UpdateWidget_Postfix(ref WGText ___itemName)
        {
            ___itemName.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            ___itemName.GetComponent<Text>().rectTransform.offsetMin = new Vector2(-120f, -20f);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WGTitleValue), "TitleFormat")]
        public static void TitleFormat_Postfix(ref Text ___Title)
        {
            ___Title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ___Title.fontSize = 24;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(WGTitleValue), "ValueFromat")]
        public static void ValueFromat_Postfix(ref Text ___Value)
        {
            ___Value.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ___Value.fontSize = 20;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIRelationship), "UpdateRelationship")]
        public static void UpdateRelationship_Postfix(ref Text ___community_name, ref Text ___community_introduction)
        {
            ___community_name.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ___community_introduction.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /*
         * Updating Date to display Year and Month in English
         * Updating the Units in SetMonth and adding a . after the number in both SetYear and SetMonth
        */

        [HarmonyPrefix, HarmonyPatch(typeof(UIDate), "SetYear")]
        public static void SetYearPrefix(ref string yearStr)
        {
            yearStr = yearStr + ".";
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDate), "SetMonth")]
        public static void SetMonthPrefix(ref UIDate __instance, ref string monthStr)
        {
            monthStr = monthStr + ".";

            //Getting child 0, which is the RectTransform
            RectTransform UI_Date_rectTransform = __instance.gameObject.transform.GetChild(0) as RectTransform;

            //Getting the Date_BG which is the first child again (Index 0)
            RectTransform dateBG_rectTransform = UI_Date_rectTransform.gameObject.transform.GetChild(0) as RectTransform;

            //Here now we can get the Unit(child 0),Year (child 1), YearUnit(child 2) Month(child 3) and monthUnit(child 4)
            RectTransform unit = dateBG_rectTransform.gameObject.transform.GetChild(0) as RectTransform;
            RectTransform year = dateBG_rectTransform.gameObject.transform.GetChild(1) as RectTransform;
            RectTransform yearUnit = dateBG_rectTransform.gameObject.transform.GetChild(2) as RectTransform;
            RectTransform month = dateBG_rectTransform.gameObject.transform.GetChild(3) as RectTransform;
            RectTransform monthUnit = dateBG_rectTransform.gameObject.transform.GetChild(4) as RectTransform;

            //Disabling "Unit" since we do not need it in English
            unit.localPosition = new Vector3(0, 0, 0);
            unit.gameObject.SetActive(false);
            //Moving the Unit/Year/YearUnit/Month/monthUnit to hardcoded positions that look good
            year.localPosition = new Vector3(-60f, 41.5f, 0f);
            yearUnit.localPosition = new Vector3(-20f, 41.5f, 0);
            yearUnit.sizeDelta = new Vector2(70f, 50f);
            month.localPosition = new Vector3(35f, 41.5f, 0f);
            monthUnit.localPosition = new Vector3(86f, 41.5f, 0);
            monthUnit.sizeDelta = new Vector2(70f, 50f);

            //Lastly we overwrite the text they contain
            unit.gameObject.GetComponent<UnityEngine.UI.Text>().text = "";
            UnityEngine.UI.Text YearUnitText = yearUnit.gameObject.GetComponent<UnityEngine.UI.Text>();
            YearUnitText.text = "Year";
            YearUnitText.fontSize = 28;
            UnityEngine.UI.Text monthUnitText = monthUnit.gameObject.GetComponent<UnityEngine.UI.Text>();
            monthUnitText.text = "Month";
            monthUnitText.fontSize = 28;
        }

        /*
        * Do all MainUI manipulation in this function.
        */

        [HarmonyPostfix, HarmonyPatch(typeof(UIMain), "Show")]
        public static void UIMainShowPostfix(ref UIMain __instance)
        {
            RectTransform dynamicCanvas = (RectTransform)__instance.transform.Find("DynamicCanvas");
            RectTransform menu = (RectTransform)dynamicCanvas.transform.Find("Menu");
            //Updating the Credits Button Text
            RectTransform btnMember = (RectTransform)menu.Find("BtnMember");
            RectTransform btn = (RectTransform)btnMember.Find("Btn");
            RectTransform btn_text = (RectTransform)btn.Find("Text");
            UnityEngine.UI.Text btn_text_text = (UnityEngine.UI.Text)btn_text.gameObject.GetComponentInChildren(typeof(UnityEngine.UI.Text));
            btn_text_text.text = "Credits";

            /*
            Reducing Menu Button Font Size
            menu.Find("BtnContinue").Find("Btn").Find("Text").GetComponent<Text>().fontSize = 20;
                menu.Find("BtnNewGame").Find("Btn").Find("Text").GetComponent<Text>().fontSize = 23;
                menu.Find("BtnLoadGame").Find("Btn").Find("Text").GetComponent<Text>().fontSize = 23;
            menu.Find("BtnSystem").Find("Btn").Find("Text").GetComponent<Text>().fontSize = 16;
            menu.Find("BtnQuitGame").Find("Btn").Find("Text").GetComponent<Text>().fontSize = 23;
            */

            //Focing the UI to update itself after all the changes we made. We just force every RectTransform to update itself
            for (int i = 0; i < __instance.transform.childCount; ++i)
            {
                Transform t = __instance.transform.GetChild(i);
                if (t != null && t.GetType() == typeof(RectTransform))
                {
                    RectTransform rt = t as RectTransform;
                    rt.ForceUpdateRectTransforms();
                }
            }
        }
    }

}
