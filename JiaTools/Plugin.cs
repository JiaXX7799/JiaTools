using System;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using JiaTools.Windows;

namespace JiaTools;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "JiaTools";

    private readonly WindowSystem windowSystem;
    private readonly Configuration configuration;
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            DService.Init(pluginInterface);

            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            configuration.Save();

            windowSystem = new WindowSystem("JiaTools");

            mainWindow = new MainWindow(configuration);
            configWindow = new ConfigWindow(configuration);

            windowSystem.AddWindow(mainWindow);
            windowSystem.AddWindow(configWindow);

            DService.UiBuilder.Draw += windowSystem.Draw;
            DService.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;
            DService.UiBuilder.OpenMainUi += () => mainWindow.IsOpen = true;

            DService.Framework.Update += OnFrameworkUpdate;

            // 注册命令
            DService.Command.AddHandler("/jtools", new Dalamud.Game.Command.CommandInfo(OnCommand)
            {
                HelpMessage = "切换 JiaTools 悬浮窗的开启/关闭"
            });
        }
        catch (Exception ex)
        {
            DService.Log?.Error(ex, "Failed to initialize JiaTools");
            throw;
        }
    }

    private void OnCommand(string command, string args)
    {
        configuration.Enabled = !configuration.Enabled;
        configuration.Save();

        var status = configuration.Enabled ? "已开启" : "已关闭";
        DService.Chat.Print($"[JiaTools] 悬浮窗{status}");
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        mainWindow.OnUpdate();
    }

    public void Dispose()
    {
        DService.Command.RemoveHandler("/jtools");
        DService.Framework.Update -= OnFrameworkUpdate;
        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        configWindow.Dispose();
        DService.Uninit();
    }
}
