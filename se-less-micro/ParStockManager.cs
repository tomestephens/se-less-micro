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
        public class ParStockManager: IRunnableModule
        {
            public const string CONFIG_GROUP = "StockManagement";
            private const string CONFIG_PAR_STOCK = "ParStocks";
            private const string CONFIG_DISPLAY = "Display";
            private const string CONFIG_CARGO = "Cargo";

            private readonly IMyCargoContainer mainCargo;
            private readonly IMyTextPanel display;
            private readonly Dictionary<string, int> parStocks;
            private readonly IMyGridTerminalSystem grid;            
            private readonly ErrorReporter reporter;
            private IMyAssembler assembler;

            public string Name
            {
                get { return "Stock Manager"; }
            }

            public bool IsRunnable
            {
                // in order for this to work we need:
                // a cargo container as "main"
                // a functional assembler
                // something defined as parstocks
                // without all three it doesn't really do anything

                
                get { return Util.IsBlockUsable(mainCargo) && Util.IsBlockUsable(assembler) && assembler.Mode == MyAssemblerMode.Assembly && parStocks.Count > 0; }
            }

            public ParStockManager(MyIni config, IMyGridTerminalSystem grid, ErrorReporter reporter)
            {
                this.reporter = reporter;
                this.grid = grid;

                parStocks = new Dictionary<string, int>();
                var items = config.Get(CONFIG_GROUP, CONFIG_PAR_STOCK).ToString().Split(';');

                foreach (var item in items)
                {
                    if (item.Contains('='))
                    {
                        var kv = item.Split('=');
                        if (kv.Length >= 2)
                            parStocks.Add(kv[0].Trim(), int.Parse(kv[1].Trim()));
                    }
                }

                mainCargo = Util.GetBlockUsingConfig<IMyCargoContainer>(grid, config, $"{CONFIG_GROUP}.{CONFIG_CARGO}");
                display = Util.GetBlockUsingConfig<IMyTextPanel>(grid, config, $"{CONFIG_GROUP}.{CONFIG_DISPLAY}");

                FindPrimaryAssembler();                    
            }

            public bool Run()
            {
                if (!IsRunnable) return false;
                // eventually this should handle finding new cargo containers if it's full...
                MoveAssemblerOutput(mainCargo.GetInventory());

                var stock = new Dictionary<string, MyFixedPoint>();
                DetermineComponentStock(stock);

                var sb = new StringBuilder(GetStockLine("Component", "Stock", "Par"));

                foreach (var kvp in parStocks)
                {
                    try
                    {
                        var currentStock = stock.ContainsKey(kvp.Key) ? stock[kvp.Key] : 0;
                        sb.Append(GetStockLine(kvp.Key, currentStock.ToString(), kvp.Value.ToString()));

                        if (currentStock < kvp.Value)
                        {
                            assembler.AddQueueItem(Util.GetMyDefinitionId(kvp.Key), kvp.Value - currentStock);
                        }
                    }
                    catch
                    {
                        reporter.Echo(Name, ReportLevel.Error, $"Failure processing item: {kvp.Key}");
                        throw;
                    }
                }

                if(Util.IsBlockUsable(display))
                    Util.DisplayText(sb.ToString(), display);

                return true;
            }

            public IList<ModuleReport> ReportStatus()
            {
                var reports = new List<ModuleReport>();

                reports.AddRange(Util.HandleBasicBlockReport(display, "Stock Display", false, Name));
                reports.AddRange(Util.HandleBasicBlockReport(mainCargo, "Primary Cargo", true, Name));
                reports.AddRange(Util.HandleBasicBlockReport(assembler, "Primary Assembler", true, Name));
                
                if(assembler.Mode == MyAssemblerMode.Disassembly)
                {
                    reports.Add(new ModuleReport()
                    {
                        ModuleName = Name,
                        Level = ReportLevel.Info,
                        Message = "Assembler is currently being used for disassembly, can't work."
                    });
                }

                if(parStocks.Count == 0)
                {
                    reports.Add(new ModuleReport()
                    {
                        ModuleName = Name,
                        Level = ReportLevel.Error,
                        Message = "Unable to manage stocks because no par stocks have been configured."
                    });
                }

                return reports;
            }

            private void MoveAssemblerOutput(IMyInventory dest)
            {
                var assemblers = new List<IMyAssembler>();
                grid.GetBlocksOfType(assemblers);

                foreach (var assembler in assemblers)
                    if (assembler.Mode != MyAssemblerMode.Disassembly)
                        Util.MoveWholeInventory(assembler.OutputInventory, dest);
            }

            private void DetermineComponentStock(Dictionary<string, MyFixedPoint> stock)
            {
                var allBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocks(allBlocks);

                foreach (var block in allBlocks)
                {
                    if (!block.HasInventory) continue;

                    var inventory = block.GetInventory();
                    if (inventory == null || inventory.ItemCount == 0) continue;

                    for (int i = 0; i < inventory.ItemCount; i++)
                    {
                        var item = inventory.GetItemAt(i);
                        if (!item.HasValue || !IsComponent(item.Value.Type)) continue;

                        var key = Util.GetItemName(item.Value.Type);
                        Util.AddStockItem(stock, key, item.Value.Amount);                        
                    }
                }

                AddAssemblerQueueToStock(stock);
            }

            private void AddAssemblerQueueToStock(Dictionary<string, MyFixedPoint> stock)
            {
                var assemblers = new List<IMyAssembler>();
                grid.GetBlocksOfType(assemblers);

                foreach (var assembler in assemblers)
                {
                    if (assembler.IsQueueEmpty) continue;

                    var items = new List<MyProductionItem>();
                    assembler.GetQueue(items);

                    foreach (var item in items)
                    {
                        var key = Util.GetNameFromDefinition(item.BlueprintId);
                        Util.AddStockItem(stock, key, item.Amount);                        
                    }

                }
            }

            private void FindPrimaryAssembler()
            {
                var assemblers = new List<IMyAssembler>();
                grid.GetBlocksOfType(assemblers);

                assembler = assemblers.FirstOrDefault(a => !a.CooperativeMode);
            }

            private static bool IsComponent(MyItemType type)
            {
                return type.TypeId.Contains("Component");
            }

            private static string GetStockLine(string component, string stock, string par)
            {
                return $"{Util.ToFixedLength(component, 25)} {Util.ToFixedLength(stock, 7)} / {Util.ToFixedLength(par, 7)}\n";
            }
        }
    }
}
