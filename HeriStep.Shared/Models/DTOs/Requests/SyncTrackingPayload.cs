using System;
using System.Collections.Generic;

namespace HeriStep.Shared.Models.DTOs.Requests
{
    public class SyncLocationLog
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SyncListenLog
    {
        public int PoiId { get; set; }
        public int ListenDurationSeconds { get; set; }
    }

    public class SyncTrackingPayload
    {
        public string DeviceId { get; set; } = string.Empty;
        public List<SyncLocationLog> LocationLogs { get; set; } = new List<SyncLocationLog>();
        public List<SyncListenLog> ListenLogs { get; set; } = new List<SyncListenLog>();
    }
}
