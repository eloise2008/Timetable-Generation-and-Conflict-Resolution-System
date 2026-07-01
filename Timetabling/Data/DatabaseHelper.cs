using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Timetabling.Models;
using System.Configuration;
using System.Data;


namespace Timetabling.Data
{
    public class DatabaseHelper
    {
        private readonly string _conn;

        public DatabaseHelper()
        {
            _conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        private SqlConnection Open()
        {
            var c = new SqlConnection(_conn);
            c.Open();
            return c;
        }

        public int Count(string table)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM " + table, con))
                return (int)cmd.ExecuteScalar();
        }

        // ── DEPARTMENTS ──────────────────────────────────────────────
        public List<Department> GetAllDepartments()
        {
            var list = new List<Department>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT DepartmentId, DepartmentName FROM DEPARTMENT ORDER BY DepartmentName", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(MapDept(r));
            return list;
        }

        public Department GetDepartmentById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT DepartmentId, DepartmentName FROM DEPARTMENT WHERE DepartmentId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapDept(r) : null;
            }
        }

        public void InsertDepartment(Department d)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "INSERT INTO DEPARTMENT (DepartmentName) VALUES (@n)", con))
            {
                cmd.Parameters.AddWithValue("@n", d.DepartmentName);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateDepartment(Department d)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE DEPARTMENT SET DepartmentName=@n WHERE DepartmentId=@id", con))
            {
                cmd.Parameters.AddWithValue("@n", d.DepartmentName);
                cmd.Parameters.AddWithValue("@id", d.DepartmentId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteDepartment(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "DELETE FROM DEPARTMENT WHERE DepartmentId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Department MapDept(SqlDataReader r) => new Department
        {
            DepartmentId = (int)r["DepartmentId"],
            DepartmentName = r["DepartmentName"].ToString()
        };

        // ── LECTURERS ────────────────────────────────────────────────
        public List<Lecturer> GetAllLecturers()
        {
            var list = new List<Lecturer>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM LECTURER ORDER BY LecturerName", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(MapLec(r));
            return list;
        }

        public Lecturer GetLecturerById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM LECTURER WHERE LecturerId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapLec(r) : null;
            }
        }

        public void InsertLecturer(Lecturer l)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"INSERT INTO LECTURER (LecturerName, LecturerDepartment, LecturerAvailability, DepartmentId)
                  VALUES (@n, @d, @a, @did)", con))
            {
                cmd.Parameters.AddWithValue("@n", l.LecturerName);
                cmd.Parameters.AddWithValue("@d", l.LecturerDepartment ?? "");
                cmd.Parameters.AddWithValue("@a", l.LecturerAvailability ?? "");
                cmd.Parameters.AddWithValue("@did", (object)l.DepartmentId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateLecturer(Lecturer l)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"UPDATE LECTURER SET LecturerName=@n, LecturerDepartment=@d,
                  LecturerAvailability=@a, DepartmentId=@did
                  WHERE LecturerId=@id", con))
            {
                cmd.Parameters.AddWithValue("@n", l.LecturerName);
                cmd.Parameters.AddWithValue("@d", l.LecturerDepartment ?? "");
                cmd.Parameters.AddWithValue("@a", l.LecturerAvailability ?? "");
                cmd.Parameters.AddWithValue("@did", (object)l.DepartmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", l.LecturerId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteLecturer(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "DELETE FROM LECTURER WHERE LecturerId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
        public List<Course> GetCoursesByLecturer(int lecturerId)
        {
            var list = new List<Course>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                COURSE_SQL + " WHERE c.LecturerId = @lid ORDER BY c.CourseCode", con))
            {
                cmd.Parameters.AddWithValue("@lid", lecturerId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(MapCourse(r));
            }
            return list;
        }
        public void AssignCourseToLecturer(int courseId, int lecturerId)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE COURSE SET LecturerId=@lid WHERE CourseId=@cid", con))
            {
                cmd.Parameters.AddWithValue("@lid", lecturerId);
                cmd.Parameters.AddWithValue("@cid", courseId);
                cmd.ExecuteNonQuery();
            }
        }

        public void UnassignCourseFromLecturer(int courseId)
        {
            // Reassign to default lecturer (id=1)
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE COURSE SET LecturerId=1 WHERE CourseId=@cid", con))
            {
                cmd.Parameters.AddWithValue("@cid", courseId);
                cmd.ExecuteNonQuery();
            }
        }

        private Lecturer MapLec(SqlDataReader r) => new Lecturer
        {
            LecturerId = (int)r["LecturerId"],
            LecturerName = r["LecturerName"].ToString(),
            LecturerDepartment = r["LecturerDepartment"].ToString(),
            LecturerAvailability = r["LecturerAvailability"].ToString(),
            DepartmentId = r["DepartmentId"] == DBNull.Value
                ? (int?)null : (int)r["DepartmentId"]
        };

        // ── ROOMS ────────────────────────────────────────────────────
        public List<Room> GetAllRooms()
        {
            var list = new List<Room>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM ROOM ORDER BY RoomCapacity DESC", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(MapRoom(r));
            return list;
        }

        public Room GetRoomById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM ROOM WHERE RoomId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapRoom(r) : null;
            }
        }

        public void InsertRoom(Room rm)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"INSERT INTO ROOM (RoomName, RoomCapacity, RoomType, RoomAvailability)
                  VALUES (@n, @c, @t, @a)", con))
            {
                cmd.Parameters.AddWithValue("@n", rm.RoomName);
                cmd.Parameters.AddWithValue("@c", rm.RoomCapacity);
                cmd.Parameters.AddWithValue("@t", (object)rm.RoomType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@a", (object)rm.RoomAvailability ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateRoom(Room rm)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"UPDATE ROOM SET RoomName=@n, RoomCapacity=@c,
                  RoomType=@t, RoomAvailability=@a
                  WHERE RoomId=@id", con))
            {
                cmd.Parameters.AddWithValue("@n", rm.RoomName);
                cmd.Parameters.AddWithValue("@c", rm.RoomCapacity);
                cmd.Parameters.AddWithValue("@t", (object)rm.RoomType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@a", (object)rm.RoomAvailability ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", rm.RoomId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteRoom(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "DELETE FROM ROOM WHERE RoomId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Room MapRoom(SqlDataReader r) => new Room
        {
            RoomId = (int)r["RoomId"],
            RoomName = r["RoomName"].ToString(),
            RoomCapacity = (int)r["RoomCapacity"],
            RoomType = r["RoomType"]?.ToString(),
            RoomAvailability = r["RoomAvailability"]?.ToString()
        };

        // ── TIMESLOTS ────────────────────────────────────────────────
        public List<TimeSlot> GetAllTimeSlots()
        {
            var list = new List<TimeSlot>();
            using (var con = Open())
            using (var cmd = new SqlCommand("SELECT * FROM TIMESLOT", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(MapSlot(r));
            return list;
        }

        public TimeSlot GetTimeSlotById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM TIMESLOT WHERE TimeId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapSlot(r) : null;
            }
        }

        public void InsertTimeSlot(TimeSlot t)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "INSERT INTO TIMESLOT (Day, StartTime, EndTime) VALUES (@d, @s, @e)", con))
            {
                cmd.Parameters.AddWithValue("@d", t.Day);
                cmd.Parameters.AddWithValue("@s", t.StartTime);
                cmd.Parameters.AddWithValue("@e", t.EndTime);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateTimeSlot(TimeSlot t)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE TIMESLOT SET Day=@d, StartTime=@s, EndTime=@e WHERE TimeId=@id", con))
            {
                cmd.Parameters.AddWithValue("@d", t.Day);
                cmd.Parameters.AddWithValue("@s", t.StartTime);
                cmd.Parameters.AddWithValue("@e", t.EndTime);
                cmd.Parameters.AddWithValue("@id", t.TimeId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteTimeSlot(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "DELETE FROM TIMESLOT WHERE TimeId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private TimeSlot MapSlot(SqlDataReader r) => new TimeSlot
        {
            TimeId = (int)r["TimeId"],
            Day = r["Day"].ToString(),
            StartTime = r["StartTime"].ToString(),
            EndTime = r["EndTime"].ToString(),
            Duration = HasColumn(r, "Duration") && r["Duration"] != DBNull.Value
                ? (int)r["Duration"] : 1
        };

        // ── COURSES ──────────────────────────────────────────────────
        private const string COURSE_SQL =
             @"SELECT c.CourseId, c.CourseCode, c.CourseTitle, c.LecturerId,
             c.CourseStudentPopulation, c.CourseLevel, c.DepartmentId,
             c.Semester, c.Duration, c.CourseType, c.IsUWC,
             l.LecturerName, l.LecturerAvailability,
             ISNULL(d.DepartmentName, '') AS DepartmentName
      FROM COURSE c
      JOIN LECTURER l ON c.LecturerId = l.LecturerId
      LEFT JOIN DEPARTMENT d ON c.DepartmentId = d.DepartmentId";

        public List<Course> GetAllCourses()
        {
            var list = new List<Course>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                COURSE_SQL + " ORDER BY c.DepartmentId, c.CourseLevel, c.CourseCode", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(MapCourse(r));
            return list;
        }

        public Course GetCourseById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                COURSE_SQL + " WHERE c.CourseId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapCourse(r) : null;
            }
        }

        public void InsertCourse(Course c)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"INSERT INTO COURSE (CourseCode, CourseTitle, LecturerId,
                  CourseStudentPopulation, CourseLevel, DepartmentId)
                  VALUES (@code, @title, @lid, @pop, @level, @did)", con))
            {
                cmd.Parameters.AddWithValue("@code", c.CourseCode);
                cmd.Parameters.AddWithValue("@title", c.CourseTitle);
                cmd.Parameters.AddWithValue("@lid", c.LecturerId);
                cmd.Parameters.AddWithValue("@pop", c.CourseStudentPopulation);
                cmd.Parameters.AddWithValue("@level", (object)c.CourseLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@did", (object)c.DepartmentId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateCourse(Course c)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"UPDATE COURSE SET CourseCode=@code, CourseTitle=@title,
                  LecturerId=@lid, CourseStudentPopulation=@pop,
                  CourseLevel=@level, DepartmentId=@did
                  WHERE CourseId=@id", con))
            {
                cmd.Parameters.AddWithValue("@code", c.CourseCode);
                cmd.Parameters.AddWithValue("@title", c.CourseTitle);
                cmd.Parameters.AddWithValue("@lid", c.LecturerId);
                cmd.Parameters.AddWithValue("@pop", c.CourseStudentPopulation);
                cmd.Parameters.AddWithValue("@level", (object)c.CourseLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@did", (object)c.DepartmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", c.CourseId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCourse(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "DELETE FROM COURSE WHERE CourseId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Course MapCourse(SqlDataReader r) => new Course
        {

            CourseId = (int)r["CourseId"],
            CourseCode = r["CourseCode"].ToString(),
            CourseTitle = r["CourseTitle"].ToString(),
            LecturerId = (int)r["LecturerId"],
            CourseStudentPopulation = (int)r["CourseStudentPopulation"],
            CourseLevel = r["CourseLevel"]?.ToString(),
            DepartmentId = r["DepartmentId"] == DBNull.Value
                              ? (int?)null : (int)r["DepartmentId"],
            Semester = r["Semester"]?.ToString(),
            Duration = HasColumn(r, "Duration") && r["Duration"] != DBNull.Value
                              ? (int)r["Duration"] : 1,
            CourseType = HasColumn(r, "CourseType") && r["CourseType"] != DBNull.Value
             ? r["CourseType"].ToString() : "Lecture",
            IsUWC = HasColumn(r, "IsUWC") && r["IsUWC"] != DBNull.Value
                              && (bool)r["IsUWC"],
            LecturerName = r["LecturerName"].ToString(),
            LecturerAvailability = r["LecturerAvailability"].ToString(),
            DepartmentName = r["DepartmentName"].ToString()

        };
        private bool HasColumn(SqlDataReader r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (r.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        public void ReassignCourse(int courseId, int newLecturerId)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE COURSE SET LecturerId=@lid WHERE CourseId=@cid", con))
            {
                cmd.Parameters.AddWithValue("@lid", newLecturerId);
                cmd.Parameters.AddWithValue("@cid", courseId);
                cmd.ExecuteNonQuery();
            }
        }

        // ── TIMETABLE ────────────────────────────────────────────────
        public void SaveTimetable(List<Assignment> assignments)
        {
            using (var con = Open())
            using (var tx = con.BeginTransaction())
            {
                try
                {
                    new SqlCommand("DELETE FROM TIMETABLE", con, tx).ExecuteNonQuery();

                    var ins = new SqlCommand(
                        "INSERT INTO TIMETABLE (CourseId, RoomId, TimeId) VALUES (@c, @r, @t)",
                        con, tx);
                    ins.Parameters.Add("@c", SqlDbType.Int);
                    ins.Parameters.Add("@r", SqlDbType.Int);
                    ins.Parameters.Add("@t", SqlDbType.Int);

                    foreach (var a in assignments)
                    {
                        ins.Parameters["@c"].Value = a.Course.CourseId;
                        ins.Parameters["@r"].Value = a.Room.RoomId;
                        ins.Parameters["@t"].Value = a.TimeSlot.TimeId;
                        ins.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public List<Assignment> GetSavedTimetable()
        {
            var list = new List<Assignment>();
            var sql =
                @"SELECT c.CourseId, c.CourseCode, c.CourseTitle, c.LecturerId,
                         c.CourseStudentPopulation, c.CourseLevel, c.DepartmentId,
                         l.LecturerName, l.LecturerAvailability,
                         ISNULL(d.DepartmentName,'') AS DepartmentName,
                         r.RoomId, r.RoomName, r.RoomCapacity, r.RoomType, r.RoomAvailability,
                         t.TimeId, t.Day, t.StartTime, t.EndTime
                  FROM TIMETABLE tt
                  JOIN COURSE   c ON tt.CourseId = c.CourseId
                  JOIN LECTURER l ON c.LecturerId = l.LecturerId
                  LEFT JOIN DEPARTMENT d ON c.DepartmentId = d.DepartmentId
                  JOIN ROOM     r ON tt.RoomId = r.RoomId
                  JOIN TIMESLOT t ON tt.TimeId  = t.TimeId";

            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                    list.Add(new Assignment
                    {
                        Course = MapCourse(rd),
                        Room = MapRoom(rd),
                        TimeSlot = MapSlot(rd)
                    });
            return list;
        }
        public Assignment GetTimetableEntryById(int timetableId)
        {
            var sql = @"SELECT c.CourseId, c.CourseCode, c.CourseTitle, c.LecturerId,
                       c.CourseStudentPopulation, c.CourseLevel, c.DepartmentId,
                       c.Semester, c.Duration, c.IsUWC,
                       l.LecturerName, l.LecturerAvailability,
                       ISNULL(d.DepartmentName,'') AS DepartmentName,
                       r.RoomId, r.RoomName, r.RoomCapacity, r.RoomType, r.RoomAvailability,
                       t.TimeId, t.Day, t.StartTime, t.EndTime,
                       tt.TimetableId
                FROM TIMETABLE tt
                JOIN COURSE c ON tt.CourseId = c.CourseId
                JOIN LECTURER l ON c.LecturerId = l.LecturerId
                LEFT JOIN DEPARTMENT d ON c.DepartmentId = d.DepartmentId
                JOIN ROOM r ON tt.RoomId = r.RoomId
                JOIN TIMESLOT t ON tt.TimeId = t.TimeId
                WHERE tt.TimetableId = @id";
            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@id", timetableId);
                using (var rd = cmd.ExecuteReader())
                    if (rd.Read())
                        return new Assignment
                        {
                            TimetableId = (int)rd["TimetableId"],
                            Course = MapCourse(rd),
                            Room = MapRoom(rd),
                            TimeSlot = MapSlot(rd)
                        };
            }
            return null;
        }

        public void UpdateTimetableEntry(int timetableId, int roomId, int timeId)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE TIMETABLE SET RoomId=@r, TimeId=@t WHERE TimetableId=@id", con))
            {
                cmd.Parameters.AddWithValue("@r", roomId);
                cmd.Parameters.AddWithValue("@t", timeId);
                cmd.Parameters.AddWithValue("@id", timetableId);
                cmd.ExecuteNonQuery();
            }
        }
        // ── USERS ────────────────────────────────────────────────────
        public User GetUserByUsername(string username)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM USERS WHERE Username=@u", con))
            {
                cmd.Parameters.AddWithValue("@u", username);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapUser(r) : null;
            }
        }

        public void InsertUser(User u)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"INSERT INTO USERS (Username, PasswordHash, FullName, Role)
          VALUES (@u, @p, @f, @r)", con))
            {
                cmd.Parameters.AddWithValue("@u", u.Username);
                cmd.Parameters.AddWithValue("@p", u.PasswordHash);
                cmd.Parameters.AddWithValue("@f", u.FullName);
                cmd.Parameters.AddWithValue("@r", u.Role ?? "Admin");
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePassword(int userId, string newHash)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "UPDATE USERS SET PasswordHash=@p WHERE UserId=@id", con))
            {
                cmd.Parameters.AddWithValue("@p", newHash);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        private User MapUser(SqlDataReader r) => new User
        {
            UserId = (int)r["UserId"],
            Username = r["Username"].ToString(),
            PasswordHash = r["PasswordHash"].ToString(),
            FullName = r["FullName"].ToString(),
            Role = r["Role"].ToString(),
            CreatedAt = (DateTime)r["CreatedAt"]
        };

        // ── SEMESTER FILTERING ───────────────────────────────────────
        public List<Course> GetCoursesBySemester(string semester)
        {
            var list = new List<Course>();
            if (string.IsNullOrWhiteSpace(semester))
                return list;

            using (var con = Open())
            using (var cmd = new SqlCommand(
                COURSE_SQL + " WHERE c.Semester = @s ORDER BY c.DepartmentId, c.CourseLevel, c.CourseCode", con))
            {
                cmd.Parameters.AddWithValue("@s", semester);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(MapCourse(r));
            }
            return list;
        }

        // ── TIMETABLE SESSIONS ───────────────────────────────────────
        public int CreateSession(TimetableSession session)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                @"INSERT INTO TIMETABLE_SESSION
          (SessionName, AcademicYear, Semester, GeneratedBy, TotalCourses, IsActive)
          OUTPUT INSERTED.SessionId
          VALUES (@name, @year, @sem, @by, @total, 1)", con))
            {
                cmd.Parameters.AddWithValue("@name", session.SessionName);
                cmd.Parameters.AddWithValue("@year", session.AcademicYear);
                cmd.Parameters.AddWithValue("@sem", session.Semester);
                cmd.Parameters.AddWithValue("@by", session.GeneratedBy ?? "Admin");
                cmd.Parameters.AddWithValue("@total", session.TotalCourses);
                return (int)cmd.ExecuteScalar();
            }
        }

        public List<TimetableSession> GetAllSessions()
        {
            var list = new List<TimetableSession>();
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM TIMETABLE_SESSION ORDER BY GeneratedAt DESC", con))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new TimetableSession
                    {
                        SessionId = (int)r["SessionId"],
                        SessionName = r["SessionName"].ToString(),
                        AcademicYear = r["AcademicYear"].ToString(),
                        Semester = r["Semester"].ToString(),
                        GeneratedAt = (DateTime)r["GeneratedAt"],
                        GeneratedBy = r["GeneratedBy"].ToString(),
                        TotalCourses = (int)r["TotalCourses"],
                        IsActive = (bool)r["IsActive"]
                    });
            return list;
        }

        public TimetableSession GetSessionById(int id)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(
                "SELECT * FROM TIMETABLE_SESSION WHERE SessionId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        return new TimetableSession
                        {
                            SessionId = (int)r["SessionId"],
                            SessionName = r["SessionName"].ToString(),
                            AcademicYear = r["AcademicYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            GeneratedAt = (DateTime)r["GeneratedAt"],
                            GeneratedBy = r["GeneratedBy"].ToString(),
                            TotalCourses = (int)r["TotalCourses"],
                            IsActive = (bool)r["IsActive"]
                        };
            }
            return null;
        }

        public void DeleteSession(int id)
        {
            using (var con = Open())
            {
                using (var cmd = new SqlCommand(
                    "DELETE FROM TIMETABLE WHERE SessionId=@id", con))
                { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }

                using (var cmd = new SqlCommand(
                    "DELETE FROM TIMETABLE_SESSION WHERE SessionId=@id", con))
                { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
            }
        }

        public void SaveTimetableSession(List<Assignment> assignments, int sessionId)
        {
            using (var con = Open())
            using (var tx = con.BeginTransaction())
            {
                try
                {
                    var del = new SqlCommand(
                        "DELETE FROM TIMETABLE WHERE SessionId=@sid", con, tx);
                    del.Parameters.AddWithValue("@sid", sessionId);
                    del.ExecuteNonQuery();

                    var ins = new SqlCommand(
                        "INSERT INTO TIMETABLE (CourseId,RoomId,TimeId,SessionId) VALUES (@c,@r,@t,@s)",
                        con, tx);
                    ins.Parameters.Add("@c", SqlDbType.Int);
                    ins.Parameters.Add("@r", SqlDbType.Int);
                    ins.Parameters.Add("@t", SqlDbType.Int);
                    ins.Parameters.Add("@s", SqlDbType.Int);

                    foreach (var a in assignments)
                    {
                        ins.Parameters["@c"].Value = a.Course.CourseId;
                        ins.Parameters["@r"].Value = a.Room.RoomId;
                        ins.Parameters["@t"].Value = a.TimeSlot.TimeId;
                        ins.Parameters["@s"].Value = sessionId;
                        ins.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public List<Assignment> GetTimetableBySession(int sessionId)
        {
            var list = new List<Assignment>();
            var sql = @"SELECT tt.TimetableId,
                        c.CourseId, c.CourseCode, c.CourseTitle, c.LecturerId,
                        c.CourseStudentPopulation, c.CourseLevel, c.DepartmentId,
                        c.Semester, c.Duration, c.IsUWC,
                        l.LecturerName, l.LecturerAvailability,
                        ISNULL(d.DepartmentName,'') AS DepartmentName,
                        r.RoomId, r.RoomName, r.RoomCapacity,
                        r.RoomType, r.RoomAvailability,
                        t.TimeId, t.Day, t.StartTime, t.EndTime
                 FROM TIMETABLE tt
                 JOIN COURSE   c ON tt.CourseId = c.CourseId
                 JOIN LECTURER l ON c.LecturerId = l.LecturerId
                 LEFT JOIN DEPARTMENT d ON c.DepartmentId = d.DepartmentId
                 JOIN ROOM     r ON tt.RoomId = r.RoomId
                 JOIN TIMESLOT t ON tt.TimeId  = t.TimeId
                 WHERE tt.SessionId = @sid";
            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@sid", sessionId);
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                        list.Add(new Assignment
                        {
                            TimetableId = (int)rd["TimetableId"],
                            Course = MapCourse(rd),
                            Room = MapRoom(rd),
                            TimeSlot = MapSlot(rd)
                        });
            }
            return list;
        }
    }
}


