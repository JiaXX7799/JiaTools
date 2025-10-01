using System;
using Dalamud.Interface.Windowing;

namespace JiaTools.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;

    public ConfigWindow(Configuration config) : base("JiaTools 设置")
    {
        this.config = config;
        Size = new System.Numerics.Vector2(500, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("设置");
        ImGui.Separator();

        // 主开关
        var enabled = config.Enabled;
        if (ImGui.Checkbox("启用悬浮窗", ref enabled))
        {
            config.Enabled = enabled;
            config.Save();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("可以使用 /jtools 命令快速切换");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // 基础设置
        var opacity = config.Opacity;
        if (ImGui.SliderFloat("透明度", ref opacity, 0.1f, 1.0f))
        {
            config.Opacity = opacity;
            config.Save();
        }

        var range = config.Range;
        if (ImGui.SliderFloat("范围", ref range, 5f, 100f))
        {
            config.Range = range;
            config.Save();
        }

        var fontScale = config.FontScale;
        if (ImGui.SliderFloat("字体缩放", ref fontScale, 0.5f, 2.0f))
        {
            config.FontScale = fontScale;
            config.Save();
        }

        var maxObjects = config.MaxObjects;
        if (ImGui.SliderInt("最大对象数", ref maxObjects, 1, 100))
        {
            config.MaxObjects = maxObjects;
            config.Save();
        }

        var mergeDistance = config.MergeDistance;
        if (ImGui.SliderFloat("合并距离（像素）", ref mergeDistance, 10f, 200f))
        {
            config.MergeDistance = mergeDistance;
            config.Save();
        }

        ImGui.Spacing();

        // 对象类型选择
        if (ImGui.CollapsingHeader("对象类型", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Columns(2, "ObjectTypes", false);

            var showPlayers = config.ShowPlayers;
            if (ImGui.Checkbox("显示玩家", ref showPlayers))
            {
                config.ShowPlayers = showPlayers;
                config.Save();
            }

            var showEventNpcs = config.ShowEventNpcs;
            if (ImGui.Checkbox("显示EventNpc", ref showEventNpcs))
            {
                config.ShowEventNpcs = showEventNpcs;
                config.Save();
            }

            var showEventObjs = config.ShowEventObjs;
            if (ImGui.Checkbox("显示EventObj", ref showEventObjs))
            {
                config.ShowEventObjs = showEventObjs;
                config.Save();
            }

            ImGui.NextColumn();

            var showLocalPlayer = config.ShowLocalPlayer;
            if (ImGui.Checkbox("显示本地玩家", ref showLocalPlayer))
            {
                config.ShowLocalPlayer = showLocalPlayer;
                config.Save();
            }

            var showBattleNpcs = config.ShowBattleNpcs;
            if (ImGui.Checkbox("显示BattleNpc", ref showBattleNpcs))
            {
                config.ShowBattleNpcs = showBattleNpcs;
                config.Save();
            }

            ImGui.Columns();
        }

        ImGui.Spacing();

        // 显示选项
        if (ImGui.CollapsingHeader("显示选项", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Columns(2, "DisplayOptions", false);

            var showEntityID = config.ShowEntityID;
            if (ImGui.Checkbox("显示EntityID", ref showEntityID))
            {
                config.ShowEntityID = showEntityID;
                config.Save();
            }

            var showPosition = config.ShowPosition;
            if (ImGui.Checkbox("显示位置", ref showPosition))
            {
                config.ShowPosition = showPosition;
                config.Save();
            }

            var showDistance = config.ShowDistance;
            if (ImGui.Checkbox("显示距离", ref showDistance))
            {
                config.ShowDistance = showDistance;
                config.Save();
            }

            var showStatusList = config.ShowStatusList;
            if (ImGui.Checkbox("显示状态列表", ref showStatusList))
            {
                config.ShowStatusList = showStatusList;
                config.Save();
            }

            ImGui.NextColumn();

            var showDataID = config.ShowDataID;
            if (ImGui.Checkbox("显示DataID", ref showDataID))
            {
                config.ShowDataID = showDataID;
                config.Save();
            }

            var showRotation = config.ShowRotation;
            if (ImGui.Checkbox("显示旋转", ref showRotation))
            {
                config.ShowRotation = showRotation;
                config.Save();
            }

            var showCastInfo = config.ShowCastInfo;
            if (ImGui.Checkbox("显示咏唱信息", ref showCastInfo))
            {
                config.ShowCastInfo = showCastInfo;
                config.Save();
            }

            var showHealth = config.ShowHealth;
            if (ImGui.Checkbox("显示生命值", ref showHealth))
            {
                config.ShowHealth = showHealth;
                config.Save();
            }

            ImGui.Columns();
        }

        ImGui.Spacing();
    }
}
