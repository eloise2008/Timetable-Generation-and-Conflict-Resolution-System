using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetabling.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public int LecturerId { get; set; }
        public int CourseStudentPopulation { get; set; }
        public string CourseLevel { get; set; }
        public int? DepartmentId { get; set; }
        public string Semester { get; set; }
        public int Duration { get; set; } 
        public bool IsUWC { get; set; }
        public string CourseType { get; set; }

        // Populated from JOIN queries
        public string LecturerName { get; set; }
        public string LecturerAvailability { get; set; }
        public string DepartmentName { get; set; }
    }
}

