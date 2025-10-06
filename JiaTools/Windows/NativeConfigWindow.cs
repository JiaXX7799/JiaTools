using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;

namespace JiaTools.Windows;

public unsafe class NativeConfigWindow(Configuration config) : NativeAddon
{
    private ScrollingAreaNode<TreeListNode>? scrollingAreaNode;
    private TreeListCategoryNode? generalCategory;
    private TreeListCategoryNode? objectTypeCategory;
    private TreeListCategoryNode? displayCategory;

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

    protected override void OnSetup(AtkUnitBase* addon)
    {
        // 创建设置的滚动区域
        AttachNode(scrollingAreaNode = new ScrollingAreaNode<TreeListNode>
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            ContentHeight = 800.0f,
            ScrollSpeed = 25,
            IsVisible = true,
        });

        var treeList = scrollingAreaNode.ContentAreaNode;

        // 常规设置分类
        treeList.AddCategoryNode(generalCategory = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
            String = "常规设置",
        });

        SetupGeneralSettings(generalCategory);

        // 对象类型分类
        treeList.AddCategoryNode(objectTypeCategory = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
            String = "对象类型",
        });

        SetupObjectTypeSettings(objectTypeCategory);

        // 显示设置分类
        treeList.AddCategoryNode(displayCategory = new TreeListCategoryNode
        {
            IsVisible = true,
            IsCollapsed = false,
            String = "显示选项",
        });

        SetupDisplaySettings(displayCategory);

        // 在所有节点添加完成后设置滑块宽度
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

    private void SetupGeneralSettings(TreeListCategoryNode category)
    {
        // 透明度滑块
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
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

        // 字体缩放滑块
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
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

        // 扫描范围滑块
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "扫描范围",
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
                if (rangeSlider != null)
                    rangeSlider.ValueNode.String = $"{value}米";
            }
        });
        rangeSlider.ValueNode.String = $"{(int)config.Range}米";

        // 最大对象数滑块
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
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

        // 合并距离滑块
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 14,
            String = "合并距离",
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
                if (mergeDistanceSlider != null)
                    mergeDistanceSlider.ValueNode.String = $"{value}像素";
            }
        });
        mergeDistanceSlider.ValueNode.String = $"{(int)config.MergeDistance}像素";
    }

    private void SetupObjectTypeSettings(TreeListCategoryNode category)
    {
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 12,
            String = "选择要在悬浮窗中显示的对象类型",
            TextColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        });

        // 显示玩家
        category.AddNode(showPlayersCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "玩家",
            IsChecked = config.ShowPlayers,
            OnClick = isChecked =>
            {
                config.ShowPlayers = isChecked;
                config.Save();
            }
        });

        // 显示本地玩家
        category.AddNode(showLocalPlayerCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "本地玩家",
            IsChecked = config.ShowLocalPlayer,
            OnClick = isChecked =>
            {
                config.ShowLocalPlayer = isChecked;
                config.Save();
            }
        });

        // 显示战斗NPC
        category.AddNode(showBattleNpcsCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "战斗NPC (BattleNpc)",
            IsChecked = config.ShowBattleNpcs,
            OnClick = isChecked =>
            {
                config.ShowBattleNpcs = isChecked;
                config.Save();
            }
        });

        // 显示事件NPC
        category.AddNode(showEventNpcsCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "事件NPC (EventNpc)",
            IsChecked = config.ShowEventNpcs,
            OnClick = isChecked =>
            {
                config.ShowEventNpcs = isChecked;
                config.Save();
            }
        });

        // 显示事件对象
        category.AddNode(showEventObjsCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
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

    private void SetupDisplaySettings(TreeListCategoryNode category)
    {
        category.AddNode(new TextNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            FontType = FontType.Axis,
            FontSize = 12,
            String = "选择要显示的对象信息",
            TextColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        });

        // 显示实体ID
        category.AddNode(showEntityIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "EntityID",
            IsChecked = config.ShowEntityID,
            OnClick = isChecked =>
            {
                config.ShowEntityID = isChecked;
                config.Save();
            }
        });

        // 显示数据ID
        category.AddNode(showDataIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "DataID",
            IsChecked = config.ShowDataID,
            OnClick = isChecked =>
            {
                config.ShowDataID = isChecked;
                config.Save();
            }
        });

        // 使用16进制ID
        category.AddNode(useHexIDCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "16进制ID显示",
            IsChecked = config.UseHexID,
            OnClick = isChecked =>
            {
                config.UseHexID = isChecked;
                config.Save();
            }
        });

        // 显示位置
        category.AddNode(showPositionCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "位置坐标",
            IsChecked = config.ShowPosition,
            OnClick = isChecked =>
            {
                config.ShowPosition = isChecked;
                config.Save();
            }
        });

        // 显示旋转
        category.AddNode(showRotationCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "旋转角度",
            IsChecked = config.ShowRotation,
            OnClick = isChecked =>
            {
                config.ShowRotation = isChecked;
                config.Save();
            }
        });

        // 显示距离
        category.AddNode(showDistanceCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "距离",
            IsChecked = config.ShowDistance,
            OnClick = isChecked =>
            {
                config.ShowDistance = isChecked;
                config.Save();
            }
        });

        // 显示生命值
        category.AddNode(showHealthCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "生命值",
            IsChecked = config.ShowHealth,
            OnClick = isChecked =>
            {
                config.ShowHealth = isChecked;
                config.Save();
            }
        });

        // 显示魔法值
        category.AddNode(showManaCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "魔法值",
            IsChecked = config.ShowMana,
            OnClick = isChecked =>
            {
                config.ShowMana = isChecked;
                config.Save();
            }
        });

        // 显示咏唱信息
        category.AddNode(showCastInfoCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
            IsVisible = true,
            String = "咏唱信息",
            IsChecked = config.ShowCastInfo,
            OnClick = isChecked =>
            {
                config.ShowCastInfo = isChecked;
                config.Save();
            }
        });

        // 显示状态列表
        category.AddNode(showStatusListCheckbox = new CheckboxNode
        {
            Size = new Vector2(category.Width - 40, 20),
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

    protected override void OnUpdate(AtkUnitBase* addon)
    {
        // 当配置在外部更改时更新滑块值和文本
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
            rangeSlider.ValueNode.String = $"{(int)config.Range}米";
        }

        if (maxObjectsSlider != null && maxObjectsSlider.Value != config.MaxObjects)
        {
            maxObjectsSlider.Value = config.MaxObjects;
            maxObjectsSlider.ValueNode.String = $"{config.MaxObjects}";
        }

        if (mergeDistanceSlider != null && mergeDistanceSlider.Value != (int)config.MergeDistance)
        {
            mergeDistanceSlider.Value = (int)config.MergeDistance;
            mergeDistanceSlider.ValueNode.String = $"{(int)config.MergeDistance}像素";
        }
    }

}
