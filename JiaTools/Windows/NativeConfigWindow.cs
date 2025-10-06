using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Nodes.TabBar;

namespace JiaTools.Windows;

public unsafe class NativeConfigWindow(Configuration config) : NativeAddon
{
    private TabBarNode? tabBar;

    private ScrollingAreaNode<TreeListNode>? generalScrollArea;
    private ScrollingAreaNode<TreeListNode>? objectTypeScrollArea;
    private ScrollingAreaNode<TreeListNode>? displayScrollArea;
    private ScrollingAreaNode<ResNode>? objectDetailScrollArea;

    // 常规设置
    private SliderNode? opacitySlider;
    private SliderNode? fontScaleSlider;
    private SliderNode? rangeSlider;
    private SliderNode? maxObjectsSlider;
    private SliderNode? mergeDistanceSlider;

    // 对象类型设置
    private CheckboxNode? showPlayersCheckbox;
    private CheckboxNode? showLocalPlayerCheckbox;
    private CheckboxNode? showBattleNpcsCheckbox;
    private CheckboxNode? showEventNpcsCheckbox;
    private CheckboxNode? showEventObjsCheckbox;

    // 显示设置
    private CheckboxNode? showEntityIDCheckbox;
    private CheckboxNode? showDataIDCheckbox;
    private CheckboxNode? useHexIDCheckbox;
    private CheckboxNode? showPositionCheckbox;
    private CheckboxNode? showRotationCheckbox;
    private CheckboxNode? showDistanceCheckbox;
    private CheckboxNode? showHealthCheckbox;
    private CheckboxNode? showManaCheckbox;
    private CheckboxNode? showCastInfoCheckbox;
    private CheckboxNode? showStatusListCheckbox;

    // 对象详情
    private string objectFilterText = "";
    private List<TextNode> objectListNodes = new();
    private int updateCounter = 0;
    private Vector2 normalSize = new(350, 600);
    private Vector2 expandedSize = new(800, 800);

    protected override void OnSetup(AtkUnitBase* addon)
    {
        AttachNode(tabBar = new TabBarNode
        {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, 32),
            IsVisible = true,
        });

        var tabContentY = ContentStartPosition.Y + 40;
        var tabContentHeight = ContentSize.Y - 40;

        AttachNode(generalScrollArea = new ScrollingAreaNode<TreeListNode>
        {
            Position = new Vector2(ContentStartPosition.X, tabContentY),
            Size = new Vector2(ContentSize.X, tabContentHeight),
            ContentHeight = 600.0f,
            ScrollSpeed = 25,
            IsVisible = true,
        });

        AttachNode(objectTypeScrollArea = new ScrollingAreaNode<TreeListNode>
        {
            Position = new Vector2(ContentStartPosition.X, tabContentY),
            Size = new Vector2(ContentSize.X, tabContentHeight),
            ContentHeight = 400.0f,
            ScrollSpeed = 25,
            IsVisible = false,
        });

        AttachNode(displayScrollArea = new ScrollingAreaNode<TreeListNode>
        {
            Position = new Vector2(ContentStartPosition.X, tabContentY),
            Size = new Vector2(ContentSize.X, tabContentHeight),
            ContentHeight = 600.0f,
            ScrollSpeed = 25,
            IsVisible = false,
        });

        AttachNode(objectDetailScrollArea = new ScrollingAreaNode<ResNode>
        {
            Position = new Vector2(ContentStartPosition.X, tabContentY),
            Size = new Vector2(ContentSize.X, tabContentHeight),
            ContentHeight = 800.0f,
            ScrollSpeed = 25,
            IsVisible = false,
        });

        tabBar.AddTab("常规设置", () =>
        {
            updateCounter = 0;
            ResizeWindow(normalSize);
            if (generalScrollArea != null) generalScrollArea.IsVisible = true;
            if (objectTypeScrollArea != null) objectTypeScrollArea.IsVisible = false;
            if (displayScrollArea != null) displayScrollArea.IsVisible = false;
            if (objectDetailScrollArea != null) objectDetailScrollArea.IsVisible = false;
        });

