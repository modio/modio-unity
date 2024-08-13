using System.Collections.Generic;
using System.Linq;
using ModIO;
using ModIO.Implementation;

internal static class TempModSetManager
{
    static HashSet<ModId> tempModSetMods = new HashSet<ModId>();
    static readonly HashSet<ModId> removedTempModSetMods = new HashSet<ModId>();
    static bool tempModSetActive = false;

    internal static void CreateTempModSet(IEnumerable<ModId> modIds)
    {
        DeleteTempModSet();
        tempModSetActive = true;
        tempModSetMods = new HashSet<ModId>(modIds);
    }

    internal static void DeleteTempModSet()
    {
        tempModSetMods.Clear();
        removedTempModSetMods.Clear();
        tempModSetActive = false;
    }

    internal static IEnumerable<ModId> GetMods(bool includeRemovedMods = false) => includeRemovedMods ? tempModSetMods.Concat(removedTempModSetMods) : tempModSetMods;

    public static bool IsTempModSetActive() => tempModSetActive;

    internal static bool IsPartOfModSet(ModId modId)
    {
        return tempModSetMods.Contains(modId) || removedTempModSetMods.Contains(modId);
    }

    internal static bool IsUnsubscribedTempMod(ModId modId)
    {
        if (!IsPartOfModSet(modId))
            return false;

        var mods = ModIOUnity.GetSubscribedMods(out Result r);
        if (!r.Succeeded())
        {
            Logger.Log(LogLevel.Error, "Unable to get subscribed mods aborting IsTempInstall function.");
            return false;
        }

        foreach (var mod in mods)
        {
            if (mod.modProfile.id == modId)
                return false;
        }
        return true;
    }

    internal static void AddMods(IEnumerable<ModId> modIds)
    {
        foreach (var modId in modIds)
        {
            removedTempModSetMods.Remove(modId);
            tempModSetMods.Add(modId);
        }
    }

    internal static void RemoveMods(IEnumerable<ModId> modIds)
    {
        foreach (var modId in modIds)
        {
            if (tempModSetMods.Remove(modId))
                removedTempModSetMods.Add(modId);
        }
    }
}
