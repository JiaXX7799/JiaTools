using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using JiaTools.Windows;
using KamiToolKit;

namespace JiaTools;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "JiaTools";

    private readonly WindowSystem windowSystem;
    private readonly Configuration configuration;
    private readonly MainWindow mainWindow;
    private readonly NativeController nativeController;
    private readonly NativeConfigWindow nativeConfigWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            DService.Init(pluginInterface);

            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            configuration.Save();

            // Initialize NativeController for Native UI support
            nativeController = new NativeController(pluginInterface);

            windowSystem = new WindowSystem("JiaTools");

            mainWindow = new MainWindow(configuration);

            windowSystem.AddWindow(mainWindow);

            // Initialize Native Config Window
            nativeConfigWindow = new NativeConfigWindow(configuration)
            {
                InternalName = "JiaToolsConfig",
                Title = "JiaTools 配置",
                Size = new Vector2(350.0f, 550.0f),
                Position = new Vector2(100.0f, 100.0f),
                NativeController = nativeController,
            };

            DService.UIBuilder.Draw += windowSystem.Draw;
            DService.UIBuilder.OpenConfigUi += () => nativeConfigWindow.Toggle();
            DService.UIBuilder.OpenMainUi += () =>
            {
                configuration.Enabled = !configuration.Enabled;
                configuration.Save();
            };

            DService.Framework.Update += OnFrameworkUpdate;

            // 注册命令
            DService.Command.AddHandler("/jtools", new Dalamud.Game.Command.CommandInfo(OnCommand)
            {
                HelpMessage = "切换 JiaTools 悬浮窗的开启/关闭"
            });
            
            DService.Command.AddHandler("/jconfig", new Dalamud.Game.Command.CommandInfo(OnConfigCommand)
            {
                HelpMessage = "打开 JiaTools 配置窗口"
            });
        }
        catch (Exception ex)
        {
            if (DService.Log != null)
                DService.Log.Error(ex, "Failed to initialize JiaTools");
            else
                throw new InvalidOperationException("Failed to initialize JiaTools and logging is unavailable", ex);
            throw;
        }
    }

    private void OnCommand(string command, string args)
    {
        configuration.Enabled = !configuration.Enabled;
        configuration.Save();

        var status = configuration.Enabled ? "已开启" : "已关闭";
        DService.Chat?.Print($"[JiaTools] 悬浮窗{status}");
    }

    private void OnConfigCommand(string command, string args)
    {
        nativeConfigWindow.Toggle();
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        mainWindow.OnUpdate();
    }

    public void Dispose()
    {
        if (DService.Command != null)
        {
            DService.Command.RemoveHandler("/jtools");
            DService.Command.RemoveHandler("/jconfig");
        }

        if (DService.Framework != null)
            DService.Framework.Update -= OnFrameworkUpdate;

        // Dispose Native UI components
        nativeConfigWindow.Dispose();
        nativeController.Dispose();

        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        DService.Uninit();
    }
}