        tabBar.AddTab("对象类型", () =>
        {
            updateCounter = 0;
            ResizeWindow(normalSize);
            if (generalScrollArea != null) generalScrollArea.IsVisible = false;
            if (objectTypeScrollArea != null) objectTypeScrollArea.IsVisible = true;
            if (displayScrollArea != null) displayScrollArea.IsVisible = false;
            if (objectDetailScrollArea != null) objectDetailScrollArea.IsVisible = false;
        });

        tabBar.AddTab("显示选项", () =>
        {
            updateCounter = 0;
            ResizeWindow(normalSize);
            if (generalScrollArea != null) generalScrollArea.IsVisible = false;
            if (objectTypeScrollArea != null) objectTypeScrollArea.IsVisible = false;
            if (displayScrollArea != null) displayScrollArea.IsVisible = true;
            if (objectDetailScrollArea != null) objectDetailScrollArea.IsVisible = false;
        });

        tabBar.AddTab("对象详情", () =>
        {
            updateCounter = 0;
            ResizeWindow(expandedSize);
            if (generalScrollArea != null) generalScrollArea.IsVisible = false;
            if (objectTypeScrollArea != null) objectTypeScrollArea.IsVisible = false;
            if (displayScrollArea != null) displayScrollArea.IsVisible = false;
            if (objectDetailScrollArea != null) objectDetailScrollArea.IsVisible = true;
        });

        SetupGeneralSettings(generalScrollArea.ContentAreaNode);
        SetupObjectTypeSettings(objectTypeScrollArea.ContentAreaNode);
        SetupDisplaySettings(displayScrollArea.ContentAreaNode);
        SetupObjectDetailTab(objectDetailScrollArea.ContentAreaNode);

