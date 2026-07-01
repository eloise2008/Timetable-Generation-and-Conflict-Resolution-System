using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using Timetabling.Models;

namespace Timetabling.Services
{
    public class ConflictResolver
    {
        private readonly List<Course> _courses;
        private readonly List<Room> _rooms;
        private readonly List<TimeSlot> _slots;

        public ConflictResolver(
            List<Course> courses,
            List<Room> rooms,
            List<TimeSlot> slots)
        {
            _courses = courses;
            _rooms = rooms;
            _slots = slots;
        }

        // Parse uploaded Excel and detect conflicts
        public ConflictReport ParseAndDetect(
            HttpPostedFileBase file,
            string academicYear,
            string semester)
        {
            var report = new ConflictReport
            {
                FileName = file.FileName,
                AcademicYear = academicYear,
                Semester = semester
            };

            var entries = ParseExcel(file);
            report.TotalEntries = entries.Count;

            DetectLecturerClashes(entries, report);
            DetectRoomClashes(entries, report);
            DetectCapacityViolations(entries, report);

            var conflictedCodes = new HashSet<string>(
                report.Conflicts
                    .SelectMany(c => new[] { c.CourseCode1, c.CourseCode2 })
                    .Where(c => c != null));

            report.ValidAssignments = entries
                .Where(e => !conflictedCodes.Contains(e.Course.CourseCode))
                .ToList();

            return report;
        }

        // Re-schedule only conflicted courses
        public List<Assignment> ResolveConflicts(ConflictReport report)
        {
            var conflictedCodes = new HashSet<string>(
                report.Conflicts
                    .SelectMany(c => new[] { c.CourseCode1, c.CourseCode2 })
                    .Where(c => c != null));

            var conflictedCourses = _courses
                .Where(c => conflictedCodes.Contains(c.CourseCode))
                .ToList();

            if (!conflictedCourses.Any())
                return report.ValidAssignments;

            var solver = new BacktrackingSolver(_rooms, _slots);
            var resolved = solver.SolveWithPreAssigned(
                conflictedCourses, report.ValidAssignments);

            return resolved ?? null;
        }

        // ── Excel Parser (supports both .xls and .xlsx) ─────────────
        private List<Assignment> ParseExcel(HttpPostedFileBase file)
        {
            var entries = new List<Assignment>();
            var courseMap = _courses.ToDictionary(
                c => c.CourseCode.Trim().ToUpper(), c => c);
            var roomMap = _rooms.ToDictionary(
                r => r.RoomName.Trim().ToUpper(), r => r);

            IWorkbook workbook;
            var ext = Path.GetExtension(file.FileName).ToLower();

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.InputStream.CopyTo(stream);
                    stream.Position = 0;

                    if (ext == ".xlsx")
                        workbook = new XSSFWorkbook(stream);
                    else
                        workbook = new HSSFWorkbook(stream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Cannot read Excel file. Make sure it is not " +
                    "password protected and is a valid .xls or .xlsx file. " +
                    "Details: " + ex.Message);
            }

            var sheet = workbook.GetSheetAt(0);
            if (sheet == null) return entries;

            // Find the header row containing "DAYS" in first column
            int headerRow = -1;
            for (int r = 0; r <= Math.Min(15, sheet.LastRowNum); r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;
                var cell = GetCellValue(row.GetCell(0)).ToUpper();
                if (cell.Contains("DAY") || cell == "DAYS")
                {
                    headerRow = r;
                    break;
                }
            }

            if (headerRow < 0)
            {
                // Fallback: scan all cells for course codes
                return ParseByScanning(sheet, courseMap, roomMap);
            }

            // Read time slot labels from header row
            var timeSlotLabels = new Dictionary<int, string>();
            var hRow = sheet.GetRow(headerRow);
            if (hRow != null)
            {
                for (int c = 2; c < hRow.LastCellNum; c++)
                {
                    var label = GetCellValue(hRow.GetCell(c)).Trim();
                    if (!string.IsNullOrEmpty(label) &&
                        !label.Equals("BREAK", StringComparison.OrdinalIgnoreCase))
                        timeSlotLabels[c] = label;
                }
            }

