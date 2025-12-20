namespace JiaTools.Helpers;

public class ActionHelper
{
    public static string GetTypeString(uint type)
    {
        var str = type switch
        {
            0           => "未知",
            1           => "单体",
            2 or 5 or 6 or 7=> "圆形", // 6还有点迷惑
            3 or 13     => "扇形",
            4 or 12     => "矩形",
            // 7           => "圆形(目的地)",
            8           => "矩形(从来源到目标或目的地)",
            9           => "不存在此类型",
            10          => "环形",
            11          => "十字",
            14          => "三角形",
            15          => "武士pvp突进",
            _           => "类型错误"
        };
        
        return str + $"({type})";
    }
}