        if (opacitySlider != null)
            opacitySlider.Width = 300.0f;
        if (fontScaleSlider != null)
            fontScaleSlider.Width = 300.0f;
        if (rangeSlider != null)
            rangeSlider.Width = 300.0f;
        if (maxObjectsSlider != null)
            maxObjectsSlider.Width = 300.0f;
        if (mergeDistanceSlider != null)
            mergeDistanceSlider.Width = 300.0f;
    }

    private void SetupGeneralSettings(TreeListNode treeList)
    {
        var category = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
        };
        treeList.AddCategoryNode(category);

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "透明度",
            TextColor = new Vector4(0.9f, 0.8f, 0.4f, 1.0f),
        });

        category.AddNode(opacitySlider = new SliderNode
        {
            Size = new Vector2(300.0f, 32),
            IsVisible = true,
            Range = 10..100,
            OnValueChanged = value =>
            {
                config.Opacity = value / 100f;
                config.Save();
                if (opacitySlider != null)
                    opacitySlider.ValueNode.String = $"{value}%";
            }
        });
        opacitySlider.ValueNode.String = $"{(int)(config.Opacity * 100)}%";

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "字体缩放",
            TextColor = new Vector4(0.9f, 0.8f, 0.4f, 1.0f),
        });

        category.AddNode(fontScaleSlider = new SliderNode
        {
            Size = new Vector2(300.0f, 32),
            IsVisible = true,
            Range = 50..200,
            OnValueChanged = value =>
            {
                config.FontScale = value / 100f;
                config.Save();
                if (fontScaleSlider != null)
                    fontScaleSlider.ValueNode.String = $"{value}%";
            }
        });
        fontScaleSlider.ValueNode.String = $"{(int)(config.FontScale * 100)}%";

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "扫描范围 (米)",
            TextColor = new Vector4(0.4f, 0.9f, 0.8f, 1.0f),
        });

        category.AddNode(rangeSlider = new SliderNode
        {
            Size = new Vector2(300.0f, 32),
            IsVisible = true,
            Range = 5..100,
            OnValueChanged = value =>
            {
                config.Range = value;
                config.Save();
            }
        });

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "最大对象数",
            TextColor = new Vector4(0.4f, 0.9f, 0.8f, 1.0f),
        });

        category.AddNode(maxObjectsSlider = new SliderNode
        {
            Size = new Vector2(300.0f, 32),
            IsVisible = true,
            Range = 1..100,
            OnValueChanged = value =>
            {
                config.MaxObjects = value;
                config.Save();
                if (maxObjectsSlider != null)
                    maxObjectsSlider.ValueNode.String = $"{value}";
            }
        });
        maxObjectsSlider.ValueNode.String = $"{config.MaxObjects}";

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "合并距离 (像素)",
            TextColor = new Vector4(0.4f, 0.9f, 0.8f, 1.0f),
        });

        category.AddNode(mergeDistanceSlider = new SliderNode
        {
            Size = new Vector2(300.0f, 32),
            IsVisible = true,
            Range = 10..200,
            OnValueChanged = value =>
            {
                config.MergeDistance = value;
                config.Save();
            }
        });
    }

    private void SetupObjectTypeSettings(TreeListNode treeList)
    {
        var category = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
        };
        treeList.AddCategoryNode(category);

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 12,
            String = "选择要在悬浮窗中显示的对象类型",
            TextColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        });

        category.AddNode(showPlayersCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "玩家",
            IsChecked = config.ShowPlayers,
            OnClick = isChecked =>
            {
                config.ShowPlayers = isChecked;
                config.Save();
            }
        });

        category.AddNode(showLocalPlayerCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "本地玩家",
            IsChecked = config.ShowLocalPlayer,
            OnClick = isChecked =>
            {
                config.ShowLocalPlayer = isChecked;
                config.Save();
            }
        });

        category.AddNode(showBattleNpcsCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "战斗NPC (BattleNpc)",
            IsChecked = config.ShowBattleNpcs,
            OnClick = isChecked =>
            {
                config.ShowBattleNpcs = isChecked;
                config.Save();
            }
        });

        category.AddNode(showEventNpcsCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "事件NPC (EventNpc)",
            IsChecked = config.ShowEventNpcs,
            OnClick = isChecked =>
            {
                config.ShowEventNpcs = isChecked;
                config.Save();
            }
        });

        category.AddNode(showEventObjsCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "事件对象 (EventObj)",
            IsChecked = config.ShowEventObjs,
            OnClick = isChecked =>
            {
                config.ShowEventObjs = isChecked;
                config.Save();
            }
        });
    }

    private void SetupDisplaySettings(TreeListNode treeList)
    {
        var category = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
        };
        treeList.AddCategoryNode(category);

        category.AddNode(new TextNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 12,
            String = "选择要显示的对象信息",
            TextColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        });

        category.AddNode(showEntityIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "EntityID",
            IsChecked = config.ShowEntityID,
            OnClick = isChecked =>
            {
                config.ShowEntityID = isChecked;
                config.Save();
            }
        });

        category.AddNode(showDataIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "DataID",
            IsChecked = config.ShowDataID,
            OnClick = isChecked =>
            {
                config.ShowDataID = isChecked;
                config.Save();
            }
        });

        category.AddNode(useHexIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "16进制ID显示",
            IsChecked = config.UseHexID,
            OnClick = isChecked =>
            {
                config.UseHexID = isChecked;
                config.Save();
            }
        });

        category.AddNode(showPositionCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "位置坐标",
            IsChecked = config.ShowPosition,
            OnClick = isChecked =>
            {
                config.ShowPosition = isChecked;
                config.Save();
            }
        });

        category.AddNode(showRotationCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "旋转角度",
            IsChecked = config.ShowRotation,
            OnClick = isChecked =>
            {
                config.ShowRotation = isChecked;
                config.Save();
            }
        });

        category.AddNode(showDistanceCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "距离",
            IsChecked = config.ShowDistance,
            OnClick = isChecked =>
            {
                config.ShowDistance = isChecked;
                config.Save();
            }
        });

        category.AddNode(showHealthCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "生命值",
            IsChecked = config.ShowHealth,
            OnClick = isChecked =>
            {
                config.ShowHealth = isChecked;
                config.Save();
            }
        });

        category.AddNode(showManaCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "魔法值",
            IsChecked = config.ShowMana,
            OnClick = isChecked =>
            {
                config.ShowMana = isChecked;
                config.Save();
            }
        });

        category.AddNode(showCastInfoCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "咏唱信息",
            IsChecked = config.ShowCastInfo,
            OnClick = isChecked =>
            {
                config.ShowCastInfo = isChecked;
                config.Save();
            }
        });

        category.AddNode(showStatusListCheckbox = new CheckboxNode
        {
            Size = new Vector2(300, 20),
            IsVisible = true,
            String = "状态列表",
            IsChecked = config.ShowStatusList,
            OnClick = isChecked =>
            {
                config.ShowStatusList = isChecked;
                config.Save();
            }
        });
    }

    private void SetupObjectDetailTab(ResNode contentNode)
    {
        var headerText = new TextNode
        {
            Position = new Vector2(10, 10),
            Size = new Vector2(500, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 16,
            String = "对象列表",
            TextColor = new Vector4(0.9f, 0.8f, 0.4f, 1.0f),
        };
        AttachNode(headerText, contentNode);

        var columnHeaders = new[]
        {
            (pos: 10f, text: "ObjectID", width: 80f),
            (pos: 100f, text: "名称", width: 150f),
            (pos: 260f, text: "类型", width: 100f),
            (pos: 370f, text: "DataID", width: 80f),
            (pos: 460f, text: "目标ID", width: 80f)
        };

        foreach (var (pos, text, width) in columnHeaders)
        {
            var header = new TextNode
            {
                Position = new Vector2(pos, 40),
                Size = new Vector2(width, 20),
                IsVisible = true,
                FontType = FontType.Axis,
                FontSize = 12,
                String = text,
                TextColor = new Vector4(0.7f, 0.9f, 0.7f, 1.0f),
            };
            AttachNode(header, contentNode);
        }

    }

    protected override void OnUpdate(AtkUnitBase* addon)
    {
        if (opacitySlider != null && opacitySlider.Value != (int)(config.Opacity * 100))
        {
            opacitySlider.Value = (int)(config.Opacity * 100);
            opacitySlider.ValueNode.String = $"{(int)(config.Opacity * 100)}%";
        }

        if (fontScaleSlider != null && fontScaleSlider.Value != (int)(config.FontScale * 100))
        {
            fontScaleSlider.Value = (int)(config.FontScale * 100);
            fontScaleSlider.ValueNode.String = $"{(int)(config.FontScale * 100)}%";
        }

        if (rangeSlider != null && rangeSlider.Value != (int)config.Range)
        {
            rangeSlider.Value = (int)config.Range;
        }

        if (maxObjectsSlider != null && maxObjectsSlider.Value != config.MaxObjects)
        {
            maxObjectsSlider.Value = config.MaxObjects;
            maxObjectsSlider.ValueNode.String = $"{config.MaxObjects}";
        }

        if (mergeDistanceSlider != null && mergeDistanceSlider.Value != (int)config.MergeDistance)
        {
            mergeDistanceSlider.Value = (int)config.MergeDistance;
        }

        if (objectDetailScrollArea?.IsVisible == true)
        {
            updateCounter++;
            if (updateCounter >= 30)
            {
                updateCounter = 0;
                UpdateObjectDetailList();
            }
        }
        else
        {
            updateCounter = 0;
        }
    }

    private void ResizeWindow(Vector2 newSize)
    {
        try
        {
            WindowNode.Size = newSize;
            Size = newSize;

            if (tabBar != null)
            {
                tabBar.Position = ContentStartPosition;
                tabBar.Size = new Vector2(ContentSize.X, 32);
            }

            var tabContentY = ContentStartPosition.Y + 40;
            var tabContentHeight = ContentSize.Y - 40;

            if (generalScrollArea != null)
            {
                generalScrollArea.Position = new Vector2(ContentStartPosition.X, tabContentY);
                generalScrollArea.Size = new Vector2(ContentSize.X, tabContentHeight);
            }

            if (objectTypeScrollArea != null)
            {
                objectTypeScrollArea.Position = new Vector2(ContentStartPosition.X, tabContentY);
                objectTypeScrollArea.Size = new Vector2(ContentSize.X, tabContentHeight);
            }

            if (displayScrollArea != null)
            {
                displayScrollArea.Position = new Vector2(ContentStartPosition.X, tabContentY);
                displayScrollArea.Size = new Vector2(ContentSize.X, tabContentHeight);
            }

            if (objectDetailScrollArea != null)
            {
                objectDetailScrollArea.Position = new Vector2(ContentStartPosition.X, tabContentY);
                objectDetailScrollArea.Size = new Vector2(ContentSize.X, tabContentHeight);
            }
        }
        catch (Exception ex)
        {
            DService.Log.Error($"调整窗口大小失败: {ex.Message}");
        }
    }

    private void UpdateObjectDetailList()
    {
        if (objectDetailScrollArea?.ContentAreaNode == null)
            return;

        if (!objectDetailScrollArea.IsVisible || objectDetailScrollArea.Size.X <= 0 || objectDetailScrollArea.Size.Y <= 0)
            return;

        try
        {
            var yPos = 70f;
            var maxObjects = 20;
            var count = 0;
            var nodeIndex = 0;

            foreach (var obj in DService.ObjectTable)
            {
                if (obj == null || count >= maxObjects)
                    break;

                var objectId = $"{obj.GameObjectID:X8}";
                var name = obj.Name.ToString();
                if (name.Length > 18)
                    name = name.Substring(0, 15) + "...";

                var type = obj.ObjectKind.ToString();
                if (type.Length > 12)
                    type = type.Substring(0, 12);

                var dataId = $"{obj.DataID}";
                var targetId = $"{obj.TargetObjectID:X8}";

                var columns = new[]
                {
                    (pos: 10f, text: objectId, width: 80f),
                    (pos: 100f, text: name, width: 150f),
                    (pos: 260f, text: type, width: 100f),
                    (pos: 370f, text: dataId, width: 80f),
                    (pos: 460f, text: targetId, width: 80f)
                };

                for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                {
                    var (pos, text, width) = columns[colIndex];
                    var nodeIdx = nodeIndex * 5 + colIndex;

                    if (nodeIdx < objectListNodes.Count)
                    {
                        objectListNodes[nodeIdx].String = text;
                        objectListNodes[nodeIdx].IsVisible = true;
                    }
                    else
                    {
                        var cellText = new TextNode
                        {
                            Position = new Vector2(pos, yPos),
                            Size = new Vector2(width, 18),
                            IsVisible = true,
                            FontType = FontType.Axis,
                            FontSize = 12,
                            String = text,
                            TextColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f),
                        };

                        AttachNode(cellText, objectDetailScrollArea.ContentAreaNode);
                        objectListNodes.Add(cellText);
                    }
                }

                yPos += 20f;
                count++;
                nodeIndex++;
            }

            for (int i = nodeIndex * 5; i < objectListNodes.Count; i++)
            {
                objectListNodes[i].IsVisible = false;
            }

            if (objectDetailScrollArea != null)
            {
                objectDetailScrollArea.ContentHeight = Math.Max(800f, yPos + 50f);
            }
        }
        catch (Exception ex)
        {
            DService.Log.Error($"更新对象列表失败: {ex.Message}");
        }
    }

}