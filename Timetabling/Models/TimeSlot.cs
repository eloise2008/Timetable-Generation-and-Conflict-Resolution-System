using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetabling.Models
{
    public class TimeSlot
    {
        public int TimeId { get; set; }
        public string Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int Duration { get; set; }
        public bool IsEarlyOrLate
        {
            get
            {
                if (string.IsNullOrEmpty(StartTime)) return false;
                var parts = StartTime.Split(':');
                if (parts.Length > 0 && int.TryParse(parts[0], out int h))
                    return h < 8 || h >= 18;
                return false;
            }
        }

        public int DayOrder
        {
            get
            {
                switch (Day?.ToLower())
                {
                    case "monday": return 1;
                    case "tuesday": return 2;
                    case "wednesday": return 3;
                    case "thursday": return 4;
                    case "friday": return 5;
                    default: return 6;
                }
            }
        }
    }
}
