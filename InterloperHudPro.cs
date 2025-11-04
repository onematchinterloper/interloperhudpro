using System.Collections;
using System.Reflection;
using System.Security.AccessControl;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop;
//using Il2CppInterop.Runtime;
//using Il2CppInterop.Runtime.Injection;
//using Il2CppInterop.Runtime.InteropTypes;
using Il2CppTLD.UI;
using Il2CppTMPro;

using MelonLoader;
using UnityEngine;
//using UnityEngine.UI;
//using static Il2Cpp.UIAtlas;


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
            float netWindChill = Mathf.Min(windChill + clothingWindBonus, 0f);  // max 0,  no warming from wind
            
            float frigidBonesBonus = 0;
            if (GameManager.GetConditionComponent().HasSpecificAffliction(AfflictionType.PoorCirculation))
            {
                // Misery FrigidBones Penalty (i.e. PoorCirculation)
                frigidBonesBonus -= 5;
            }
            float feelsLike = airTemp + clothingBonus + netWindChill + frigidBonesBonus;

            float currentWeight = GameManager.GetInventoryComponent().GetTotalWeightKG().m_Units / 1e9f;

            var tempText = $"{airTemp:F0}°   {windChill:F0}°   {feelsLike:F0}°";
            var weightText = $"{currentWeight:F2} KG";

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

        [HarmonyPatch(typeof(GameManager), "Start")]
        private static class ResetHudRefsOnGameStart
        {
            private static void Postfix()
            {
                // Clean up TempMeter
                if (TempMeter.tempObject != null)
                {
                    UnityEngine.Object.Destroy(TempMeter.tempObject);
                    TempMeter.tempObject = null;
                }
                TempMeter.elapsedMinutes = 0d;

                // Clean up ActiveItemHUDPatch
                ActiveItemHUDPatch.elapsedMinutes = 0d;
                // (If you ever add a static GameObject/UILabel here, destroy it too)

                // Clean up DayNightHUDPatch
                if (DayNightHUDPatch.dayNightLabel != null)
                {
                    UnityEngine.Object.Destroy(DayNightHUDPatch.dayNightLabel.gameObject);
                    DayNightHUDPatch.dayNightLabel = null;
                }
                DayNightHUDPatch.elapsedMinutes = 0d;
            }
        }

        private static Color darkRed = new Color(0.8f, 0.2f, 0.23f, 1.000f);
        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class TempMeter
        {
            public static double elapsedMinutes = 0d;
            public static GameObject tempObject;

            private static void Postfix(StatusBar __instance)
            {
                //if (__instance.m_StatusBarType != StatusBar.StatusBarType.Hunger) return;
                if (!__instance.m_IsOnHUD) return;

                if (__instance.m_StatusBarType == StatusBar.StatusBarType.Cold)
                {
                    UpdateTempLabel(__instance);
                }
            }

            private static void UpdateTempLabel(StatusBar __instance)
            {
                if (tempObject == null)
                {
                    // init
                    UISprite sprite = __instance.m_OuterBoxSprite.GetComponent<UISprite>();
                    GameObject spriteObject = sprite.gameObject;

                    tempObject = new GameObject("temperature");
                    tempObject.transform.SetParent(spriteObject.transform.parent);
                    tempObject.transform.localScale = spriteObject.transform.localScale;

                    UILabel tempLabel = tempObject.AddComponent<UILabel>();
                    tempLabel.text = "0°C";
                    // tempLabel.color = Color.white;
                    tempLabel.color = new Color(0.9f, 0.95f, 1f);  // Use an off white
                    tempLabel.fontStyle = FontStyle.Normal;
                    tempLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    tempLabel.fontSize = 32;
                    tempLabel.effectStyle = UILabel.Effect.Outline;
                    tempLabel.effectColor = new Color(0.125f, 0.094f, 0.094f, 0.6f);
                    tempLabel.effectDistance = new Vector2(1.7f, 1.7f);

                    tempLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
                    tempLabel.alignment = NGUIText.Alignment.Left;
                    tempLabel.pivot = UIWidget.Pivot.Left;

                    //int x_offset = -sprite.width / 2; // + tempLabel.width/2;
                    int x_offset = -tempLabel.width / 2;
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

        [HarmonyPatch(typeof(Panel_HUD), "Update")]
        private static class ActiveItemHUDPatch
        {
            public static double elapsedMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                double now = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                if (now - elapsedMinutes < 0.01d) return;
                elapsedMinutes = now;

                // TODO get this working
                var item = __instance.m_EquipItemPopup;
                if (item == null) return;
                //if (!item.gameObject.activeInHierarchy) return;

                var activeItem = GameManager.GetPlayerManagerComponent().m_ItemInHands;
                if (activeItem)
                {
                    var hudText2 = InterloperHudProMain.getHUDText2();
                    UISprite[] sprites = __instance.m_EquipItemPopup.GetComponentsInChildren<UISprite>();
                    foreach (UISprite sprite in sprites)
                    {
                        if (sprite != null && sprite.type == UIBasicSprite.Type.Filled && sprite.depth >= 3)
                        {

                            var s = sprite;
                            var msg = $"Found bar: GO={s.gameObject.name.PadRight(20)}, Atlas={s.spriteName.PadRight(30)}, Depth={s.depth}";
                            msg += $"Parent={s.transform.parent.name} active1={s.gameObject.activeInHierarchy} active2={s.isActiveAndEnabled}";                            
                            EnsureHudLabel(sprite.gameObject, "conditionpercent", hudText2, Color.yellow, new Vector3(40, 0, 0));
                        }
                    }
                }

            }

            private static void EnsureHudLabel(GameObject parentSprite, string name, string text, Color color, Vector3 offset)
            {
                var labelObj = parentSprite.transform.parent.Find(name)?.gameObject;
                UILabel label;
                if (labelObj == null)
                {
                    labelObj = new GameObject(name);
                    labelObj.transform.SetParent(parentSprite.transform.parent, false);
                    labelObj.transform.localScale = parentSprite.transform.localScale;
                    labelObj.transform.localPosition = offset;

                    label = labelObj.AddComponent<UILabel>();
                    label.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    label.fontSize = 20;
                    label.effectStyle = UILabel.Effect.Outline;
                    label.effectColor = new Color(0, 0, 0, 0.6f);
                    label.effectDistance = new Vector2(1.5f, 1.5f);
                    label.alignment = NGUIText.Alignment.Left;
                    label.pivot = UIWidget.Pivot.Left;

                    //int x_offset = -parentSprite. / 2; // + tempLabel.width/2;
                    int x_offset = label.width;
                    int y_offset = -26;
                    label.transform.localPosition = new Vector3(x_offset, y_offset, 0);
                }
                else
                {
                    label = labelObj.GetComponent<UILabel>();
                }

                label.text = text;
                // label.color = color;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), "Update")]
        private static class DayNightHUDPatch
        {
            public static UILabel dayNightLabel;
            public static double elapsedMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                double now = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                if (dayNightLabel != null && now - elapsedMinutes < 0.01d) return;
                elapsedMinutes = now;

                TimeOfDay tod = GameManager.GetTimeOfDayComponent();
                if (tod == null) return;

                int day = tod.GetDayNumber();
                int hour = Mathf.FloorToInt(tod.GetHour());
                int minute = Mathf.FloorToInt(tod.GetMinutes());
                
                string text = $"Day {day}  {hour:D2}:{minute:D2}";
                // MelonLogger.Msg(text);
                //DrawOnMiddle(__instance, text);
                DrawOnTemperatureHUD(__instance, text);
            }

            private static void DrawOnTemperatureHUD(Panel_HUD __instance, String text)
            {
                // if the temperature HUD hasn’t been initialized, bail
                if (TempMeter.tempObject == null) return;

                if (dayNightLabel == null)
                {
                    // Create new label as a child of the tempObject’s parent
                    GameObject parent = TempMeter.tempObject.transform.parent.gameObject;
                    dayNightLabel = NGUITools.AddWidget<UILabel>(parent);

                    dayNightLabel.name = "DayNightHUDLabel";
                    dayNightLabel.text = text;
                    // dayNightLabel.color = Color.yellow;
                    dayNightLabel.color = new Color(0.9f, 0.95f, 1f);  // Use an off white
                    dayNightLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    dayNightLabel.fontSize = 32;
                    dayNightLabel.effectStyle = UILabel.Effect.Outline;
                    dayNightLabel.effectColor = new Color(0.125f, 0.094f, 0.094f, 0.6f);
                    dayNightLabel.effectDistance = new Vector2(1.7f, 1.7f);

                    dayNightLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
                    dayNightLabel.alignment = NGUIText.Alignment.Left;
                    dayNightLabel.pivot = UIWidget.Pivot.Left;

                    // Position it above tempObject
                    UILabel tempLabel = TempMeter.tempObject.GetComponent<UILabel>();
                    int y_offset = tempLabel.height + 10; // adjust padding
                    dayNightLabel.transform.localPosition = TempMeter.tempObject.transform.localPosition + new Vector3(0, y_offset, 0);
                }
                else
                {
                    // Just update text each frame
                    dayNightLabel.text = text;
                }

            }

            private static void DrawOnMiddle(Panel_HUD __instance, String text)
            {
                if (dayNightLabel == null)
                {
                    MelonLoader.MelonLogger.Msg("=== Direct children of Panel_HUD ===");
                    Transform root = __instance.gameObject.transform.Find("NonEssentialHud");
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root.GetChild(i);
                        MelonLoader.MelonLogger.Msg($"Child {i}: {child.gameObject.name}");
                    }

                    // anchor under the DayNight HUD object
                    //Transform parent = __instance.transform.Find("DayNight");
                    Transform parent = __instance.gameObject.transform; // middle of screen!
                    if (parent == null)
                    {
                        return;
                    }
                    MelonLogger.Msg("daynight inited");

                    GameObject go = new GameObject("DayNightText");
                    go.transform.SetParent(parent, false);
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = new Vector3(0, -40, 0); // adjust below the arc

                    dayNightLabel = go.AddComponent<UILabel>();
                    dayNightLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    dayNightLabel.fontSize = 22;
                    dayNightLabel.alignment = NGUIText.Alignment.Center;
                    dayNightLabel.color = Color.white;
                    dayNightLabel.effectStyle = UILabel.Effect.Outline;
                    dayNightLabel.effectColor = new Color(0, 0, 0, 0.7f);
                    dayNightLabel.effectDistance = new Vector2(1.2f, 1.2f);
                    MelonLogger.Msg("daynight inited");
                }

                dayNightLabel.text = text;
            }
        }
    }

}
