using System.Collections;
using System.Reflection;

using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;


using Il2Cpp;
using Il2CppInterop;
using Il2CppTLD.UI;
using Il2CppTMPro;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;


// using ModSettingsAPI;

namespace InterloperHudPro
{
    public class InterloperHudProMain : MelonMod
    {
        private GUIStyle style;
        private bool guiInitiated = false;
        private TextMeshProUGUI helloText;


        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Interloper HUD PRO Starting");
        }

        public static string getHUDText1()
        {
            float airTemp = GameManager.GetWeatherComponent().GetCurrentTemperature();
            float clothingBonus = GameManager.GetPlayerManagerComponent().m_WarmthBonusFromClothing;

            float windChill = GameManager.GetWeatherComponent().GetCurrentWindchill();
            float clothingWindBonus = GameManager.GetPlayerManagerComponent().m_WindproofBonusFromClothing;
            float netWindChill = Mathf.Min(windChill + clothingWindBonus, 0f);  // max 0, no warming from wind
            float feelsLike = airTemp + clothingBonus + netWindChill;

            float currentWeight = GameManager.GetInventoryComponent().GetTotalWeightKG().m_Units / 1e9f;

            var tempText = $"{airTemp:F0}°   {windChill:F0}°   {feelsLike:F0}°";
            var weightText = $"{currentWeight:F2}KG";

            return tempText + "       " + weightText;
        }
        public static string getHUDText2()
        {
            var activeItem = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            var activeItemText = activeItem?.m_CurrentHP;
            var itemText = $"{activeItemText:F0}%";
            return itemText;
        }

        //public override void OnGUI()
        //{
        //    if (GameManager.m_IsPaused
        //        || GameManager.GetPlayerManagerComponent() == null
        //        || GameManager.GetConditionComponent() == null
        //        || InterfaceManager.GetPanel<Panel_Inventory>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_Map>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_Crafting>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_Container>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_PauseMenu>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_Log>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_Clothing>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_OptionsMenu>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_MainMenu>()?.isActiveAndEnabled == true
        //        || InterfaceManager.GetPanel<Panel_FirstAid>()?.isActiveAndEnabled == true
        //        )
        //    {
        //        return;
        //    }

        //    if (!guiInitiated)
        //    {
        //        style = new GUIStyle(GUI.skin.label)
        //        {
        //            fontSize = 16,
        //            normal = { textColor = Color.white }
        //        };

        //        guiInitiated = true;
        //        MelonLogger.Msg("Interloper HUD PRO initiated");
        //        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
        //        {
        //            MelonLogger.Msg($"Canvas found: {canvas.name}, enabled={canvas.enabled}, worldCamera={canvas.worldCamera?.name}");
        //        }
        //    }

        //    float y = 160;
        //    float x = 80;
        //    float lineHeight = 25;
        //    string hudText1 = InterloperHudProMain.getHUDText1();
        //    // Temp/Weight Stats
        //    if (hudText1.Trim() != "")
        //    {
        //        GUI.Label(new Rect(x, Screen.height - y, 400, lineHeight), hudText1, style); // y += lineHeight;

