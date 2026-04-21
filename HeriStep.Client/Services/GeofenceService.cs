using HeriStep.Shared.Models;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// Geofencing engine that detects when the user enters/exits stall proximity zones.
    /// Uses the Haversine formula (via MAUI's Location.CalculateDistance) for distance calculation.
    /// 
    /// How it works:
    /// 1. A background loop calls CheckProximity() with current GPS coordinates.
    /// 2. For each stall, it calculates the distance to the user.
    /// 3. If the user is within the effective radius AND hasn't been announced yet,
    ///    the StallEntered event fires.
    /// 4. When the user moves away, the stall is removed from the "entered" set,
    ///    allowing re-entry announcements on the next visit.
    /// </summary>
    public class GeofenceService
    {
        /// <summary>Fires when the user enters a stall's geofence for the first time.</summary>
        public event Action<Stall>? StallEntered;

        /// <summary>Fires when the user exits a stall's geofence.</summary>
        public event Action<Stall>? StallExited;

        // Track which stalls the user is currently inside
        private readonly HashSet<int> _currentlyInside = new();

        /// <summary>
        /// Gets the configured alert radius from user preferences (VoiceAura settings).
        /// Falls back to 50 meters if not set.
        /// </summary>
        private double GlobalRadius => Preferences.Default.Get("voice_radius", 50.0);

        /// <summary>
        /// Checks the user's proximity to all stalls and fires enter/exit events.
        /// Should be called periodically from a background loop (every 1-5 seconds).
        /// </summary>
        /// <param name="userLat">User's current latitude.</param>
        /// <param name="userLon">User's current longitude.</param>
        /// <param name="stalls">List of stalls with GPS coordinates.</param>
        public void CheckProximity(double userLat, double userLon, IEnumerable<Stall> stalls)
        {
            var userLoc = new Microsoft.Maui.Devices.Sensors.Location(userLat, userLon);
            var currentNearby = new HashSet<int>();

            foreach (var stall in stalls)
            {
                if (stall.Latitude == 0 && stall.Longitude == 0) continue;

                var stallLoc = new Microsoft.Maui.Devices.Sensors.Location(stall.Latitude, stall.Longitude);
                double distMeters = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(userLoc, stallLoc, DistanceUnits.Kilometers) * 1000;

                // Use per-stall radius if set (from API), otherwise global user preference
                double effectiveRadius = stall.RadiusMeter > 0
                    ? Math.Min(stall.RadiusMeter, GlobalRadius)
                    : GlobalRadius;

                if (distMeters <= effectiveRadius)
                {
                    currentNearby.Add(stall.Id);

                    // First entry → fire StallEntered
                    if (!_currentlyInside.Contains(stall.Id))
                    {
                        _currentlyInside.Add(stall.Id);
                        StallEntered?.Invoke(stall);
                    }
                }
            }

            // Check for exits: stalls that were inside but no longer nearby
            var exited = _currentlyInside.Except(currentNearby).ToList();
            foreach (var stallId in exited)
            {
                _currentlyInside.Remove(stallId);
                var exitedStall = stalls.FirstOrDefault(s => s.Id == stallId);
                if (exitedStall != null)
                {
                    StallExited?.Invoke(exitedStall);
                }
            }
        }

        /// <summary>
        /// Resets all tracked geofences. Useful when switching demo mode
        /// or when the user wants to re-hear all announcements.
        /// </summary>
        public void Reset()
        {
            _currentlyInside.Clear();
        }
    }
}
