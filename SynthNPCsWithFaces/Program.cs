using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Binary;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Strings;
using System;
using System.Linq;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthNPCsWithFaces
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch).Run(args, new RunPreferences()
            {
                ActionsForEmptyArgs = new RunDefaultPatcher()
                {
                    IdentifyingModKey = "SynthNPCsWithFaces.esp",
                    TargetRelease = GameRelease.SkyrimSE,
                }
            });


        }
        
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var races = state.LoadOrder.PriorityOrder.Race()
                .WinningOverrides()
                .Where(race => race.Flags.HasFlag(Race.Flag.FaceGenHead))
                .Where(race => !race.Flags.HasFlag(Race.Flag.Child))
                .ToDictionary(race => race.FormKey);
            
            Console.WriteLine($"Found {races.Count} races");

            var npcs = state.LoadOrder.PriorityOrder.Npc()
                .WinningOverrides()
                .Where(npc =>
                    (npc.Template.IsNull ||
                     !npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits))
                    && races.ContainsKey(npc.Race.FormKey))
                .Select(npc => npc.DeepCopy())
                .ToArray();
            
            Console.WriteLine($"Found {npcs.Length} NPCs");
            state.PatchMod.Npcs.Set(npcs);
            
            var vanillaHeadParts = state.LoadOrder.PriorityOrder
                .TakeLast(Extensions.StockESMs.Count)
                .HeadPart()
                .WinningOverrides()
                .ToDictionary(r => r.FormKey);

            var headParts = state.LoadOrder.PriorityOrder.HeadPart()
                .WinningOverrides()
                .Where(headPart =>
                {
                    if (vanillaHeadParts.TryGetValue(headPart.FormKey, out var vanillaHeadPart))
                        return !vanillaHeadPart.Equals(headPart);
                    return true;
                })
                .Select(headPart => headPart.DeepCopy())
                .ToArray();

            Console.WriteLine($"Found {headParts.Length} Head Parts");
            state.PatchMod.HeadParts.Set(headParts);
            
            
            var vanillaColors = state.LoadOrder.PriorityOrder
                .TakeLast(Extensions.StockESMs.Count)
                .ColorRecord()
                .WinningOverrides()
                .ToDictionary(r => r.FormKey);
            
            var colors = state.LoadOrder.PriorityOrder.ColorRecord()
                .WinningOverrides()
                .Where(color =>
                {
                    if (vanillaColors.TryGetValue(color.FormKey, out var vanillaColor))
                        return !vanillaColor.Equals(color);
                    return true;
                })
                .Select(color => color.DeepCopy())
                .ToArray();

            Console.WriteLine($"Found {colors.Length} Colors");
            state.PatchMod.Colors.Set(colors);
        }
    }
}
