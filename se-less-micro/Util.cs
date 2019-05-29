using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Util
        {
            public static void DisplayText(string text, IMyTextPanel panel)
            {
                // showing the text on screen is deprecated, not sure the "correct" way to do this
                panel.WriteText(text);
                panel.ShowPublicTextOnScreen();
            }

            public static string GetItemName(MyItemType type)
            {
                return type.SubtypeId;
            }

            // kind of pointless until I solve the monospace font issue....
            public static string ToFixedLength(string s, int len)
            {
                // only handling a length under the target...
                while (s.Length < len) s += " ";
                return s;
            }

            public static string GetNameFromDefinition(MyDefinitionId def)
            {
                var name = def.SubtypeId.ToString();
                switch (name)
                {
                    case "ConstructionComponent":
                    case "ComputerComponent":
                    case "MotorComponent":
                    case "GirderComponent":
                        name = name.Replace("Component", "");
                        break;
                    case "AngleGrinder":
                    case "HandDrill":
                    case "Welder":
                        name += "Item";
                        break;
                }
                return name;
            }

            public static void MoveWholeInventory(IMyInventory src, IMyInventory dest)
            {
                for (int i = 0; i < src.ItemCount; i++)
                {
                    var item = src.GetItemAt(i);
                    if (!item.HasValue) continue;
                    // the destination actually has space...
                    if (dest.CanItemsBeAdded(item.Value.Amount, item.Value.Type))
                    {
                        // how can we report back not having space?
                        src.TransferItemTo(dest, item.Value, null);
                    }
                }
            }

            public static MyDefinitionId GetMyDefinitionId(string item)
            {
                var def = "MyObjectBuilder_BlueprintDefinition/";
                switch (item)
                {
                    // where we just add "component"
                    case "Construction":
                    case "Computer":
                    case "Motor":
                    case "Girder":
                        def += item + "Component";
                        break;
                    case "AngleGrinderItem":
                    case "HandDrillItem":
                    case "WelderItem":
                        def += item.Replace("Item", "");
                        break;
                    // nothing to change
                    default:
                        def += item;
                        break;
                }
                return MyDefinitionId.Parse(def);
            }

            public static T GetBlockUsingConfig<T>(IMyGridTerminalSystem grid, MyIni config, string configName)
            {
                var block = default(T);

                var configKeys = configName.Split('.');
                // currently only accepting a group.item format
                if (configKeys.Count() != 2) return block;

                var blockName = config.Get(configKeys[0], configKeys[1]).ToString();
                if (!string.IsNullOrEmpty(blockName))
                {
                    var b = grid.GetBlockWithName(blockName);
                    if (b != null) block = (T)b;
                }

                return block;
            }

            public static void AddStockItem(Dictionary<string, MyFixedPoint> stock, string item, MyFixedPoint amount)
            {
                if (!stock.ContainsKey(item))
                    stock.Add(item, 0);

                stock[item] += amount;
            }

            public static bool IsBlockUsable<T>(T block) where T : IMyTerminalBlock
            {
                return block != null && block.IsWorking;
            }

            public static IList<ModuleReport> HandleBasicBlockReport<T>(T block, string blockName, bool errorIfMissing, string module) where T : IMyTerminalBlock
            {
                var reports = new List<ModuleReport>();
                if (block == null)
                {
                    reports.Add(new ModuleReport()
                    {
                        Level = errorIfMissing ? ReportLevel.Error : ReportLevel.Info,
                        Message = $"Unable to locate {blockName}"
                    });
                }
                else
                {
                    if (!block.IsFunctional)
                    {
                        reports.Add(new ModuleReport()
                        {
                            Level = ReportLevel.Error,
                            Message = $"{block.CustomName} is currently damaged."
                        });
                    }
                    else if (!block.IsWorking)
                    {
                        reports.Add(new ModuleReport()
                        {
                            Level = ReportLevel.Error,
                            Message = $"{block.CustomName} is not able to be used, verify it has power and is not turned off."
                        });
                    }
                }

                return reports;
            }
        }
    }
}
