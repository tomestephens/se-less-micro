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
    partial class Program : MyGridProgram
    {
        private const string CONFIG_ENABLED = "Enabled";

        private readonly List<IRunnableModule> modules;
        private readonly ErrorReporter reporter;
        private readonly MyIni config = new MyIni();

        public Program()
        {
            MyIniParseResult res;
            if (!config.TryParse(Me.CustomData, out res))
            {
                Echo($"CustomData error:\nLine {res}");
            }

            reporter = new ErrorReporter(config, GridTerminalSystem, Echo);
            modules = new List<IRunnableModule>();

            InitModules();

            // only setup regular runs if we actually have modules to run
            if (modules.Count != 0)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var errors = new List<ModuleReport>();

            foreach(var module in modules)
            {
                try
                {
                    if (!module.Run())
                        errors.AddRange(module.ReportStatus());
                }
                catch (Exception ex)
                {
                    errors.Add(new ModuleReport()
                    {
                        ModuleName = module.Name,
                        Level = ReportLevel.Error,
                        Message = ex.Message
                    });
                }
            }

            reporter.Report(errors);
        }

        private void InitModules()
        {
            if (config.Get(IngotTracker.CONFIG_GROUP, CONFIG_ENABLED).ToBoolean())
                TryAddModule(new IngotTracker(config, GridTerminalSystem, reporter));
            else
                reporter.Echo("init", ReportLevel.Info, $"{IngotTracker.CONFIG_GROUP} is disabled.");

            if (config.Get(OreTracker.CONFIG_GROUP, CONFIG_ENABLED).ToBoolean())
                TryAddModule(new OreTracker(config, GridTerminalSystem, reporter));
            else
                reporter.Echo("init", ReportLevel.Info, $"{OreTracker.CONFIG_GROUP} is disabled.");

            if (config.Get(ParStockManager.CONFIG_GROUP, CONFIG_ENABLED).ToBoolean())
                TryAddModule(new ParStockManager(config, GridTerminalSystem, reporter));
            else
                reporter.Echo("init", ReportLevel.Info, $"{ParStockManager.CONFIG_GROUP} is disabled.");

            if (config.Get(AutoDisassembler.CONFIG_GROUP, CONFIG_ENABLED).ToBoolean())
                TryAddModule(new AutoDisassembler(config, GridTerminalSystem, reporter));
            else
                reporter.Echo("init", ReportLevel.Info, $"{AutoDisassembler.CONFIG_GROUP} is disabled.");

            if(config.Get(DamageTracker.CONFIG_GROUP, CONFIG_ENABLED).ToBoolean())
                TryAddModule(new DamageTracker(config, GridTerminalSystem, reporter));
            else
                reporter.Echo("init", ReportLevel.Info, $"{DamageTracker.CONFIG_GROUP} is disabled.");
        }

        private void TryAddModule(IRunnableModule module)
        {
            // wanted to do dynamic instantiation here, but SE doesn't seem to allow it
            if (module.IsRunnable)
            {
                modules.Add(module);
                reporter.Echo("init", ReportLevel.Info, $"{module.Name} initialized");
            }
            else
                reporter.Report(module.ReportStatus());
        }
    }
}