            // Parse data rows
            string currentDay = "";
            for (int r = headerRow + 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var dayCell = GetCellValue(row.GetCell(0)).Trim();
                var roomCell = GetCellValue(row.GetCell(1)).Trim();

                if (!string.IsNullOrEmpty(dayCell) && IsDay(dayCell))
                    currentDay = NormalizeDay(dayCell);

                if (string.IsNullOrEmpty(roomCell) ||
                    string.IsNullOrEmpty(currentDay)) continue;

                foreach (var kvp in timeSlotLabels)
                {
                    var cellVal = GetCellValue(row.GetCell(kvp.Key)).Trim();
                    if (string.IsNullOrEmpty(cellVal) ||
                        cellVal.Equals("BREAK",
                            StringComparison.OrdinalIgnoreCase)) continue;

                    // Split multiple courses in one cell
                    var codes = cellVal
                        .Split(new[] { '/', ',', '\n', '+' },
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToUpper())
                        .Where(x => x.Length >= 4 && x.Length <= 20);

                    foreach (var code in codes)
                    {
                        if (!courseMap.TryGetValue(code, out var course))
                            continue;

                        var slot = FindSlot(currentDay, kvp.Value);
                        if (slot == null) continue;

                        Room room;
                        if (!roomMap.TryGetValue(roomCell.ToUpper(), out room))
                            room = new Room
                            {
                                RoomId = -1,
                                RoomName = roomCell,
                                RoomCapacity = 9999
                            };

                        entries.Add(new Assignment
                        {
                            Course = course,
                            Room = room,
                            TimeSlot = slot
                        });
                    }
                }
            }

            return entries;
        }

        // Fallback: scan every cell for course codes
        private List<Assignment> ParseByScanning(
            ISheet sheet,
            Dictionary<string, Course> courseMap,
            Dictionary<string, Room> roomMap)
        {
            var entries = new List<Assignment>();
            string currDay = "Monday";
            var defaultSlot = _slots.FirstOrDefault();
            var defaultRoom = _rooms.FirstOrDefault();

            for (int r = 0; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                for (int c = 0; c < row.LastCellNum; c++)
                {
                    var val = GetCellValue(row.GetCell(c)).Trim().ToUpper();

                    if (IsDay(val))
                    {
                        currDay = NormalizeDay(val);
                        continue;
                    }

                    if (val.Length >= 5 && val.Length <= 12 &&
                        courseMap.ContainsKey(val) && defaultSlot != null)
                    {
                        var slot = _slots.FirstOrDefault(s =>
                            s.Day.Equals(currDay,
                                StringComparison.OrdinalIgnoreCase))
                            ?? defaultSlot;

                        entries.Add(new Assignment
                        {
                            Course = courseMap[val],
                            Room = defaultRoom ?? new Room
                            {
                                RoomId = -1,
                                RoomName = "Unknown",
                                RoomCapacity = 100
                            },
                            TimeSlot = slot
                        });
                    }
                }
            }

            return entries;
        }

        // ── Conflict detectors ───────────────────────────────────────
        private void DetectLecturerClashes(
            List<Assignment> entries, ConflictReport report)
        {
            var groups = entries.GroupBy(e =>
                $"{e.Course.LecturerId}_{e.TimeSlot.Day}_{e.TimeSlot.StartTime}");

            foreach (var g in groups.Where(g => g.Count() > 1))
            {
                var list = g.ToList();
                for (int i = 0; i < list.Count - 1; i++)
                    report.Conflicts.Add(new ConflictResolution
                    {
                        Type = "LecturerClash",
                        Description = $"Lecturer '{list[i].Course.LecturerName}'" +
                            " assigned to two courses simultaneously",
                        CourseCode1 = list[i].Course.CourseCode,
                        CourseCode2 = list[i + 1].Course.CourseCode,
                        Day = list[i].TimeSlot.Day,
                        TimeSlot = list[i].TimeSlot.StartTime + " – " +
                                      list[i].TimeSlot.EndTime,
                        Room = list[i].Room.RoomName
                    });
            }
        }

