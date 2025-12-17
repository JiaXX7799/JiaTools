using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace JiaTools.Windows;

public class ObjectListWindow : Window, IDisposable
{
    private string filterText = "";
    private static readonly string[] ColumnNames = ["ObjectID", "名称", "类型", "标记", "DataID", "目标ID"];

    // draw lines variables
    private readonly List<ulong> targetObjects1 = [];
    private readonly List<ulong> targetObjects2 = [];
    private bool enableLine;
    private bool target1IsLocalPlayer;
    private bool pairwiseMode; // pair mode
    private string newTarget1Input = "";
    private string newTarget2Input = "";
    private static readonly uint LineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1));
    private static readonly uint DotColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0.5f, 1, 1));

    private bool showPlayers = true;
    private bool showBattleNpc = true;
    private bool showEventNpc = true;
    private bool showEventObj = true;
    private bool showTreasure = true;
    private bool showMountType = true;
    private bool showCompanion = true;
    private bool showOthers = true;

    public ObjectListWindow() : base(
        "对象列表###JiaToolsObjectList",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(800, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(1200, 900)
        };
        
        DService.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        if (DService.ClientState != null)
            DService.ClientState.TerritoryChanged -= OnTerritoryChanged;
        
        enableLine = false;
        targetObjects1.Clear();
        targetObjects2.Clear();
    }

    private void OnTerritoryChanged(ushort territoryId)
    {
        // TerritoryChanged -> clear draw
        try
        {
            targetObjects1.Clear();
            targetObjects2.Clear();
            target1IsLocalPlayer = false;
            DService.Log?.Debug($"ObjectListWindow: Territory changed to {territoryId}, cleared line targets");
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
    }

    public override void Draw()
    {
        DrawHeader();
        DrawTypeFilters();
        ImGui.Spacing();

        // two lines for draw lines
        var availWidth = ImGui.GetContentRegionAvail().X;
        var availHeight = ImGui.GetContentRegionAvail().Y;
        var linePanelWidth = 250f;
        var tableWidth = availWidth - linePanelWidth - ImGui.GetStyle().ItemSpacing.X;

        if (ImGui.BeginChild("##TablePanel", new Vector2(tableWidth, availHeight)))
        {
            if (ImGui.BeginTable("##ObjectListTable", ColumnNames.Length,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
                new Vector2(0, 0)))
            {
                // set cols
                ImGui.TableSetupColumn(ColumnNames[0], ImGuiTableColumnFlags.WidthFixed, 100);  // ObjectID
                ImGui.TableSetupColumn(ColumnNames[1], ImGuiTableColumnFlags.WidthStretch);     // 名称
                ImGui.TableSetupColumn(ColumnNames[2], ImGuiTableColumnFlags.WidthFixed, 80);   // 类型
                ImGui.TableSetupColumn(ColumnNames[3], ImGuiTableColumnFlags.WidthFixed, 80);   // 标记
                ImGui.TableSetupColumn(ColumnNames[4], ImGuiTableColumnFlags.WidthFixed, 100);  // DataID
                ImGui.TableSetupColumn(ColumnNames[5], ImGuiTableColumnFlags.WidthFixed, 100);  // 目标ID
                ImGui.TableHeadersRow();

                // draw lsit
                if (DService.ObjectTable != null)
                {
                    try
                    {
                        foreach (var obj in DService.ObjectTable)
                        {
                            if (obj == null || !obj.IsValid()) continue;

                            string objectId, name, type, marker, dataId, targetId;
                            Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind;

                            try
                            {
                                objectId = $"{obj.GameObjectID:X8}";
                                name = obj.Name?.ToString() ?? "";
                                objectKind = obj.ObjectKind;
                                type = GetShortTypeName(objectKind);
                                var markType = MarkerHelper.GetObjectMarker(obj);
                                marker = markType != MarkType.None ? MarkerHelper.GetMarkerName(markType) : "";
                                dataId = $"{obj.DataID}";
                                targetId = $"{obj.TargetObjectID:X8}";
                            }
                            catch
                            {
                                continue;
                            }
                    
                            if (!IsObjectKindVisible(objectKind))
                                continue;

                            // text filter
                            if (!string.IsNullOrEmpty(filterText))
                            {
                                var filter = filterText.ToLower();
                                if (!objectId.ToLower().Contains(filter) &&
                                    !name.ToLower().Contains(filter) &&
                                    !type.ToLower().Contains(filter) &&
                                    !marker.ToLower().Contains(filter) &&
                                    !dataId.ToLower().Contains(filter) &&
                                    !targetId.ToLower().Contains(filter))
                                {
                                    continue;
                                }
                            }

                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.TextUnformatted(objectId);
                            DrawCopyTooltip(objectId);

                            ImGui.TableSetColumnIndex(1);
                            ImGui.TextUnformatted(name);
                            DrawCopyTooltip(name);

                            ImGui.TableSetColumnIndex(2);
                            ImGui.TextUnformatted(type);
                            DrawCopyTooltip(type);

                            ImGui.TableSetColumnIndex(3);
                            ImGui.TextUnformatted(marker);
                            DrawCopyTooltip(marker);

                            ImGui.TableSetColumnIndex(4);
                            ImGui.TextUnformatted(dataId);
                            DrawCopyTooltip(dataId);

                            ImGui.TableSetColumnIndex(5);
                            ImGui.TextUnformatted(targetId);
                            DrawCopyTooltip(targetId);
                        }
                    }
                    catch (Exception ex)
                    {
                        DService.Log?.Error($"Error drawing object list: {ex.Message}");
                    }
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

        // LinePanel
        ImGui.SameLine();
        if (ImGui.BeginChild("##LinePanel", new Vector2(linePanelWidth, availHeight), true))
        {
            DrawLinePanel();
            ImGui.EndChild();
        }
    }
    
    public void DrawLineOverlay()
    {
        if (enableLine)
        {
            DrawObjectLine();
        }
    }

    private void DrawHeader()
    {
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##Filter", "搜索 ObjectID/名称/类型...", ref filterText, 100);

        ImGui.SameLine();
        if (ImGui.Button("清空"))
        {
            filterText = "";
        }

        ImGui.SameLine();
        var count = DService.ObjectTable?.Count() ?? 0;
        ImGui.TextDisabled($"总计: {count} 个对象");
    }

    private void DrawTypeFilters()
    {
        ImGui.Text("类型筛选:");
        ImGui.SameLine();

        if (ImGui.Button("全选"))
        {
            showPlayers = showBattleNpc = showEventNpc = showEventObj = true;
            showTreasure = showMountType = showCompanion = showOthers = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("全不选"))
        {
            showPlayers = showBattleNpc = showEventNpc = showEventObj = false;
            showTreasure = showMountType = showCompanion = showOthers = false;
        }

        ImGui.Checkbox("玩家(Player)", ref showPlayers);
        ImGui.SameLine();
        ImGui.Checkbox("战斗NPC(BattleNpc)", ref showBattleNpc);
        ImGui.SameLine();
        ImGui.Checkbox("事件NPC(EventNpc)", ref showEventNpc);
        ImGui.SameLine();
        ImGui.Checkbox("事件对象(EventObj)", ref showEventObj);
        ImGui.SameLine();
        ImGui.Checkbox("宝箱(Treasure)", ref showTreasure);
        ImGui.SameLine();
        ImGui.Checkbox("坐骑(MountType)", ref showMountType);
        ImGui.SameLine();
        ImGui.Checkbox("宠物(Companion)", ref showCompanion);
        ImGui.SameLine();
        ImGui.Checkbox("其他", ref showOthers);
    }

    private bool IsObjectKindVisible(Dalamud.Game.ClientState.Objects.Enums.ObjectKind kind)
    {
        return kind switch
        {
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player => showPlayers,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc => showBattleNpc,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc => showEventNpc,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj => showEventObj,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure => showTreasure,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.MountType => showMountType,
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion => showCompanion,
            _ => showOthers
        };
    }

    private static string GetShortTypeName(Dalamud.Game.ClientState.Objects.Enums.ObjectKind kind)
    {
        return kind switch
        {
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player => "Player",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc => "战斗NPC(BattleNpc)",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc => "事件NPC(EventNpc)",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj => "事件对象(EventObj)",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure => "宝箱(Treasure)",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.MountType => "坐骑(MountType)",
            Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion => "宠物(Companion)",
            _ => kind.ToString()
        };
    }

    private void DrawLinePanel()
    {
        ImGui.Text("连线功能");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("启用连线", ref enableLine);
        ImGui.Spacing();

        ImGui.Checkbox("配对模式", ref pairwiseMode);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("开启：目标1列表[0]→目标2列表[0], 目标1列表[1]→目标2列表[1]\n关闭：目标1列表每个Obj都连接目标2列表每个Obj（多对多）");
        }
        ImGui.Spacing();

        ImGui.Text("目标 1 列表:");

        ImGui.Checkbox("自己##Target1LocalPlayer", ref target1IsLocalPlayer);

        if (!target1IsLocalPlayer)
        {
            ImGui.SetNextItemWidth(-80);
            ImGui.InputTextWithHint("##NewTarget1", "输入 ObjectID", ref newTarget1Input, 16, ImGuiInputTextFlags.CharsHexadecimal);

            ImGui.SameLine();
            if (ImGui.Button("添加##AddTarget1", new Vector2(-1, 0)))
            {
                if (ulong.TryParse(newTarget1Input, System.Globalization.NumberStyles.HexNumber, null, out var val) && val != 0)
                {
                    if (!targetObjects1.Contains(val))
                        targetObjects1.Add(val);
                    newTarget1Input = "";
                }
            }

            if (ImGui.Button("添加当前目标##AddCurrentTarget1", new Vector2(-1, 0)))
            {
                var target = DService.Targets?.Target;
                if (target != null && !targetObjects1.Contains(target.GameObjectID))
                    targetObjects1.Add(target.GameObjectID);
            }

            ImGui.Spacing();

            // Target 1 List
            if (ImGui.BeginChild("##Target1List", new Vector2(-1, 100), true))
            {
                for (var i = targetObjects1.Count - 1; i >= 0; i--)
                {
                    var targetId = targetObjects1[i];
                    ImGui.Text($"{targetId:X8}");

                    ImGui.SameLine();
                    if (ImGui.SmallButton($"删除##1_{i}"))
                    {
                        targetObjects1.RemoveAt(i);
                    }
                }
                ImGui.EndChild();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("目标 2 列表:");
        
        ImGui.SetNextItemWidth(-80);
        ImGui.InputTextWithHint("##NewTarget2", "输入 ObjectID", ref newTarget2Input, 16, ImGuiInputTextFlags.CharsHexadecimal);

        ImGui.SameLine();
        if (ImGui.Button("添加##AddTarget2", new Vector2(-1, 0)))
        {
            if (ulong.TryParse(newTarget2Input, System.Globalization.NumberStyles.HexNumber, null, out var val) && val != 0)
            {
                if (!targetObjects2.Contains(val))
                    targetObjects2.Add(val);
                newTarget2Input = "";
            }
        }

        if (ImGui.Button("添加当前目标##AddCurrentTarget", new Vector2(-1, 0)))
        {
            var target = DService.Targets?.Target;
            if (target != null && !targetObjects2.Contains(target.GameObjectID))
                targetObjects2.Add(target.GameObjectID);
        }

        ImGui.Spacing();

        // Target 2 List
        if (ImGui.BeginChild("##Target2List", new Vector2(-1, 150), true))
        {
            for (var i = targetObjects2.Count - 1; i >= 0; i--)
            {
                var targetId = targetObjects2[i];
                ImGui.Text($"{targetId:X8}");

                ImGui.SameLine();
                if (ImGui.SmallButton($"删除##2_{i}"))
                {
                    targetObjects2.RemoveAt(i);
                }
            }
            ImGui.EndChild();
        }

        ImGui.Spacing();
        if (ImGui.Button("清空全部", new Vector2(-1, 0)))
        {
            targetObjects1.Clear();
            targetObjects2.Clear();
            target1IsLocalPlayer = false;
        }
    }

    private void DrawObjectLine()
    {
        // check & save VV's computer
        if (!enableLine) return;
        if (targetObjects2.Count == 0) return;
        if (!target1IsLocalPlayer && targetObjects1.Count == 0) return;
        
        try
        {
            // basic
            if (DService.ClientState == null || DService.ObjectTable == null || DService.Gui == null)
                return;
            
            // check loading
            if (DService.ObjectTable.LocalPlayer == null)
                return;
            
            if (DService.ClientState.TerritoryType == 0)
                return;

            var drawList = ImGui.GetForegroundDrawList();

            var sources = new List<ulong>();

            // check Target1 list
            if (target1IsLocalPlayer)
            {
                var localPlayer = DService.ObjectTable.LocalPlayer;
                if (localPlayer != null && localPlayer.IsValid())
                    sources.Add(localPlayer.EntityID);
            }
            else
            {
                sources.AddRange(targetObjects1);
            }

            if (sources.Count == 0) return;
            
            if (pairwiseMode)
            {
                // p2p
                var pairCount = Math.Min(sources.Count, targetObjects2.Count);
                for (var i = 0; i < pairCount; i++)
                {
                    try
                    {
                        var source1Id = sources[i];
                        var target2Id = targetObjects2[i];

                        var obj1 = DService.ObjectTable.FirstOrDefault(o => o != null && o.IsValid() && o.GameObjectID == source1Id);
                        if (obj1 == null) continue;

                        var obj2 = DService.ObjectTable.FirstOrDefault(o => o != null && o.IsValid() && o.GameObjectID == target2Id);
                        if (obj2 == null) continue;

                        if (!DService.Gui.WorldToScreen(obj1.Position, out var pos1) || !IsValidScreenPosition(pos1))
                            continue;

                        if (!DService.Gui.WorldToScreen(obj2.Position, out var pos2) || !IsValidScreenPosition(pos2))
                            continue;

                        drawList.AddLine(pos1, pos2, LineColor, 4f);
                        drawList.AddCircleFilled(pos1, 7f, DotColor);
                        drawList.AddCircleFilled(pos2, 7f, DotColor);
                    }
                    catch (Exception e)
                    {
                        Error(e.Message);
                    }
                }
            }
            else
            {
                // multi 2 multi
                foreach (var source1Id in sources)
                {
                    try
                    {
                        var obj1 = DService.ObjectTable.FirstOrDefault(o => o != null && o.IsValid() && o.GameObjectID == source1Id);
                        if (obj1 == null) continue;

                        if (!DService.Gui.WorldToScreen(obj1.Position, out var pos1))
                            continue;
                        
                        if (!IsValidScreenPosition(pos1))
                            continue;
                        
                        foreach (var target2Id in targetObjects2)
                        {
                            try
                            {
                                var obj2 = DService.ObjectTable.FirstOrDefault(o => o != null && o.IsValid() && o.GameObjectID == target2Id);
                                if (obj2 == null) continue;

                                if (DService.Gui.WorldToScreen(obj2.Position, out var pos2) && IsValidScreenPosition(pos2))
                                {
                                    drawList.AddLine(pos1, pos2, LineColor, 4f);
                                    drawList.AddCircleFilled(pos2, 7f, DotColor);
                                }
                            }
                            catch (Exception e)
                            {
                                Error(e.Message);
                            }
                        }
                        
                        drawList.AddCircleFilled(pos1, 7f, DotColor);
                    }
                    catch (Exception e)
                    {
                        Error(e.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DService.Log?.Debug($"DrawObjectLine error: {ex.Message}");
        }
    }

    private static bool IsValidScreenPosition(Vector2 pos)
    {
        // check valid pos
        if (float.IsNaN(pos.X) || float.IsNaN(pos.Y))
            return false;

        if (float.IsInfinity(pos.X) || float.IsInfinity(pos.Y))
            return false;

        // check in Screen
        var viewport = ImGui.GetMainViewport();
        var size = viewport.Size;
        return pos.X >= -1000 && pos.X <= size.X + 1000 &&
               pos.Y >= -1000 && pos.Y <= size.Y + 1000;
    }

    private static void DrawCopyTooltip(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("点击复制");
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemClicked())
            {
                ImGui.SetClipboardText(text);
                NotificationInfo($"已复制: {text}");
                Debug($"已复制: {text}");
            }
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
    }

    // me -> target2
    public void SetDrawLine(bool useSelfAsTarget1, ulong target2Id)
    {
        targetObjects1.Clear();
        targetObjects2.Clear();

        target1IsLocalPlayer = useSelfAsTarget1;
        targetObjects2.Add(target2Id);

        enableLine = true;
        pairwiseMode = true;
    }

    // target1 -> target2
    public void SetDrawLine(ulong target1Id, ulong target2Id)
    {
        targetObjects1.Clear();
        targetObjects2.Clear();

        target1IsLocalPlayer = false;
        targetObjects1.Add(target1Id);
        targetObjects2.Add(target2Id);

        enableLine = true;
        pairwiseMode = true;
    }
    
    // Clear all lines
    public void ClearDrawLine()
    {
        targetObjects1.Clear();
        targetObjects2.Clear();
        target1IsLocalPlayer = false;
        enableLine = false;
    }
}
