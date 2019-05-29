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
        public class DamageTracker : IRunnableModule
        {
            public const string CONFIG_GROUP = "DamageTracker";
            private const string CONFIG_THRESHOLD = "Threshold";
            private const string CONFIG_DISPLAY = "Display";
            private const string CONFIG_USE_ERROR = "UseErrorDisplay";

            private readonly IMyGridTerminalSystem grid;
            private readonly ErrorReporter reporter;
            private readonly int threshold;
            private readonly bool useErrorDisplay;
            private readonly IMyTextPanel display;


            public string Name
            {
                get { return "Damage Tracker"; }
            }

            public bool IsRunnable
            {
                get { return Util.IsBlockUsable(display) || useErrorDisplay; }
            }

            public DamageTracker(MyIni config, IMyGridTerminalSystem grid, ErrorReporter reporter)
            {
                this.grid = grid;
                this.reporter = reporter;

                threshold = config.Get(CONFIG_GROUP, CONFIG_THRESHOLD).ToInt16(80);
                useErrorDisplay = config.Get(CONFIG_GROUP, CONFIG_USE_ERROR).ToBoolean();
                if(!useErrorDisplay)
                    display = Util.GetBlockUsingConfig<IMyTextPanel>(grid, config, $"{CONFIG_GROUP}.{CONFIG_DISPLAY}");
            }

            public bool Run()
            {
                if (!IsRunnable || useErrorDisplay) return false;

                var blocks = GetDamangedBlocks();
                var sb = new StringBuilder();

                sb.AppendLine("Damaged Blocks:");

                foreach (var block in GetDamangedBlocks())
                    sb.AppendLine(block);

                return true;
            }

            float GetMyTerminalBlockHealth(IMyTerminalBlock block)
            {
                IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
                return (slimblock.BuildIntegrity - slimblock.CurrentDamage) / slimblock.MaxIntegrity;
            }

            private List<string> GetDamangedBlocks()
            {
                var dmg = new List<string>();
                var allBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocks(allBlocks);

                foreach (var block in allBlocks)
                {
                    var health = GetMyTerminalBlockHealth(block) * 100;
                    if (threshold > health)
                    {
                        dmg.Add($"{block.CustomName}: {health.ToString("0.00")}");
                    }
                }

                return dmg;
            }

            public IList<ModuleReport> ReportStatus()
            {
                var reports = new List<ModuleReport>();
                if(useErrorDisplay)
                {
                    foreach(var block in GetDamangedBlocks())
                    {
                        reports.Add(new ModuleReport()
                        {
                            ModuleName = Name,
                            Level = ReportLevel.Info,
                            Message = block
                        });
                    }
                }

                return reports;
            }
        }
    }
}
