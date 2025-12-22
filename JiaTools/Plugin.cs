using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using JiaTools.Windows;

namespace JiaTools;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "JiaTools";

    private readonly WindowSystem windowSystem;
    private readonly Configuration configuration;
    private readonly MainWindow mainWindow;
    private readonly ObjectListWindow objectListWindow;
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
            objectListWindow = new ObjectListWindow();
            configWindow = new ConfigWindow(configuration, objectListWindow);

            windowSystem.AddWindow(mainWindow);
            windowSystem.AddWindow(objectListWindow);
            windowSystem.AddWindow(configWindow);

            DService.UIBuilder.Draw += windowSystem.Draw;
            DService.UIBuilder.Draw += objectListWindow.DrawLineOverlay;
            DService.UIBuilder.OpenConfigUi += () => configWindow.Toggle();
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
            
            DService.Command.AddHandler("/jlist", new Dalamud.Game.Command.CommandInfo(OnListCommand)
            {
                HelpMessage = "打开 JiaTools Object List窗口"
            });

            DService.Command.AddHandler("/jdraw", new Dalamud.Game.Command.CommandInfo(OnDrawCommand)
            {
                HelpMessage = "连线指定对象 　/jdraw <EntityId1> <EntityId2>(只有一个EntityId时，EntityId1为自己)"
            });

            DService.Command.AddHandler("/jclear", new Dalamud.Game.Command.CommandInfo(OnClearDrawCommand)
            {
                HelpMessage = "清除所有连线绘图"
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
        configWindow.Toggle();
    }
    
    private void OnListCommand(string command, string args)
    {
        objectListWindow.Toggle();
    }

    private void OnDrawCommand(string command, string args)
    {
        try
        {
            var parts = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                // no para
                var localPlayer = DService.ObjectTable.LocalPlayer;
                var target = DService.Targets?.Target;

                if (localPlayer == null)
                {
                    NotificationError("当前没有可用的角色");
                    return;
                }

                if (target == null)
                {
                    NotificationWarning("当前没有选中目标");
                    return;
                }

                if (target.GameObjectID == localPlayer.EntityID)
                {
                    NotificationWarning("目标不能是自己");
                    return;
                }

                objectListWindow.SetDrawLine(true, target.GameObjectID);
                NotificationSuccess($"已设置连线：自己 → {target.Name}");
                return;
            }

            if (parts.Length == 1)
            {
                // para 1
                if (ulong.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var entityId2))
                {
                    objectListWindow.SetDrawLine(true, entityId2);
                    NotificationSuccess($"已设置连线：自己 → {entityId2:X8}");
                }
                else
                    NotificationError($"无效的 EntityID: {parts[0]}");
            }
            else if (parts.Length >= 2)
            {
                // para 2
                if (ulong.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var entityId1) &&
                    ulong.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var entityId2))
                {
                    objectListWindow.SetDrawLine(entityId1, entityId2);
                    NotificationSuccess($"已设置连线：{entityId1:X8} → {entityId2:X8}");
                }
                else
                    NotificationError("无效的 EntityID");
            }
        }
        catch (Exception ex)
        {
            DService.Log?.Error(ex, "Failed to execute /jdraw command");
            NotificationError($"命令执行失败: {ex.Message}");
        }
    }

    private void OnClearDrawCommand(string command, string args)
    {
        try
        {
            objectListWindow.ClearDrawLine();
            HelpersOm.NotificationSuccess("已清除所有连线");
        }
        catch (Exception ex)
        {
            DService.Log?.Error(ex, "Failed to execute /jclear command");
            NotificationError($"命令执行失败: {ex.Message}");
        }
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
            DService.Command.RemoveHandler("/jlist");
            DService.Command.RemoveHandler("/jdraw");
            DService.Command.RemoveHandler("/jclear");
        }

        if (DService.Framework != null)
            DService.Framework.Update -= OnFrameworkUpdate;

        if (DService.UIBuilder != null)
            DService.UIBuilder.Draw -= objectListWindow.DrawLineOverlay;

        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        objectListWindow.Dispose();
        configWindow.Dispose();
        DService.Uninit();
    }
}
