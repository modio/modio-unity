namespace Modio.Reports
{
    public enum ModNotWorkingReason
    {
        None = 0,
        CrashesGame = 1,
        DoesNotLoad = 2,
        ConflictsWithOtherMods = 3,
        MissingDependencies = 4,
        InstallationIssues = 5,
        BuggyBehaviour = 6,
        IncompatibleWithGameVersion = 7,
        FileCorruption = 8,
    }
}
