using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Timetabling.Models
{
    public class Lecturer
    {

        public int LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string LecturerDepartment { get; set; }
        public string LecturerAvailability { get; set; }
        public int? DepartmentId { get; set; }

    }
}