using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Timetabling.Models
{
    public class ConflictResolution
    {

        public string Type { get; set; } // "LecturerClash", "RoomClash", "CapacityViolation"
        public string Description { get; set; }
        public string CourseCode1 { get; set; }
        public string CourseCode2 { get; set; }
        public string Day { get; set; }
        public string TimeSlot { get; set; }
        public string Room { get; set; }
        public bool IsResolved { get; set; }
    }

    public class ConflictReport
    {
        public string FileName { get; set; }
        public string AcademicYear { get; set; }
        public string Semester { get; set; }
        public int TotalEntries { get; set; }
        public List<ConflictResolution> Conflicts { get; set; } = new List<ConflictResolution>();
        public List<Assignment> ValidAssignments { get; set; } = new List<Assignment>();
        public bool HasConflicts => Conflicts.Count > 0;
    }

    public class UploadViewModel
    {
        public string AcademicYear { get; set; }
        public string Semester { get; set; }
        public string SessionName { get; set; }
    }

}

