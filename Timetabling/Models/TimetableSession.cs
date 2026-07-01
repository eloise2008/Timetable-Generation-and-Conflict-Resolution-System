using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Timetabling.Models
{
    public class TimetableSession
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public string AcademicYear { get; set; }
        public string Semester { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; }
        public int TotalCourses { get; set; }
        public bool IsActive { get; set; }

        public List<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    public class GenerateViewModel
    {
        public string AcademicYear { get; set; }
        public string Semester { get; set; }
        public string SessionName { get; set; }
    }
}