        private void DetectRoomClashes(
            List<Assignment> entries, ConflictReport report)
        {
            var groups = entries.GroupBy(e =>
                $"{e.Room.RoomId}_{e.TimeSlot.Day}_{e.TimeSlot.StartTime}");

            foreach (var g in groups.Where(g => g.Count() > 1))
            {
                var list = g.ToList();
                for (int i = 0; i < list.Count - 1; i++)
                    report.Conflicts.Add(new ConflictResolution
                    {
                        Type = "RoomClash",
                        Description = $"Room '{list[i].Room.RoomName}'" +
                            " assigned to two courses simultaneously",
                        CourseCode1 = list[i].Course.CourseCode,
                        CourseCode2 = list[i + 1].Course.CourseCode,
                        Day = list[i].TimeSlot.Day,
                        TimeSlot = list[i].TimeSlot.StartTime + " – " +
                                      list[i].TimeSlot.EndTime,
                        Room = list[i].Room.RoomName
                    });
            }
        }

        private void DetectCapacityViolations(
            List<Assignment> entries, ConflictReport report)
        {
            foreach (var e in entries.Where(
                e => e.Room.RoomId > 0 &&
                     e.Room.RoomCapacity < e.Course.CourseStudentPopulation))
                report.Conflicts.Add(new ConflictResolution
                {
                    Type = "CapacityViolation",
                    Description = $"Room '{e.Room.RoomName}' " +
                        $"(cap {e.Room.RoomCapacity}) too small for " +
                        $"'{e.Course.CourseCode}' " +
                        $"({e.Course.CourseStudentPopulation} students)",
                    CourseCode1 = e.Course.CourseCode,
                    Day = e.TimeSlot.Day,
                    TimeSlot = e.TimeSlot.StartTime + " – " + e.TimeSlot.EndTime,
                    Room = e.Room.RoomName
                });
        }

        // ── Helpers ──────────────────────────────────────────────────
        private string GetCellValue(ICell cell)
        {
            if (cell == null) return "";
            switch (cell.CellType)
            {
                case CellType.String: return cell.StringCellValue ?? "";
                case CellType.Numeric: return cell.NumericCellValue.ToString();
                case CellType.Boolean: return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try { return cell.StringCellValue ?? ""; }
                    catch { return cell.NumericCellValue.ToString(); }
                default: return "";
            }
        }

        private bool IsDay(string s)
        {
            var days = new[]
            {
                "monday","tuesday","wednesday","thursday","friday",
                "mon","tue","wed","thu","fri"
            };
            return days.Any(d =>
                s.Trim().Equals(d, StringComparison.OrdinalIgnoreCase));
        }

        private string NormalizeDay(string s)
        {
            s = s.Trim().ToLower();
            if (s.StartsWith("mon")) return "Monday";
            if (s.StartsWith("tue")) return "Tuesday";
            if (s.StartsWith("wed")) return "Wednesday";
            if (s.StartsWith("thu")) return "Thursday";
            if (s.StartsWith("fri")) return "Friday";
            return s;
        }

        private TimeSlot FindSlot(string day, string label)
        {
            // Try to match by start hour from label like "8-9AM", "10-11", "12NOON"
            label = label.ToUpper()
                .Replace("AM", "").Replace("PM", "")
                .Replace("NOON", "").Replace("MIDNIGHT", "").Trim();

            var parts = label.Split('-', '–');
            if (parts.Length < 1) return null;

            if (!int.TryParse(parts[0].Trim(), out int startHour)) return null;
            if (startHour > 0 && startHour < 7) startHour += 12;

            return _slots.FirstOrDefault(s =>
                s.Day.Equals(day, StringComparison.OrdinalIgnoreCase) &&
                s.StartTime.StartsWith(startHour.ToString("00") + ":"));
        }
    }
}