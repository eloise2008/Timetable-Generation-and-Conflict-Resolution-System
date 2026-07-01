using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Timetabling.Models;

namespace Timetabling.Services
{
    public class MetricsCalculator
    {
        public EvaluationMetrics Calculate(
            List<Assignment> assignments,
            List<Course> allCourses,
            TimetableSession session)
        {
            var m = new EvaluationMetrics();
            m.Session = session;

            if (assignments == null || assignments.Count == 0)
                return m;

            // ── M1: Hard Constraint Satisfaction ─────────────────────
            int checks = 0;
            int violations = 0;

            // Check lecturer clashes
            var lecGroups = assignments.GroupBy(a =>
                $"{a.Course.LecturerId}_{a.TimeSlot.Day}_{a.TimeSlot.StartTime}");
            foreach (var g in lecGroups)
            {
                checks++;
                if (g.Count() > 1) violations++;
            }

            // Check room clashes
            var roomGroups = assignments.GroupBy(a =>
                $"{a.Room.RoomId}_{a.TimeSlot.Day}_{a.TimeSlot.StartTime}");
            foreach (var g in roomGroups)
            {
                checks++;
                if (g.Count() > 1) violations++;
            }

            // Check capacity violations
            foreach (var a in assignments)
            {
                checks++;
                if (a.Room.RoomCapacity < a.Course.CourseStudentPopulation)
                    violations++;
            }

            m.TotalConstraintChecks = checks;
            m.TotalViolations = violations;
            m.HardConstraintSatisfactionRate = checks > 0
                ? Math.Round((double)(checks - violations) / checks * 100, 2)
                : 100;

            // ── M2: Scheduling Efficiency ────────────────────────────
            m.TotalCoursesLoaded = allCourses?.Count ?? assignments.Count;
            m.CoursesScheduled = assignments
                .Select(a => a.Course.CourseId).Distinct().Count();
            m.SchedulingEfficiency = m.TotalCoursesLoaded > 0
                ? Math.Round((double)m.CoursesScheduled / m.TotalCoursesLoaded * 100, 2)
                : 0;

            // ── M3: Soft Constraint Penalty ──────────────────────────
            m.TotalSoftPenalty = assignments.Sum(a => a.SoftPenalty);
            m.AverageSoftPenalty = Math.Round(
                (double)m.TotalSoftPenalty / assignments.Count, 2);
            m.HighPenaltyCount = assignments.Count(a => a.SoftPenalty > 20);

            // ── M4: Room Utilisation ─────────────────────────────────
            var roomGroups2 = assignments
                .GroupBy(a => a.Room.RoomId)
                .Select(g => new RoomUtilisationDetail
                {
                    RoomName = g.First().Room.RoomName,
                    Capacity = g.First().Room.RoomCapacity,
                    AvgStudents = Math.Round(
                        g.Average(a => a.Course.CourseStudentPopulation), 1),
                    UtilPct = Math.Round(
                        g.Average(a => (double)a.Course.CourseStudentPopulation
                                       / a.Room.RoomCapacity * 100), 1),
                    TimesUsed = g.Count()
                })
                .OrderByDescending(r => r.UtilPct)
                .ToList();

            m.RoomDetails = roomGroups2;
            m.AverageRoomUtilisation = roomGroups2.Count > 0
                ? Math.Round(roomGroups2.Average(r => r.UtilPct), 2) : 0;
            m.MinRoomUtilisation = roomGroups2.Count > 0
                ? roomGroups2.Min(r => r.UtilPct) : 0;
            m.MaxRoomUtilisation = roomGroups2.Count > 0
                ? roomGroups2.Max(r => r.UtilPct) : 0;

            // ── M5: Lecturer Workload Distribution ───────────────────
            var lecWorkload = assignments
                .GroupBy(a => a.Course.LecturerId)
                .Select(g => new LecturerWorkloadDetail
                {
                    LecturerName = g.First().Course.LecturerName,
                    CourseCount = g.Count()
                })
                .OrderByDescending(l => l.CourseCount)
                .ToList();

            double lecMean = lecWorkload.Count > 0
                ? lecWorkload.Average(l => l.CourseCount) : 0;
            double lecVariance = lecWorkload.Count > 0
                ? lecWorkload.Average(l =>
                    Math.Pow(l.CourseCount - lecMean, 2)) : 0;

            foreach (var l in lecWorkload)
                l.WorkloadPct = lecMean > 0
                    ? Math.Round(l.CourseCount / lecMean * 100, 1) : 0;

            m.LecturerDetails = lecWorkload;
            m.AverageCoursesPerLecturer = Math.Round(lecMean, 2);
            m.LecturerWorkloadStdDev = Math.Round(Math.Sqrt(lecVariance), 2);
            m.MaxCoursesForOneLecturer = lecWorkload.Count > 0
                ? lecWorkload.Max(l => l.CourseCount) : 0;
            m.MinCoursesForOneLecturer = lecWorkload.Count > 0
                ? lecWorkload.Min(l => l.CourseCount) : 0;

            // ── M6: Schedule Balance Across the Week ─────────────────
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
            foreach (var day in days)
                m.AssignmentsPerDay[day] = assignments
                    .Count(a => a.TimeSlot.Day.Equals(
                        day, StringComparison.OrdinalIgnoreCase));

            double dayMean = m.AssignmentsPerDay.Values.Average();
            double dayVariance = m.AssignmentsPerDay.Values
                .Average(v => Math.Pow(v - dayMean, 2));
            m.SlotBalanceStdDev = Math.Round(Math.Sqrt(dayVariance), 2);

            // ── Overall Quality Score (weighted composite) ───────────
            double score = 0;
            score += m.HardConstraintSatisfactionRate * 0.40; // 40% weight
            score += m.SchedulingEfficiency * 0.20; // 20% weight
            score += Math.Max(0, 100 - m.TotalSoftPenalty / 2.0) * 0.15; // 15%
            score += m.AverageRoomUtilisation * 0.15; // 15% weight
            score += Math.Max(0, 100 - m.LecturerWorkloadStdDev * 10) * 0.05; // 5%
            score += Math.Max(0, 100 - m.SlotBalanceStdDev * 10) * 0.05; // 5%

            m.OverallQualityScore = Math.Round(Math.Min(100, score), 1);
            m.QualityRating = m.OverallQualityScore >= 90 ? "Excellent"
                            : m.OverallQualityScore >= 75 ? "Good"
                            : m.OverallQualityScore >= 60 ? "Acceptable"
                            : "Needs Improvement";

            return m;
        }
    }
}