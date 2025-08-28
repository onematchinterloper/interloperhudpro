using System.Collections;
using System.Reflection;

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
        private static Color darkRed = new Color(0.8f, 0.2f, 0.23f, 1.000f);

        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class TempMeter
        {
            private static double elapsedMinutes = 0d;
            private static void Postfix(StatusBar __instance)
            {
                //if (__instance.m_StatusBarType != StatusBar.StatusBarType.Hunger) return;
                if (!__instance.m_IsOnHUD) return;

                UISprite sprite = __instance.m_OuterBoxSprite.GetComponent<UISprite>();
                
                if (__instance.m_StatusBarType == StatusBar.StatusBarType.Cold)
                {
                    UpdateTempLabel(sprite);
                }
            }
            private static void UpdateTempLabel(UISprite sprite)
            {
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
            private static double elapsedMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                // TODO get this working
                var item = __instance.m_EquipItemPopup;
                if (item == null) return;
                //if (!item.gameObject.activeInHierarchy) return;

                double now = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                if (now - elapsedMinutes < 0.01d) return;
                elapsedMinutes = now;

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
    }

}
