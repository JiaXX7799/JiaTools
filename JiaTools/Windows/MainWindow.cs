using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace JiaTools.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Configuration config;
    private static readonly Dictionary<uint, Vector2> OverlayPositions = new();
    private static readonly List<GameObjectInfo> CachedGameObjects = [];
    private static readonly Dictionary<Vector2, List<GameObjectInfo>> GroupedObjects = new();
    private static readonly Dictionary<Vector2, int> GroupCurrentPage = new();

    public MainWindow(Configuration config) : base(
        "JiaTools",
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground |
        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public void Dispose() { }

    public void OnUpdate()
    {
        if (!config.Enabled) return;
        if (!IsScreenReady() || BetweenAreas) return;

        var localPlayer = DService.ObjectTable.LocalPlayer;
        if (localPlayer == null) return;

        CachedGameObjects.Clear();
        OverlayPositions.Clear();
        GroupedObjects.Clear();

        foreach (var obj in DService.ObjectTable)
        {
            if (obj.EntityId == 0) continue;
            if (Vector3.Distance(localPlayer.Position, obj.Position) > config.Range) continue;
            if (!ShouldShowObject(obj)) continue;

            if (!DService.Gui.WorldToScreen(obj.Position, out var screenPos)) continue;
            var objInfo = CreateGameObjectInfo(obj);
            CachedGameObjects.Add(objInfo);
            OverlayPositions[obj.EntityId] = screenPos;
            if (CachedGameObjects.Count >= config.MaxObjects) break;
        }

        foreach (var objInfo in CachedGameObjects)
        {
            if (!OverlayPositions.TryGetValue(objInfo.EntityID, out var screenPos)) continue;

            Vector2? nearestGroup = null;
            var minDistance = float.MaxValue;

            foreach (var existingGroup in GroupedObjects.Keys)
            {
                var distance = Vector2.Distance(screenPos, existingGroup);
                if (!(distance < config.MergeDistance) || !(distance < minDistance)) continue;
                minDistance = distance;
                nearestGroup = existingGroup;
            }

            if (nearestGroup.HasValue)
                GroupedObjects[nearestGroup.Value].Add(objInfo);
            else
                GroupedObjects[screenPos] = [objInfo];
        }
    }

    public override void Draw()
    {
        if (!config.Enabled) return;
        if (!IsScreenReady() || BetweenAreas) return;
        if (CachedGameObjects.Count == 0) return;

        var drawList = ImGui.GetForegroundDrawList();

        foreach (var (groupPos, objects) in GroupedObjects)
        {
            if (objects.Count == 0) continue;

            GroupCurrentPage.TryAdd(groupPos, 0);

            var currentPage = GroupCurrentPage[groupPos];
            if (currentPage >= objects.Count)
            {
                currentPage = 0;
                GroupCurrentPage[groupPos] = 0;
            }

            var objInfo = objects[currentPage];
            var (bgMin, bgMax, lineRects) = DrawObjectInfoAt(drawList, objInfo, groupPos, objects.Count, currentPage);

            ImGui.SetNextWindowPos(bgMin);
            ImGui.SetNextWindowSize(bgMax - bgMin);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
            ImGui.PushStyleColor(ImGuiCol.Border, 0);

            if (ImGui.Begin($"##Interact_{groupPos.X}_{groupPos.Y}",
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var mousePos = ImGui.GetMousePos();
                    var clickedLine = false;

                    foreach (var (lineMin, lineMax, copyValue) in lineRects)
                    {
                        if (mousePos.X >= lineMin.X && mousePos.X <= lineMax.X &&
                            mousePos.Y >= lineMin.Y && mousePos.Y <= lineMax.Y &&
                            !string.IsNullOrEmpty(copyValue))
                        {
                            ImGui.SetClipboardText(copyValue);
                            DService.Chat.Print($"已复制: {copyValue}");
                            clickedLine = true;
                            break;
                        }
                    }

                    if (!clickedLine && objects.Count > 1)
                        GroupCurrentPage[groupPos] = (currentPage + 1) % objects.Count;
                }
                ImGui.End();
            }

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();
        }
    }

    private (Vector2 bgMin, Vector2 bgMax, List<(Vector2 lineMin, Vector2 lineMax, string copyValue)> lineRects)
        DrawObjectInfoAt(ImDrawListPtr drawList, GameObjectInfo objInfo, Vector2 position, int totalCount = 1, int currentIndex = 0)
    {
        var lines = new List<(string text, Vector4 color, string copyValue)>();

        if (totalCount > 1)
            lines.Add(($"[{currentIndex + 1}/{totalCount}] 点击切换", Cyan, ""));

        lines.Add((objInfo.Name, Yellow, objInfo.Name));
        lines.Add(($"类型: {objInfo.ObjectKind}", White, objInfo.ObjectKind.ToString()));

        if (config.ShowEntityID)
            lines.Add(($"EntityID: {objInfo.EntityID}", White, objInfo.EntityID.ToString()));

        if (config.ShowDataID)
            lines.Add(($"DataID: {objInfo.DataID}", White, objInfo.DataID.ToString()));

        if (config.ShowPosition)
            lines.Add(($"位置: {objInfo.Position.X:F1}, {objInfo.Position.Y:F1}, {objInfo.Position.Z:F1}", White,
                $"{objInfo.Position.X:F1}, {objInfo.Position.Y:F1}, {objInfo.Position.Z:F1}"));

        if (config.ShowRotation)
        {
            var radians = objInfo.Rotation;
            var degrees = radians * 180.0 / Math.PI % 360.0;
            if (degrees < 0)
                degrees += 360.0;

            lines.Add(($"旋转: {objInfo.Rotation:F3} ({degrees:F2}°)", White, objInfo.Rotation.ToString("F3")));
        }

        if (config.ShowDistance && objInfo.Distance >= 0)
            lines.Add(($"距离: {objInfo.Distance:F1}m", White, objInfo.Distance.ToString("F1")));

        if (config.ShowHealth && objInfo.CurrentHP > 0)
        {
            var healthPercentage = objInfo.MaxHP > 0 ? (float)objInfo.CurrentHP / objInfo.MaxHP : 0f;
            var healthColor = healthPercentage switch
            {
                > 0.7f => Green,
                > 0.3f => Yellow,
                _ => Red
            };
            lines.Add(($"生命值: {objInfo.CurrentHP:N0}/{objInfo.MaxHP:N0} ({healthPercentage:P0})", healthColor,
                $"{objInfo.CurrentHP}/{objInfo.MaxHP}"));
        }

        if (config.ShowCastInfo && objInfo.IsCasting)
        {
            lines.Add(("正在咏唱", Orange, ""));

            var actionName = "";
            if (LuminaGetter.TryGetRow<Lumina.Excel.Sheets.Action>(objInfo.CastActionID, out var actionRow))
                actionName = actionRow.Name.ExtractText();

            var actionText = !string.IsNullOrEmpty(actionName)
                ? $"咏唱技能: {objInfo.CastActionID} ({actionName})"
                : $"咏唱技能: {objInfo.CastActionID}";

            lines.Add((actionText, Orange, objInfo.CastActionID.ToString()));

            if (objInfo.CastRotation.HasValue)
            {
                var castDegrees = objInfo.CastRotation.Value * 180.0 / Math.PI % 360.0;
                if (castDegrees < 0)
                    castDegrees += 360.0;
                lines.Add(($"咏唱朝向: {objInfo.CastRotation.Value:F3} ({castDegrees:F2}°)", Orange,
                    objInfo.CastRotation.Value.ToString("F3")));
            }

            if (!string.IsNullOrEmpty(objInfo.CastTargetName))
                lines.Add(($"咏唱目标: {objInfo.CastTargetName}", Orange, objInfo.CastTargetName));

            var castProgress = objInfo.TotalCastTime > 0 ? objInfo.CurrentCastTime / objInfo.TotalCastTime : 0f;
            lines.Add(($"咏唱时间: {objInfo.CurrentCastTime:F1}s / {objInfo.TotalCastTime:F1}s ({castProgress:P0})", Orange,
                $"{objInfo.CurrentCastTime:F1}/{objInfo.TotalCastTime:F1}"));
        }

        if (config.ShowStatusList && objInfo.StatusEffects.Count > 0)
        {
            lines.Add(($"状态效果 ({objInfo.StatusEffects.Count}):", Cyan, ""));
            foreach (var status in objInfo.StatusEffects.Take(5))
            {
                var timeColor = status.RemainingTime < 5 ? Red : White;

                var statusName = "";
                if (LuminaGetter.TryGetRow<Lumina.Excel.Sheets.Status>(status.StatusID, out var statusRow))
                    statusName = statusRow.Name.ExtractText();

                var statusText = !string.IsNullOrEmpty(statusName)
                    ? $"  {status.StatusID} ({statusName}): {status.RemainingTime:F1}s"
                    : $"  {status.StatusID}: {status.RemainingTime:F1}s";

                if (status.Param > 0)
                    statusText += $" [{status.Param}]";

                lines.Add((statusText, timeColor, status.StatusID.ToString()));
            }
        }

        if (lines.Count == 0) return (Vector2.Zero, Vector2.Zero, []);

        var fontSize = 13f * config.FontScale;
        var lineHeight = fontSize + 2f;
        var maxWidth = fontSize * 18;
        var totalHeight = lines.Count * lineHeight + 8f;
        var bgMin = position + new Vector2(-4, -4);
        var bgMax = position + new Vector2(maxWidth + 4, totalHeight + 4);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, config.Opacity * 0.85f));
        var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, config.Opacity * 0.7f));

        drawList.AddRectFilled(bgMin, bgMax, bgColor);
        drawList.AddRect(bgMin, bgMax, borderColor, 2f);

        var currentPos = position;
        var mousePos = ImGui.GetMousePos();
        var lineRects = new List<(Vector2 lineMin, Vector2 lineMax, string copyValue)>();

        foreach (var (text, color, copyValue) in lines)
        {
            var finalColor = color with { W = config.Opacity };
            var textColor = ImGui.ColorConvertFloat4ToU32(finalColor);

            var font = ImGui.GetFont();
            var scaledSize = font.FontSize * config.FontScale;

            var lineMin = new Vector2(bgMin.X, currentPos.Y);
            var lineMax = bgMax with { Y = currentPos.Y + lineHeight };

            lineRects.Add((lineMin, lineMax, copyValue));

            if (!string.IsNullOrEmpty(copyValue) &&
                mousePos.X >= lineMin.X && mousePos.X <= lineMax.X &&
                mousePos.Y >= lineMin.Y && mousePos.Y <= lineMax.Y)
                drawList.AddRectFilled(lineMin, lineMax,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.1f)));

            drawList.AddText(font, scaledSize, currentPos, textColor, text);
            currentPos.Y += lineHeight;
        }

        return (bgMin, bgMax, lineRects);
    }

    private bool ShouldShowObject(IGameObject obj)
    {
        var localPlayer = DService.ObjectTable.LocalPlayer;

        return obj.ObjectKind switch
        {
            ObjectKind.Player when obj.Equals(localPlayer) => config.ShowLocalPlayer,
            ObjectKind.Player => config.ShowPlayers,
            ObjectKind.BattleNpc => config.ShowBattleNpcs,
            ObjectKind.EventNpc => config.ShowEventNpcs,
            ObjectKind.EventObj => config.ShowEventObjs,
            _ => false
        };
    }

    private static GameObjectInfo CreateGameObjectInfo(IGameObject obj)
    {
        var localPlayer = DService.ObjectTable.LocalPlayer;
        var distance = localPlayer != null ? Vector3.Distance(localPlayer.Position, obj.Position) : -1f;

        var objInfo = new GameObjectInfo
        {
            EntityID = obj.EntityId,
            DataID = obj.DataId,
            Name = obj.Name.TextValue,
            ObjectKind = obj.ObjectKind,
            Position = obj.Position,
            Rotation = obj.Rotation,
            Distance = distance
        };

        if (obj is not IBattleChara battleChara) return objInfo;
        objInfo.CurrentHP = battleChara.CurrentHp;
        objInfo.MaxHP = battleChara.MaxHp;
        objInfo.IsCasting = battleChara.IsCasting;
        objInfo.CastActionID = battleChara.CastActionId;
        objInfo.CurrentCastTime = battleChara.CurrentCastTime;
        objInfo.TotalCastTime = battleChara.TotalCastTime;

        if (battleChara.IsCasting)
        {
            unsafe
            {
                var character = (Character*)obj.Address;
                objInfo.CastRotation = character->CastRotation;
            }

            var castTargetID = battleChara.CastTargetObjectId;
            if (castTargetID != 0)
            {
                var castTarget = DService.ObjectTable.SearchById(castTargetID);
                objInfo.CastTargetName = castTarget?.Name.TextValue ?? $"ID:{castTargetID}";
            }
            else
                objInfo.CastTargetName = "无目标";
        }

        objInfo.StatusEffects.Clear();
        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId == 0) continue;
            objInfo.StatusEffects.Add(new StatusInfo
            {
                StatusID = status.StatusId,
                RemainingTime = status.RemainingTime,
                Param = status.Param
            });
        }

        return objInfo;
    }

    private class GameObjectInfo
    {
        public uint EntityID { get; init; }
        public uint DataID { get; init; }
        public string Name { get; init; } = string.Empty;
        public ObjectKind ObjectKind { get; init; }
        public Vector3 Position { get; init; }
        public float Rotation { get; init; }
        public float Distance { get; init; }
        public uint CurrentHP { get; set; }
        public uint MaxHP { get; set; }
        public bool IsCasting { get; set; }
        public uint CastActionID { get; set; }
        public float CurrentCastTime { get; set; }
        public float TotalCastTime { get; set; }
        public string CastTargetName { get; set; } = string.Empty;
        public List<StatusInfo> StatusEffects { get; } = [];
        public float? CastRotation { get; set; }
    }

    private class StatusInfo
    {
        public uint StatusID { get; init; }
        public float RemainingTime { get; init; }
        public ushort Param { get; init; }
    }
}