        //        string hudText2 = getHUDText2(); // Active item %
        //        if (hudText2.Trim() != "")
        //        {
        //            x = 160;
        //            y = 140;
        //            GUI.Label(new Rect(Screen.width - x, Screen.height - y, 400, lineHeight), hudText2, style); // y += lineHeight;
        //        }
        //    }
        //}

    }


    internal static class Patches
    {
        //// ===== HUD Panel Patch =====
        //[HarmonyPatch(typeof(Panel_HUD), "Enable")]
        //private static class HelloHUDPatch
        //{
        //    private static bool initialized = false;

        //    private static void Postfix(Panel_HUD __instance)
        //    {
        //        //if (initialized) return;
        //        initialized = true;

        //        // Clone the condition label from HUD
        //        var conditionMeter = __instance.m_Label_Condition.transform;
        //        if (conditionMeter == null) return;

        //        var helloGO = new GameObject("HelloConditionLabel");
        //        helloGO.transform.SetParent(conditionMeter, false);

        //        var helloLabel = helloGO.AddComponent<UILabel>();
        //        helloLabel.text = InterloperHudProMain.getHUDText1();
        //        helloLabel.alignment = NGUIText.Alignment.Center;
        //        helloLabel.fontSize = 18;
        //        helloLabel.color = Color.yellow;

        //        // Move it above the condition circle
        //        helloLabel.transform.localPosition += new Vector3(0, 50, 0);

        //        NGUITools.SetActive(helloGO, true);
        //        // MelonLogger.Msg("Interloper HelloHUDPatch");
        //    }
        //}

        //[HarmonyPatch(typeof(Panel_HUD), "Enable")]
        //private static class ShowHudNumbers
        //{
        //    private static void Postfix(Panel_HUD __instance)
        //    {

        //        foreach (UILabel lbl in new UILabel[] {
        //            __instance.m_Label_Condition,
        //            __instance.m_Label_DebugLines,
        //            __instance.m_Label_SurvivalTime,
        //        })
        //        {
        //            if (lbl == null) continue;
        //            lbl.enabled = true;
        //            NGUITools.SetActive(lbl.gameObject, true);
        //            lbl.color = Color.yellow; // make them pop
        //        }
        //    }
        //}


        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class TempMeter
        {
            private static double elapsedMinutes = 0d;
            private static Color darkRed = new Color(0.8f, 0.2f, 0.23f, 1.000f);
            private static void Postfix(StatusBar __instance)
            {
                //if (__instance.m_StatusBarType != StatusBar.StatusBarType.Hunger) return;
                if (__instance.m_StatusBarType != StatusBar.StatusBarType.Cold) return;
                if (!__instance.m_IsOnHUD) return;

                UISprite sprite = __instance.m_OuterBoxSprite.GetComponent<UISprite>();
                GameObject spriteObject = sprite.gameObject;
                GameObject tempObject = spriteObject.transform.parent.FindChild("temperature")?.gameObject;

                if (tempObject == null)
                {
                    // init
                    tempObject = new GameObject("temperature");
                    tempObject.transform.SetParent(spriteObject.transform.parent);
                    tempObject.transform.localScale = spriteObject.transform.localScale;
                    

                    UILabel tempLabel = tempObject.AddComponent<UILabel>();
                    tempLabel.text = "0°C";
                    tempLabel.color = Color.white;
                    tempLabel.fontStyle = FontStyle.Normal;
                    tempLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    tempLabel.fontSize = 32;
                    tempLabel.effectStyle = UILabel.Effect.Outline;
                    tempLabel.effectColor = new Color(0.125f, 0.094f, 0.094f, 0.6f);
                    tempLabel.effectDistance = new Vector2(1.7f, 1.7f);

                    
                    MelonLogger.Msg(tempLabel.width + " " + tempLabel.text.Length);
                    

                    tempLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
                    tempLabel.alignment = NGUIText.Alignment.Left;
                    tempLabel.pivot = UIWidget.Pivot.Left;

                    //int x_offset = -sprite.width / 2; // + tempLabel.width/2;
                    int x_offset =  - tempLabel.width/2;
                    int y_offset = 20 + tempLabel.height;
                    tempObject.transform.localPosition = new Vector3(x_offset, y_offset, 0);
                }
                else if (GameManager.GetHighResolutionTimerManager().GetElapsedMinutes() - elapsedMinutes >= 0.1d)
                {
                    // update every 0.1 ingame minutes
                    UILabel tempLabel = tempObject.GetComponent<UILabel>();
                    if (tempLabel != null && GameManager.GetFreezingComponent() != null)
                    {

                        int temp = (int)Math.Round(GameManager.GetFreezingComponent().CalculateBodyTemperature());
                        if (temp < 0)
                        {
                            tempLabel.color = darkRed;
                        }
                        else
                        {
                            tempLabel.color = Color.white;
                        }
                        tempLabel.text = InterloperHudProMain.getHUDText1();
                        elapsedMinutes = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                    }
                }
            }
        }
    }
}
