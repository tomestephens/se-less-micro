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
        public enum ReportLevel
        {
            Debug = 0,
            Info = 1,
            Error = 2
        }

        public class ErrorReporter
        {
            public const string CONFIG_GROUP = "ErrorReporter";
            private const string CONFIG_DISPLAY = "Display";
            private const string CONFIG_REPORT_LEVEL = "ReportLevel";

            private readonly IMyTextPanel display;
            private readonly ReportLevel reportLevel;
            private readonly Action<string> echo;

            public bool IsRunnable
            {
                get { return Util.IsBlockUsable(display) || echo != null; }
            }

            public ErrorReporter(MyIni config, IMyGridTerminalSystem grid, Action<string> echo)
            {
                display = Util.GetBlockUsingConfig<IMyTextPanel>(grid, config, $"{CONFIG_GROUP}.{CONFIG_DISPLAY}");

                int lev = 2;
                int.TryParse(config.Get(CONFIG_GROUP, CONFIG_REPORT_LEVEL).ToString(), out lev);

                reportLevel = (ReportLevel)lev;

                this.echo = echo;
            }

            public void Echo(string module, ReportLevel level, string message)
            {
                // going to use this even if the core part of the module is not enabled
                // if this isn't runnable, we have an issue
                if (echo != null && level >= reportLevel)
                    echo($"{module}::{level.ToString()}::{message}");
            }
            
            public bool Report(IList<ModuleReport> reports)
            {
                // if this isn't runnable, we have an issue
                if (!IsRunnable) return false;

                var sb = new StringBuilder();
                sb.AppendLine("Unassociated's Management Script Report");

                if (reports.Count > 0)
                {
                    foreach (var report in reports)
                    {
                        if (report.Level >= reportLevel)
                            sb.AppendLine($"{report.ModuleName}::{report.Level.ToString()}::{report.Message}");
                    }
                }
                else
                    sb.AppendLine("Nothing to report.");

                if (Util.IsBlockUsable(display))
                    Util.DisplayText(sb.ToString(), display);
                else
                    echo(sb.ToString());

                return true;
            }
        }
    }
}
