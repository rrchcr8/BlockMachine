namespace BlockMachine.Services;

public static class AdminPortalService
{
    public const string OpenAdminEventName = "BlockMachine_OpenAdmin_Global";

    public static bool IsAdminLaunch(string[] args)
    {
        return args.Any(arg => string.Equals(arg, "--admin", StringComparison.OrdinalIgnoreCase));
    }

    public static bool SignalOpenAdminPortal()
    {
        try
        {
            using var openEvent = EventWaitHandle.OpenExisting(OpenAdminEventName);
            return openEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    public static EventWaitHandle CreateOpenAdminListener()
    {
        return new EventWaitHandle(false, EventResetMode.AutoReset, OpenAdminEventName);
    }
}
