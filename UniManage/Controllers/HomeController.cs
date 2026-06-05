using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniManage.Models;
using UniManage.Data;
using UniManage.Models.ViewModels;

namespace UniManage.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            switch (user.Role)
            {
                case UserRole.Student:
                    return RedirectToAction("StudentDashboard");
                case UserRole.Lecturer:
                    return RedirectToAction("LecturerDashboard");
                case UserRole.Administrator:
                    return RedirectToAction("AdministratorDashboard");
                default:
                    return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Lecturer)
                .Where(e => e.StudentId == user.Id && e.Status == EnrollmentStatus.Active)
                .ToListAsync();

            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.IsActive)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .Where(s => s.StudentId == user.Id)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            ViewBag.Assignments = assignments;
            ViewBag.Submissions = submissions;

            return View();
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .Where(c => c.LecturerId == user.Id)
                .ToListAsync();

            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                .Where(a => a.LecturerId == user.Id)
                .ToListAsync();

            ViewBag.Courses = courses;
            ViewBag.Assignments = assignments;

            return View();
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AdministratorDashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();
            var totalAssignments = await _context.Assignments.CountAsync();

            var students = await _userManager.GetUsersInRoleAsync("Student");
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            var administrators = await _userManager.GetUsersInRoleAsync("Administrator");

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalEnrollments = totalEnrollments;
            ViewBag.TotalAssignments = totalAssignments;
            ViewBag.StudentCount = students.Count;
            ViewBag.LecturerCount = lecturers.Count;
            ViewBag.AdministratorCount = administrators.Count;

            return View();
        }

        [Authorize(Roles = "Administrator,Lecturer")]
        public async Task<IActionResult> Reports()
        {
            var model = new SystemReportViewModel();

            // Basic stats
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            var administrators = await _userManager.GetUsersInRoleAsync("Administrator");

            model.TotalStudents = students.Count;
            model.TotalLecturers = lecturers.Count;
            model.TotalAdministrators = administrators.Count;
            
            model.TotalCourses = await _context.Courses.CountAsync();
            model.TotalAssignments = await _context.Assignments.CountAsync();
            model.TotalSubmissions = await _context.Submissions.CountAsync();
            model.TotalEnrollments = await _context.Enrollments.CountAsync();

            // Top enrolled courses
            model.TopEnrolledCourses = await _context.Courses
                .Select(c => new CourseEnrollmentReport
                {
                    CourseName = c.Title,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(5)
                .ToListAsync();

            // Lecturer performance
            foreach (var lecturer in lecturers)
            {
                var courseCount = await _context.Courses.CountAsync(c => c.LecturerId == lecturer.Id);
                var assignmentCount = await _context.Assignments.CountAsync(a => a.LecturerId == lecturer.Id);
                
                model.LecturerPerformance.Add(new LecturerPerformanceReport
                {
                    LecturerName = $"{lecturer.FirstName} {lecturer.LastName}",
                    CourseCount = courseCount,
                    AssignmentCount = assignmentCount
                });
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TestRoute()
        {
            return Content("Routing is working");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
