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
        public class AutoDisassembler : IRunnableModule
        {
            public const string CONFIG_GROUP = "AutoDisassembler";
            private const string CONFIG_DISASSEMBLE = "UnwantedItems";
            private const string CONFIG_DISASSEMBLER = "Disassembler";

            private readonly List<string> disassemblyWhiteList;
            private readonly IMyGridTerminalSystem grid;
            private IMyAssembler disassembler;
            private readonly ErrorReporter reporter;

            public string Name
            {
                get { return "Automatic Disassembler"; }
            }

            public bool IsRunnable
            {
                // in order for this to work we need:
                // a functional assembler
                // items to disassemble

                // checking for working should allow us to check on a per run basis and pick back up when things start working again?
                get { return Util.IsBlockUsable(disassembler) && disassemblyWhiteList.Count > 0; }
            }

            public AutoDisassembler(MyIni config, IMyGridTerminalSystem grid, ErrorReporter reporter)
            {
                this.reporter = reporter;
                this.grid = grid;

                disassemblyWhiteList = new List<string>();
                var items = config.Get(CONFIG_GROUP, CONFIG_DISASSEMBLE).ToString().Split(';');

                foreach (var item in items)
                {
                    var trimmed = item.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !disassemblyWhiteList.Contains(trimmed))
                        disassemblyWhiteList.Add(trimmed);
                }

                disassembler = Util.GetBlockUsingConfig<IMyAssembler>(grid, config, $"{CONFIG_GROUP}.{CONFIG_DISASSEMBLER}");

                if(disassembler == null)
                    FindDisassembler();
            }

            public bool Run()
            {
                if (!IsRunnable) return false;

                var cargos = new List<IMyCargoContainer>();
                grid.GetBlocksOfType(cargos);
                
                foreach (var cargo in cargos)
                {
                    var inventory = cargo.GetInventory();
                    if (inventory == null || inventory.ItemCount == 0) continue;

                    for (int i = 0; i < inventory.ItemCount; i++)
                    {
                        var item = inventory.GetItemAt(i);
                        if (item.HasValue && disassemblyWhiteList.Contains(Util.GetItemName(item.Value.Type)))
                            SendItemToDisassembler(inventory, item.Value);
                    }
                }

                // make sure that it's getting switched back when you're done
                if(disassembler.IsQueueEmpty)
                    disassembler.Mode = MyAssemblerMode.Assembly;

                return true;
            }

            private void SendItemToDisassembler(IMyInventory src, MyInventoryItem item)
            {
                // set our assembler to disassembly mode and remove any items for the queue
                if (disassembler.Mode != MyAssemblerMode.Disassembly)
                {
                    disassembler.Mode = MyAssemblerMode.Disassembly;
                    disassembler.ClearQueue();
                }

                if(disassembler.OutputInventory.CanItemsBeAdded(item.Amount, item.Type))
                {
                    src.TransferItemTo(disassembler.OutputInventory, item, item.Amount);
                    disassembler.AddQueueItem(Util.GetMyDefinitionId(Util.GetItemName(item.Type)), item.Amount);
                }
            }

            public IList<ModuleReport> ReportStatus()
            {
                var reports = new List<ModuleReport>();

                reports.AddRange(Util.HandleBasicBlockReport(disassembler, "Disassembler", true, Name));

                if (disassemblyWhiteList.Count == 0)
                {
                    reports.Add(new ModuleReport()
                    {
                        ModuleName = Name,
                        Level = ReportLevel.Error,
                        Message = "No items configured for automatic disassembly."
                    });
                }

                return reports;
            }
            
            private void FindDisassembler()
            {
                var assemblers = new List<IMyAssembler>();
                grid.GetBlocksOfType(assemblers);

                disassembler = assemblers.FirstOrDefault(a => a.CooperativeMode);
                if (disassembler == null)
                    disassembler = assemblers.FirstOrDefault();
            }
        }
    }
}
