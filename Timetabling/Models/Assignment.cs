using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetabling.Models
{

    public class Assignment
    {
        public int TimetableId { get; set; }
        public Course Course { get; set; }
        public Room Room { get; set; }
        public TimeSlot TimeSlot { get; set; }
        public int SoftPenalty { get; set; }
    }

}
