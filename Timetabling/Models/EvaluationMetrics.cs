using System.Collections.Generic;

namespace Timetabling.Models
{
    public class EvaluationMetrics
    {
        public TimetableSession Session { get; set; }

        // M1: Hard constraint satisfaction
        public double HardConstraintSatisfactionRate { get; set; }
        public int TotalConstraintChecks { get; set; }
        public int TotalViolations { get; set; }
        public int LecturerClashes { get; set; }
        public int RoomClashes { get; set; }
        public int CapacityViolations { get; set; }
        public int ConflictsBeforeAutomation { get; set; }
        public int ConflictsAfterAutomation { get; set; }

        // M2: Scheduling efficiency
        public double SchedulingEfficiency { get; set; }
        public int TotalCoursesLoaded { get; set; }
        public int CoursesScheduled { get; set; }
        public int CoursesUnscheduled { get; set; }

        // M3: Soft penalty
        public int TotalSoftPenalty { get; set; }
        public double AverageSoftPenalty { get; set; }
        public int HighPenaltyCount { get; set; }

        // M4: Room utilisation
        public double AverageRoomUtilisation { get; set; }
        public double MinRoomUtilisation { get; set; }
        public double MaxRoomUtilisation { get; set; }
        public List<RoomUtilisationDetail> RoomDetails { get; set; }
            = new List<RoomUtilisationDetail>();

        // M5: Lecturer workload
        public double LecturerWorkloadStdDev { get; set; }
        public double AverageCoursesPerLecturer { get; set; }
        public int MaxCoursesForOneLecturer { get; set; }
        public int MinCoursesForOneLecturer { get; set; }
        public List<LecturerWorkloadDetail> LecturerDetails { get; set; }
            = new List<LecturerWorkloadDetail>();

        // M6: Slot balance
        public double SlotBalanceStdDev { get; set; }
        public Dictionary<string, int> AssignmentsPerDay { get; set; }
            = new Dictionary<string, int>();

        // M7: Performance metrics
        public double GenerationTimeMs { get; set; }
        public double GenerationTimeSec { get; set; }
        public double MemoryUsedMB { get; set; }
        public double SystemResponseTimeMs { get; set; }
        public int CoursesPerSecond { get; set; }

        // Overall
        public double OverallQualityScore { get; set; }
        public string QualityRating { get; set; }
    }

    public class RoomUtilisationDetail
    {
        public string RoomName { get; set; }
        public int Capacity { get; set; }
        public double AvgStudents { get; set; }
        public double UtilPct { get; set; }
        public int TimesUsed { get; set; }
    }

    public class LecturerWorkloadDetail
    {
        public string LecturerName { get; set; }
        public int CourseCount { get; set; }
        public double WorkloadPct { get; set; }
    }
}