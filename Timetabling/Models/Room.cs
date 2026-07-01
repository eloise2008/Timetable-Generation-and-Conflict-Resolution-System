using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetabling.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int RoomCapacity { get; set; }
        public string RoomType { get; set; }
        public string RoomAvailability { get; set; }
    }
}
