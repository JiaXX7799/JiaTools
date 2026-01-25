using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace JiaTools.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly ObjectListWindow objectListWindow;
    private readonly CleanBackgroundManager? backgroundManager;

    public ConfigWindow(Configuration config, ObjectListWindow objectListWindow) : base(
        "JiaTools 配置###JiaToolsConfig",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar)
    {
        this.config = config;
        this.objectListWindow = objectListWindow;
        Size = new Vector2(400, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 520),
            MaximumSize = new Vector2(1000, 1000)
        };

        try
        {
            backgroundManager = new CleanBackgroundManager(DService.Instance().Log);
            backgroundManager.Initialize();
            DService.Instance().Log.Information("磨砂玻璃背景管理器初始化成功");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error(ex, "磨砂玻璃背景管理器初始化失败");
            backgroundManager = null;
        }
    }

    public void Dispose()
    {
        backgroundManager?.Dispose();
    }

    public override void PreDraw()
    {
        if (config.UseFrostedGlass)
        {
            Flags |= ImGuiWindowFlags.NoBackground;
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0));
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoBackground;
        }
    }

    public override void PostDraw()
    {
        if (config.UseFrostedGlass)
        {
            ImGui.PopStyleColor();
        }
    }

    public override void Draw()
    {
        if (config.UseFrostedGlass && backgroundManager != null)
        {
            try
            {
                backgroundManager.DrawBackground(config.Opacity);
            }
            catch (Exception ex)
            {
                DService.Instance().Log.Error(ex, "绘制磨砂背景时出错");
            }
        }

        DrawCustomTitleBar();
        DrawHeader();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("##ConfigTabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("常规设置"))
            {
                DrawGeneralSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("对象类型"))
            {
                DrawObjectTypeSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("显示选项"))
            {
                DrawDisplaySettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawCustomTitleBar()
    {
        var titleBarHeight = 35f;
        var windowPos = ImGui.GetWindowPos();
        var windowWidth = ImGui.GetWindowWidth();

        var titleBarMin = windowPos;
        var titleBarMax = new Vector2(windowPos.X + windowWidth, windowPos.Y + titleBarHeight);

        var drawList = ImGui.GetWindowDrawList();
        var titleBgColor = config.UseFrostedGlass
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.2f, 0.3f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.15f, 0.9f));

        drawList.AddRectFilled(titleBarMin, titleBarMax, titleBgColor);

        var titleText = "JiaTools 配置";
        var titleSize = ImGui.CalcTextSize(titleText);
        var titlePos = new Vector2(
            windowPos.X + (windowWidth - titleSize.X) * 0.5f,
            windowPos.Y + (titleBarHeight - titleSize.Y) * 0.5f
        );

        var titleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.9f, 1.0f, 1.0f));
        drawList.AddText(titlePos, titleColor, titleText);

        var closeButtonSize = 25f;
        var closeButtonPos = new Vector2(windowPos.X + windowWidth - closeButtonSize - 5f, windowPos.Y + 5f);
        var closeButtonMin = closeButtonPos;
        var closeButtonMax = new Vector2(closeButtonPos.X + closeButtonSize, closeButtonPos.Y + closeButtonSize);

        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= closeButtonMin.X && mousePos.X <= closeButtonMax.X &&
                       mousePos.Y >= closeButtonMin.Y && mousePos.Y <= closeButtonMax.Y;

        var closeButtonColor = isHovered
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 0.8f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 0.6f));

        drawList.AddRectFilled(closeButtonMin, closeButtonMax, closeButtonColor, 3f);

        var crossSize = 12f;
        var crossCenter = new Vector2(
            closeButtonPos.X + closeButtonSize * 0.5f,
            closeButtonPos.Y + closeButtonSize * 0.5f
        );
        var crossColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));

        drawList.AddLine(
            new Vector2(crossCenter.X - crossSize * 0.35f, crossCenter.Y - crossSize * 0.35f),
            new Vector2(crossCenter.X + crossSize * 0.35f, crossCenter.Y + crossSize * 0.35f),
            crossColor, 2f
        );
        drawList.AddLine(
            new Vector2(crossCenter.X + crossSize * 0.35f, crossCenter.Y - crossSize * 0.35f),
            new Vector2(crossCenter.X - crossSize * 0.35f, crossCenter.Y + crossSize * 0.35f),
            crossColor, 2f
        );

        if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            IsOpen = false;
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + titleBarHeight);

        if (ImGui.IsMouseHoveringRect(titleBarMin, new Vector2(titleBarMax.X - 35f, titleBarMax.Y)) &&
            ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left);
            ImGui.SetWindowPos(new Vector2(windowPos.X + delta.X, windowPos.Y + delta.Y));
            ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
        }
    }

    private void DrawHeader()
    {
        var enabled = config.Enabled;
        var headerColor = config.Enabled
            ? new Vector4(0.4f, 0.8f, 0.4f, 1.0f)
            : new Vector4(0.8f, 0.4f, 0.4f, 1.0f);

        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        if (ImGui.Checkbox("##EnableToggle", ref enabled))
        {
            config.Enabled = enabled;
            config.Save();
        }
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        ImGui.Text(config.Enabled ? "● 悬浮窗已启用" : "○ 悬浮窗已禁用");
        ImGui.PopStyleColor();

        if (config.Enabled)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(所有设置立即生效)");
        }

        ImGui.SameLine();
        var buttonPosX = ImGui.GetWindowWidth() - 120;
        ImGui.SetCursorPosX(buttonPosX);
        if (ImGui.Button("对象列表", new Vector2(100, 0)))
        {
            objectListWindow.Toggle();
        }
        DrawTooltip("打开对象列表窗口");
    }

    private void DrawGeneralSettings()
    {
        ImGui.BeginChild("##GeneralSettings", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysUseWindowPadding);

        DrawSectionHeader("外观设置", 0xFFE6B800);

        var opacity = (int)(config.Opacity * 100);
        ImGui.SetNextItemWidth(250);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.4f, 0.7f, 1.0f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.3f, 0.6f, 0.9f, 1.0f));
        if (ImGui.SliderInt("不透明度", ref opacity, 10, 100, "%d%%"))
        {
            config.Opacity = opacity / 100f;
            config.Save();
        }
        ImGui.PopStyleColor(2);
        DrawTooltip("调整悬浮窗的透明度\n数值越大越不透明");

        var fontScale = (int)(config.FontScale * 100);
        ImGui.SetNextItemWidth(250);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.4f, 0.7f, 1.0f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.3f, 0.6f, 0.9f, 1.0f));
        if (ImGui.SliderInt("字体缩放", ref fontScale, 50, 200, "%d%%"))
        {
            config.FontScale = fontScale / 100f;
            config.Save();
        }
        ImGui.PopStyleColor(2);
        DrawTooltip("调整文字大小\n建议范围：80% - 120%");

        var useFrostedGlass = config.UseFrostedGlass;
        if (ImGui.Checkbox("磨砂玻璃背景", ref useFrostedGlass))
        {
            config.UseFrostedGlass = useFrostedGlass;
            config.Save();
        }
        DrawTooltip("启用磨砂玻璃背景效果\n关闭则使用默认背景");

        var useMainWindowFrostedGlass = config.UseMainWindowFrostedGlass;
        if (ImGui.Checkbox("悬浮窗磨砂玻璃背景", ref useMainWindowFrostedGlass))
        {
            config.UseMainWindowFrostedGlass = useMainWindowFrostedGlass;
            config.Save();
        }
        DrawTooltip("启用悬浮窗磨砂玻璃背景效果\n关闭则使用默认背景");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();
        
        DrawSectionHeader("扫描设置", 0xFF00E6B8);

        var range = (int)config.Range;
        ImGui.SetNextItemWidth(250);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.0f, 0.9f, 0.7f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.0f, 0.8f, 0.6f, 1.0f));
        if (ImGui.SliderInt("扫描范围", ref range, 5, 100, "%d米"))
        {
            config.Range = range;
            config.Save();
        }
        ImGui.PopStyleColor(2);
        DrawTooltip("设置扫描对象的最大距离\n范围越大，性能消耗越高");

        var maxObjects = config.MaxObjects;
        ImGui.SetNextItemWidth(250);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.0f, 0.9f, 0.7f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.0f, 0.8f, 0.6f, 1.0f));
        if (ImGui.SliderInt("最大对象数", ref maxObjects, 1, 100))
        {
            config.MaxObjects = maxObjects;
            config.Save();
        }
        ImGui.PopStyleColor(2);
        DrawTooltip("同时显示的最大对象数量\n建议不超过50个");

        var mergeDistance = (int)config.MergeDistance;
        ImGui.SetNextItemWidth(250);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.0f, 0.9f, 0.7f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.0f, 0.8f, 0.6f, 1.0f));
        if (ImGui.SliderInt("合并距离", ref mergeDistance, 10, 200, "%d像素"))
        {
            config.MergeDistance = mergeDistance;
            config.Save();
        }
        ImGui.PopStyleColor(2);
        DrawTooltip("屏幕距离小于此值的对象将被合并显示\n点击可切换显示不同对象");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();

        DrawSectionHeader("DataID 筛选", 0xFFE67300);

        var enableDataIDFilter = config.EnableDataIDFilter;
        if (ImGui.Checkbox("启用 DataID 筛选", ref enableDataIDFilter))
        {
            config.EnableDataIDFilter = enableDataIDFilter;
            config.Save();
        }
        DrawTooltip("开启后，根据白名单/黑名单模式筛选对象\n关闭则显示所有对象");

        var useDataIDWhitelist = config.UseDataIDWhitelist;
        if (ImGui.Checkbox("白名单模式", ref useDataIDWhitelist))
        {
            config.UseDataIDWhitelist = useDataIDWhitelist;
            config.Save();
        }
        DrawTooltip("白名单：只显示列表中的 DataID\n黑名单：隐藏列表中的 DataID");

        ImGui.Spacing();

        var filterDataIDs = config.FilterDataIDs ?? "";
        ImGui.SetNextItemWidth(450);
        if (ImGui.InputTextWithHint("##FilterDataIDs", "输入 DataID (逗号分隔，如: 123,456,789)", ref filterDataIDs, 500))
        {
            config.FilterDataIDs = filterDataIDs;
            config.Save();
        }
        DrawTooltip("输入要筛选的 DataID\n多个 DataID 用逗号分隔\n例如：2000001,2000002,2000003");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();

        DrawSectionHeader("读条筛选", 0xFFE600B8);

        var enableCastingFilter = config.EnableCastingFilter;
        if (ImGui.Checkbox("启用读条筛选", ref enableCastingFilter))
        {
            config.EnableCastingFilter = enableCastingFilter;
            config.Save();
        }
        DrawTooltip("开启后，只显示正在读条的对象\n关闭则显示所有对象");

        ImGui.EndChild();
    }

    private void DrawObjectTypeSettings()
    {
        ImGui.BeginChild("##ObjectTypeSettings", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysUseWindowPadding);

        DrawSectionHeader("对象类型过滤", 0xFFB800E6);
        ImGui.TextWrapped("✓ 选择要在悬浮窗中显示的对象类型");
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Columns(2, "##ObjectColumns", false);
        ImGui.SetColumnWidth(0, 300);

        config.ShowPlayers = DrawStyledCheckbox("玩家", config.ShowPlayers, "显示其他玩家", new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
        config.ShowLocalPlayer = DrawStyledCheckbox("本地玩家", config.ShowLocalPlayer, "显示自己", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
        config.ShowBattleNpcs = DrawStyledCheckbox("显示战斗NPC(BattleNpc)", config.ShowBattleNpcs, "显示敌人和友方NPC", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));

        ImGui.NextColumn();

        config.ShowEventNpcs = DrawStyledCheckbox("显示事件NPC(EventNpc)", config.ShowEventNpcs, "显示任务相关NPC", new Vector4(1.0f, 0.8f, 0.4f, 1.0f));
        config.ShowEventObjs = DrawStyledCheckbox("显示事件对象(EventObj)", config.ShowEventObjs, "显示可交互的对象", new Vector4(0.8f, 0.4f, 1.0f, 1.0f));

        ImGui.Columns();

        ImGui.EndChild();
    }

    private void DrawDisplaySettings()
    {
        ImGui.BeginChild("##DisplaySettings", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysUseWindowPadding);

        DrawSectionHeader("基础信息", 0xFF00B8E6);
        ImGui.TextWrapped("✓ 选择要显示的对象信息");
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Columns(2, "##DisplayColumns", false);
        ImGui.SetColumnWidth(0, 300);

        config.ShowEntityID = DrawStyledCheckbox("EntityID", config.ShowEntityID, "对象的唯一实体ID\n", new Vector4(0.6f, 0.8f, 1.0f, 1.0f));
        config.ShowDataID = DrawStyledCheckbox("DataID", config.ShowDataID, "对象的DataID\nps: 相同类型对象共享此ID", new Vector4(0.6f, 0.8f, 1.0f, 1.0f));
        config.UseHexID = DrawStyledCheckbox("16进制ID显示", config.UseHexID, "使用16进制显示EntityID和DataID", new Vector4(1.0f, 0.8f, 0.4f, 1.0f));
        config.ShowPosition = DrawStyledCheckbox("位置坐标", config.ShowPosition, "对象的世界坐标", new Vector4(0.8f, 1.0f, 0.6f, 1.0f));
        config.ShowRotation = DrawStyledCheckbox("旋转角度", config.ShowRotation, "对象的朝向角度\n(弧度和度数)", new Vector4(0.8f, 1.0f, 0.6f, 1.0f));

        ImGui.NextColumn();

        config.ShowDistance = DrawStyledCheckbox("距离", config.ShowDistance, "与你的距离（米）", new Vector4(1.0f, 0.8f, 0.6f, 1.0f));
        config.ShowHealth = DrawStyledCheckbox("生命值", config.ShowHealth, "当前/最大生命值", new Vector4(1.0f, 0.6f, 0.6f, 1.0f));
        config.ShowMana = DrawStyledCheckbox("魔法值", config.ShowMana, "当前/最大魔法值", new Vector4(0.3f, 0.7f, 1.0f, 1.0f));
        config.ShowMarker = DrawStyledCheckbox("标记", config.ShowMarker, "对象身上的目标标记信息", new Vector4(1.0f, 0.5f, 0.8f, 1.0f));
        config.ShowCastInfo = DrawStyledCheckbox("咏唱信息", config.ShowCastInfo, "当前咏唱的技能详细信息", new Vector4(1.0f, 0.6f, 0.9f, 1.0f));
        config.ShowStatusList = DrawStyledCheckbox("状态列表", config.ShowStatusList, "Buff和Debuff\n(最多显示5个)", new Vector4(0.9f, 0.6f, 1.0f, 1.0f));

        ImGui.Columns();

        ImGui.EndChild();
    }

    private static void DrawSectionHeader(string text, uint color = 0xFFFFCC66)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text($"■ {text}");
        ImGui.PopStyleColor();
        ImGui.Spacing();
    }

    private static bool DrawStyledCheckbox(string label, bool value, string tooltip, Vector4 color)
    {
        if (string.IsNullOrEmpty(label)) return value;
        ImGui.PushStyleColor(ImGuiCol.CheckMark, color);
        var changed = ImGui.Checkbox(label, ref value);
        ImGui.PopStyleColor();
        DrawTooltip(tooltip);
        ImGui.Spacing();
        return value;
    }

    private static void DrawTooltip(string text)
    {
        if (string.IsNullOrEmpty(text) || !ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(300);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}