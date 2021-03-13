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
                    IdentifyingModKey = "HalgariSoulGemDisenchantment.esp",
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
                    (!npc.Template.IsNull ||
                     !npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits))
                    && races.ContainsKey(npc.Race.FormKey))
                .Select(npc => npc.DeepCopy())
                .ToArray();
            
            Console.WriteLine($"Found {npcs.Length} NPCs");
            state.PatchMod.Npcs.Set(npcs);
            

            var headParts = state.LoadOrder.PriorityOrder.HeadPart()
                .WinningOverrides()
                .NoStockRecords()
                .Select(headPart => headPart.DeepCopy())
                .ToArray();

            Console.WriteLine($"Found {headParts.Length} Head Parts");
            state.PatchMod.HeadParts.Set(headParts);
            
            var colors = state.LoadOrder.PriorityOrder.ColorRecord()
                .WinningOverrides()
                .NoStockRecords()
                .Select(color => color.DeepCopy())
                .ToArray();

            Console.WriteLine($"Found {colors.Length} Colors");
            state.PatchMod.Colors.Set(colors);
        }
    }
}