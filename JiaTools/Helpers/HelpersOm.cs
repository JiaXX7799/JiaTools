using System;
using OmenTools.Dalamud;
using OmenTools.Extensions;
using OmenTools.OmenService;

namespace JiaTools.Helpers;

public static class HelpersOm
{
    public static bool BetweenAreas => DService.Instance().Condition.IsBetweenAreas;

    public static void Debug(string message) =>
        DLog.Debug(message);

    public static void Debug(string message, Exception ex) =>
        DLog.Debug(message, ex);

    public static void Error(string message) =>
        DLog.Error(message);

    public static void Error(string message, Exception ex) =>
        DLog.Error(message, ex);

    public static void NotificationSuccess(string message) =>
        NotifyHelper.Instance().NotificationSuccess(message);

    public static void NotificationWarning(string message) =>
        NotifyHelper.Instance().NotificationWarning(message);

    public static void NotificationError(string message) =>
        NotifyHelper.Instance().NotificationError(message);

    public static void NotificationInfo(string message) =>
        NotifyHelper.Instance().NotificationInfo(message);
}
