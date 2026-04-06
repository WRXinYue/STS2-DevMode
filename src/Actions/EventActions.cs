using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Actions;

internal static class EventActions
{
    public static IEnumerable<EventModel> GetAllEvents()
    {
        try { return ModelDb.AllEvents; }
        catch { return Enumerable.Empty<EventModel>(); }
    }

    /// <summary>Force-enter an event by navigating to its room.</summary>
    public static bool TryForceEnterEvent(EventModel eventModel)
    {
        try
        {
            var runManager = RunManager.Instance;
            if (runManager == null || !runManager.IsInProgress) return false;

            // Use reflection to find a method to enter an event room
            var methods = typeof(RunManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var enterMethod = methods.FirstOrDefault(m =>
                m.Name.Contains("Event", StringComparison.OrdinalIgnoreCase) &&
                m.GetParameters().Any(p => typeof(AbstractModel).IsAssignableFrom(p.ParameterType)));

            if (enterMethod != null)
            {
                enterMethod.Invoke(runManager, new object[] { eventModel });
                return true;
            }

            // Fallback: try CreateRoom with event room type
            var createRoom = typeof(RunManager).GetMethod("CreateRoom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (createRoom != null)
            {
                var roomType = (MegaCrit.Sts2.Core.Rooms.RoomType)5; // Event room type
                createRoom.Invoke(runManager, new object[] { roomType, default(MegaCrit.Sts2.Core.Map.MapPointType), eventModel });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"ForceEnterEvent failed: {ex.Message}");
            return false;
        }
    }

    public static string GetEventDisplayName(EventModel evt)
    {
        try { return evt.Title?.GetFormattedText() ?? ((AbstractModel)evt).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)evt).Id.Entry ?? "?"; }
    }
}
