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
        public class OreTracker: IRunnableModule
        {
            public const string CONFIG_GROUP = "OreTracker";
            private const string CONFIG_DISPLAY = "Display";

            private readonly IMyTextPanel display;
            private readonly IMyGridTerminalSystem grid;
            private readonly ErrorReporter reporter;

            public string Name
            {
                get { return "Ore Tracker"; }
            }

            public bool IsRunnable
            {
                get { return Util.IsBlockUsable(display); }
            }           

            public OreTracker(MyIni config, IMyGridTerminalSystem grid, ErrorReporter reporter)
            {
                this.grid = grid;
                this.reporter = reporter;
                display = Util.GetBlockUsingConfig<IMyTextPanel>(grid, config, $"{CONFIG_GROUP}.{CONFIG_DISPLAY}");
                
            }

            public bool Run()
            {
                if (!IsRunnable) return false;

                var stock = new Dictionary<string, MyFixedPoint>();

                var sb = new StringBuilder("Ore Type::Stock\n");

                DetermineOreStock(stock);

                foreach (var kvp in stock)
                {
                    sb.AppendFormat("{0}::{1}\n", kvp.Key, kvp.Value.ToString());
                }

                Util.DisplayText(sb.ToString(), display);

                return true;
            }

            public IList<ModuleReport> ReportStatus()
            {
                return Util.HandleBasicBlockReport(display, "Ore Display", true, Name);
            }

            private void DetermineOreStock(Dictionary<string, MyFixedPoint> stock)
            {
                // this isn't checking refineries...
                var cargoContainers = new List<IMyCargoContainer>();
                grid.GetBlocksOfType(cargoContainers);

                foreach (var cc in cargoContainers)
                {
                    if (!cc.HasInventory) continue;

                    var inventory = cc.GetInventory();
                    if (inventory == null || inventory.ItemCount == 0) continue;

                    for (int i = 0; i < inventory.ItemCount; i++)
                    {
                        var item = inventory.GetItemAt(i);
                        if (!item.HasValue || !IsOre(item.Value.Type)) continue;

                        var key = Util.GetItemName(item.Value.Type);
                        Util.AddStockItem(stock, key, item.Value.Amount);
                    }
                }
            }

            private static bool IsOre(MyItemType type)
            {
                return type.ToString().Contains("Ore");
            }
        }
    }
}
