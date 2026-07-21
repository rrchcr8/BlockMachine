using System.IO;

namespace BlockMachine.Services;

public static class ShortcutService
{
    public const string AdminShortcutName = "Block Machine - Administración.lnk";

    public static string GetAdminDesktopShortcutPath()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return Path.Combine(desktop, AdminShortcutName);
    }

    public static void CreateAdminDesktopShortcut(string? executablePath = null)
    {
        executablePath ??= ResolveExecutablePath();
        var shortcutPath = GetAdminDesktopShortcutPath();
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;

        CreateShortcut(
            shortcutPath,
            executablePath,
            "--admin",
            workingDirectory,
            "Block Machine - Panel de administración (requiere contraseña)");
    }

    public static void RemoveAdminDesktopShortcut()
    {
        var shortcutPath = GetAdminDesktopShortcutPath();
        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    public static bool AdminDesktopShortcutExists()
    {
        return File.Exists(GetAdminDesktopShortcutPath());
    }

    public static string ResolveExecutablePath()
    {
        return Environment.ProcessPath
               ?? Path.Combine(AppContext.BaseDirectory, "BlockMachine.exe");
    }

    private static void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string arguments,
        string workingDirectory,
        string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new InvalidOperationException("No se pudo acceder al shell de Windows.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = description;
        shortcut.WindowStyle = 1;
        shortcut.Save();
    }
}
