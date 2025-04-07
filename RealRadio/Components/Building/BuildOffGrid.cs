using RealRadio;
using ScheduleOne.Building;
using ScheduleOne.ItemFramework;

public class BuildStartOffGrid : BuildStart_Base
{
    public override void StartBuilding(ItemInstance item)
    {
        // todo
        Plugin.Logger.LogInfo($"BuildStartOffGrid.StartBuilding {item}");
    }
}

public class BuildUpdateOffGrid : BuildUpdate_Base
{
    public override void Stop()
    {
        // todo
        Plugin.Logger.LogInfo("BuildUpdateOffGrid.Stop");
    }
}

public class BuildStopOffGrid : BuildStop_Base
{
}
