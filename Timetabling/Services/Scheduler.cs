using System;
using System.Collections.Generic;
using System.Linq;
using Timetabling.Models;

namespace Timetabling.Services
{
    public static class ConstraintChecker
    {
        public static bool DurationMatches(Assignment a)
            => a.TimeSlot.Duration == a.Course.Duration;

        public static bool LecturerFree(Assignment a, List<Assignment> assigned)
            => !assigned.Any(x =>
                x.TimeSlot.Day == a.TimeSlot.Day &&
                x.Course.LecturerId == a.Course.LecturerId &&
                SlotsOverlap(x.TimeSlot, a.TimeSlot));

        public static bool RoomFree(Assignment a, List<Assignment> assigned)
            => !assigned.Any(x =>
                x.TimeSlot.Day == a.TimeSlot.Day &&
                x.Room.RoomId == a.Room.RoomId &&
                SlotsOverlap(x.TimeSlot, a.TimeSlot));

        public static bool RoomFits(Assignment a)
            => a.Room.RoomCapacity >= a.Course.CourseStudentPopulation;

        public static bool LecturerOnDay(Assignment a)
        {
            var avail = a.Course.LecturerAvailability;
            if (string.IsNullOrWhiteSpace(avail)) return true;
            return avail.Split(',').Any(d =>
                d.Trim().Equals(a.TimeSlot.Day,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static bool RoomOnDay(Assignment a)
        {
            var avail = a.Room.RoomAvailability;
            if (string.IsNullOrWhiteSpace(avail)) return true;
            return avail.Split(',').Any(d =>
                d.Trim().Equals(a.TimeSlot.Day,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static bool UWCSlotFree(Assignment a, List<Assignment> assigned)
        {
            if (!a.Course.IsUWC) return true;
            return !assigned.Any(x =>
                x.TimeSlot.Day == a.TimeSlot.Day &&
                x.Course.CourseLevel == a.Course.CourseLevel &&
                SlotsOverlap(x.TimeSlot, a.TimeSlot));
        }

        public static bool NotClashingWithUWC(Assignment a, List<Assignment> assigned)
        {
            if (a.Course.IsUWC) return true;
            return !assigned.Any(x =>
                x.Course.IsUWC &&
                x.TimeSlot.Day == a.TimeSlot.Day &&
                x.Course.CourseLevel == a.Course.CourseLevel &&
                SlotsOverlap(x.TimeSlot, a.TimeSlot));
        }

        public static bool SlotsOverlap(TimeSlot a, TimeSlot b)
        {
            int aS = ParseHour(a.StartTime), aE = ParseHour(a.EndTime);
            int bS = ParseHour(b.StartTime), bE = ParseHour(b.EndTime);
            return aS < bE && bS < aE;
        }

        private static int ParseHour(string t)
        {
            if (string.IsNullOrEmpty(t)) return 0;
            return int.TryParse(t.Split(':')[0], out int h) ? h : 0;
        }

        public static bool AllHard(Assignment a, List<Assignment> assigned)
            => DurationMatches(a)
            && RoomFits(a)
            && LecturerOnDay(a)
            && RoomOnDay(a)
            && LecturerFree(a, assigned)
            && RoomFree(a, assigned)
            && UWCSlotFree(a, assigned)
            && NotClashingWithUWC(a, assigned);

        public static int SoftPenalty(Assignment a, List<Assignment> assigned)
        {
            int p = 0;
            if (a.TimeSlot.IsEarlyOrLate) p += 10;
            p += assigned.Count(x => x.TimeSlot.Day == a.TimeSlot.Day) * 3;
            if (assigned.Any(x =>
                x.Course.LecturerId == a.Course.LecturerId &&
                x.TimeSlot.Day == a.TimeSlot.Day)) p += 5;
            return p;
        }
    }

    public class BacktrackingSolver
    {
        private readonly List<Room> _rooms;
        private readonly List<TimeSlot> _slots;

        // Pre-built lookup sets for O(1) conflict checking
        private readonly HashSet<string> _lecturerSlotUsed = new HashSet<string>();
        private readonly HashSet<string> _roomSlotUsed = new HashSet<string>();
        private readonly HashSet<string> _uwcLevelSlotUsed = new HashSet<string>();

        public BacktrackingSolver(List<Room> rooms, List<TimeSlot> slots)
        {
            _rooms = rooms;
            _slots = slots;
        }

        public List<Assignment> Solve(List<Course> courses)
        {
            var result = new List<Assignment>();
            return Backtrack(courses, 0, result) ? result : null;
        }


        private bool Backtrack(List<Course> courses, int idx, List<Assignment> assigned)
        {
            if (idx == courses.Count) return true;

            var course = courses[idx];
            var candidates = GetCandidates(course, assigned);

            foreach (var candidate in candidates)
            {
                if (IsValid(candidate))
                {
                    candidate.SoftPenalty =
                        ConstraintChecker.SoftPenalty(candidate, assigned);

                    Add(candidate);
                    assigned.Add(candidate);

                    if (Backtrack(courses, idx + 1, assigned)) return true;

                    assigned.Remove(candidate);
                    Remove(candidate);
                }
            }
            return false;
        }


        // Fast candidate generation — only matching duration slots
        private List<Assignment> GetCandidates(Course course, List<Assignment> assigned)
        {
            var matchingSlots = _slots
                .Where(s => s.Duration == course.Duration)
                .ToList();

            var candidates = new List<Assignment>();
            foreach (var slot in matchingSlots)
                foreach (var room in _rooms
                    .Where(r => r.RoomCapacity >= course.CourseStudentPopulation))
                    candidates.Add(new Assignment
                    {
                        Course = course,
                        Room = room,
                        TimeSlot = slot
                    });

            // Count current assignments per day
            var dayCount = new Dictionary<string, int>
    {
        {"Monday",0},{"Tuesday",0},{"Wednesday",0},
        {"Thursday",0},{"Friday",0}
    };
            foreach (var a in assigned)
                if (dayCount.ContainsKey(a.TimeSlot.Day))
                    dayCount[a.TimeSlot.Day]++;

            return candidates
                // Prefer days with fewer assignments (balance the week)
                .OrderBy(a => dayCount.ContainsKey(a.TimeSlot.Day)
                    ? dayCount[a.TimeSlot.Day] : 0)
                .ThenBy(a => a.TimeSlot.IsEarlyOrLate ? 1 : 0)
                .ThenBy(a => a.TimeSlot.StartTime)
                .ThenBy(a => a.Room.RoomCapacity)
                .ToList();
        }

        // O(1) conflict check using hash sets
        private bool IsValid(Assignment a)
        {
            var day = a.TimeSlot.Day;
            var sStart = ConstraintChecker.SlotsOverlap(a.TimeSlot, a.TimeSlot)
                         ? a.TimeSlot.StartTime : a.TimeSlot.StartTime;

            // Check lecturer not double booked
            for (int h = ParseH(a.TimeSlot.StartTime); h < ParseH(a.TimeSlot.EndTime); h++)
            {
                string lecKey = $"L{a.Course.LecturerId}_{day}_{h}";
                string roomKey = $"R{a.Room.RoomId}_{day}_{h}";
                string uwcKey = $"U{a.Course.CourseLevel}_{day}_{h}";

                if (_lecturerSlotUsed.Contains(lecKey)) return false;
                if (_roomSlotUsed.Contains(roomKey)) return false;
                if (a.Course.IsUWC && _uwcLevelSlotUsed.Contains(uwcKey)) return false;
                if (!a.Course.IsUWC && _uwcLevelSlotUsed.Contains(uwcKey)) return false;
            }

            // Check lecturer availability
            var avail = a.Course.LecturerAvailability;
            if (!string.IsNullOrWhiteSpace(avail))
            {
                if (!avail.Split(',').Any(d =>
                    d.Trim().Equals(day, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            // Check room availability
            var ravail = a.Room.RoomAvailability;
            if (!string.IsNullOrWhiteSpace(ravail))
            {
                if (!ravail.Split(',').Any(d =>
                    d.Trim().Equals(day, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

        private void Add(Assignment a)
        {
            var day = a.TimeSlot.Day;
            for (int h = ParseH(a.TimeSlot.StartTime); h < ParseH(a.TimeSlot.EndTime); h++)
            {
                _lecturerSlotUsed.Add($"L{a.Course.LecturerId}_{day}_{h}");
                _roomSlotUsed.Add($"R{a.Room.RoomId}_{day}_{h}");
                if (a.Course.IsUWC)
                    _uwcLevelSlotUsed.Add($"U{a.Course.CourseLevel}_{day}_{h}");
            }
        }

        private void Remove(Assignment a)
        {
            var day = a.TimeSlot.Day;
            for (int h = ParseH(a.TimeSlot.StartTime); h < ParseH(a.TimeSlot.EndTime); h++)
            {
                _lecturerSlotUsed.Remove($"L{a.Course.LecturerId}_{day}_{h}");
                _roomSlotUsed.Remove($"R{a.Room.RoomId}_{day}_{h}");
                if (a.Course.IsUWC)
                    _uwcLevelSlotUsed.Remove($"U{a.Course.CourseLevel}_{day}_{h}");
            }
        }

        private int ParseH(string t)
        {
            if (string.IsNullOrEmpty(t)) return 0;
            return int.TryParse(t.Split(':')[0], out int h) ? h : 0;
        }

        // Solve with some assignments already fixed (for conflict resolution)
        public List<Assignment> SolveWithPreAssigned(
    List<Course> conflictedCourses,
    List<Assignment> preAssigned)
        {
            // Rebuild hash sets from pre-assigned valid entries
            foreach (var a in preAssigned)
                Add(a);

            var result = new List<Assignment>(preAssigned);
            bool success = Backtrack(conflictedCourses, 0, result);

            // Clean up
            foreach (var a in preAssigned)
                Remove(a);

            return success ? result : null;
        }
    }

    public class Scheduler
    {
        public List<Assignment> GenerateTimetable(
            List<Course> courses, List<Room> rooms, List<TimeSlot> slots)
        {
            // Order: UWC first, then by student population desc (hardest to place first)
            var ordered = courses
                .OrderByDescending(c => c.IsUWC)
                .ThenByDescending(c => c.CourseStudentPopulation)
                .ToList();

            return new BacktrackingSolver(rooms, slots).Solve(ordered);
        }
    }

}