using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ruyu.WinterPlants
{
    //Patch to change descriptions of winter plants
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    public static class WinterPlantsDescPatch
    {
        [HarmonyPostfix]
        public static void PlantDescPatch(ref IEnumerable<StatDrawEntry> __result, ThingDef __instance)
        {
            if (__instance.HasModExtension<WinterPlantMod>())
                __result = PassthroughMethod(__result, __instance);
        }
        public static IEnumerable<StatDrawEntry> PassthroughMethod(IEnumerable<StatDrawEntry> __result, ThingDef __instance)
        {
            float minTemp = -58f;
            float maxTemp = 20f;
            float growMinGlow = 1f;
            foreach (StatDrawEntry entry in __result)
            {
                if (entry.displayOrderWithinCategory == 4152)
                {
                    yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                        "MinGrowthTemperature".Translate(), minTemp.ToStringTemperature(),
                        "Stat_Thing_Plant_MinGrowthTemperature_Desc".Translate(), 4152);
                }
                else if (entry.displayOrderWithinCategory == 4153)
                {
                    yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                        "MaxGrowthTemperature".Translate(), maxTemp.ToStringTemperature(),
                        "Stat_Thing_Plant_MaxGrowthTemperature_Desc".Translate(), 4153);
                }
                else if (entry.displayOrderWithinCategory == 4154)
                {
                    yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                        "LightRequirement".Translate(), growMinGlow.ToStringPercent(),
                        "Stat_Thing_Plant_LightRequirement_Desc".Translate(), 4154);
                }
                else
                {
                    yield return entry;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlantUtility), nameof(PlantUtility.GrowthSeasonNow))]
    public static class GrowthSeasonNowPatch
    {
        [HarmonyPrefix]
        public static bool Patch_GrowthSeasonNow(ref bool __result, ref IntVec3 c, Map map, bool forSowing)
        {
            ThingDef thingDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
            if (thingDef != null && thingDef.thingClass == typeof(WinterPlant))
            {
                __result = GrowthSeasonNowWinterPlant(c, map); ;
                return false;
            }
            return true;
        }
        public static bool GrowthSeasonNowWinterPlant(IntVec3 c, Map map)
        {
            Room roomOrAdjacent = c.GetRoomOrAdjacent(map, RegionType.ImpassableFreeAirExchange | RegionType.Normal | RegionType.Portal);
            if (roomOrAdjacent == null)
            {
                return false;
            }
            float temperature = c.GetTemperature(map);
            if (temperature < -58f)
                return false;
            else if (temperature > 20f)
                return false;
            else
                return true;
        }
        [HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.GetInspectString))]
        public static class ZoneInspectStringPatch
        {
            [HarmonyPrefix]
            public static bool ZoneGetInspectStringPatch(ref Zone_Growing __instance, ref string __result)
            {
                ThingDef plantDefToGrow = __instance.GetPlantDefToGrow();
                if (plantDefToGrow.thingClass != typeof(WinterPlant))
                {
                    return true;
                }
                string text = "";
                if (!__instance.cells.NullOrEmpty())
                {
                    IntVec3 c = __instance.cells.First();
                    if (c.UsesOutdoorTemperature(__instance.Map) && GrowthSeasonNowWinterPlant(c, __instance.Map))
                    {
                        string text2 = text;
                        text = text2 + "(" + "ColdImmuneWinterPlants".Translate() + ")\n";
                    }
                    text = (__result = ((!GrowthSeasonNowWinterPlant(c, __instance.Map)) ? (text + "CannotGrowBadSeasonTemperature".Translate()) : (text + "GrowSeasonHereNow".Translate())));
                }
                return false;
            }
        }
    }
    [HarmonyPatch(typeof(WildPlantSpawner), nameof(WildPlantSpawner.CanRegrowAt))]
    public static class CanRegrowAt_Patch
    {
        [HarmonyPostfix]
        public static void RegrowOnCold(IntVec3 c, Map ___map, ref bool __result, ref WildPlantSpawner __instance)
        {
            if (___map.Biome == BiomeDefOf.Tundra || ___map.Biome == BiomeDefOf.IceSheet)
            {
                __result = !c.Roofed(___map);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class WinterPlantPatch
    {
        static WinterPlantPatch()
        {
            Harmony val = new Harmony("Ruyu.WinterPlants");
            val.PatchAll();
        }
    }
}
