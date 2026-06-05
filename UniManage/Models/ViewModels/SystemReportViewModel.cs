using System.Collections.Generic;

namespace UniManage.Models.ViewModels
{
    public class SystemReportViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalAdministrators { get; set; }
        public int TotalCourses { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalSubmissions { get; set; }
        public int TotalEnrollments { get; set; }

        public List<CourseEnrollmentReport> TopEnrolledCourses { get; set; } = new List<CourseEnrollmentReport>();
        public List<LecturerPerformanceReport> LecturerPerformance { get; set; } = new List<LecturerPerformanceReport>();
    }

    public class CourseEnrollmentReport
    {
        public string CourseName { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
    }

    public class LecturerPerformanceReport
    {
        public string LecturerName { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public int AssignmentCount { get; set; }
    }
}
