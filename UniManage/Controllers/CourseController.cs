using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.Models.ViewModels;

namespace UniManage.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Course
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Lecturer)
                .ToListAsync();
            return View(courses);
        }

        // GET: Course/Browse
        [AllowAnonymous]
        public async Task<IActionResult> Browse()
        {
            var courses = await _context.Courses
                .Include(c => c.Lecturer)
                .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
                .Where(c => c.IsActive && c.CurrentEnrollment < c.MaxEnrollment)
                .ToListAsync();

            return View(courses);
        }

        // GET: Course/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Lecturer)
                .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isEnrolled = false;
            var canEnroll = false;

            if (user != null && user.Role == UserRole.Student)
            {
                isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == user.Id && e.CourseId == id && e.Status == EnrollmentStatus.Active);

                if (!isEnrolled)
                {
                    canEnroll = await ValidatePrerequisites(user.Id, course);
                }
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.CanEnroll = canEnroll;

            return View(course);
        }

        // GET: Course/Create
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create()
        {
            var lecturers = await GetAvailableLecturers();
            ViewBag.Lecturers = lecturers;
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var course = new Course
                {
                    CourseCode = model.CourseCode,
                    Title = model.Title,
                    Description = model.Description,
                    Credits = model.Credits,
                    MaxEnrollment = model.MaxEnrollment,
                    LecturerId = model.LecturerId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = course.Id });
            }

            var lecturers = await GetAvailableLecturers();
            ViewBag.Lecturers = lecturers;
            return View(model);
        }

        // GET: Course/Edit/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var model = new EditCourseViewModel
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                Title = course.Title,
                Description = course.Description,
                Credits = course.Credits,
                MaxEnrollment = course.MaxEnrollment,
                IsActive = course.IsActive,
                LecturerId = course.LecturerId
            };

            var lecturers = await GetAvailableLecturers();
            ViewBag.Lecturers = lecturers;
            return View(model);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(EditCourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses.FindAsync(model.Id);
                if (course == null)
                {
                    return NotFound();
                }

                course.Title = model.Title;
                course.Description = model.Description;
                course.Credits = model.Credits;
                course.MaxEnrollment = model.MaxEnrollment;
                course.IsActive = model.IsActive;
                course.LecturerId = model.LecturerId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = course.Id });
            }

            var lecturers = await GetAvailableLecturers();
            ViewBag.Lecturers = lecturers;
            return View(model);
        }

        // POST: Course/Enroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(Browse));
            }
            
            if (!course.IsActive)
            {
                TempData["Error"] = "Course is not active.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            if (course.CurrentEnrollment >= course.MaxEnrollment)
            {
                TempData["Error"] = $"Course is full. Current enrollment: {course.CurrentEnrollment}, Max: {course.MaxEnrollment}";
                return RedirectToAction(nameof(Details), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Student)
            {
                return Forbid();
            }

            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == id);

            if (existingEnrollment != null)
            {
                TempData["Error"] = "You are already enrolled in this course.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var canEnroll = await ValidatePrerequisites(user.Id, course);
            if (!canEnroll)
            {
                TempData["Error"] = "You do not meet the prerequisites for this course.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var enrollment = new Enrollment
            {
                StudentId = user.Id,
                CourseId = id,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active
            };

            _context.Enrollments.Add(enrollment);
            course.CurrentEnrollment++;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Successfully enrolled in the course!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Course/Drop/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Drop(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == _userManager.GetUserId(User));

            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.Status = EnrollmentStatus.Dropped;
            enrollment.Course.CurrentEnrollment--;

            await _context.SaveChangesAsync();

            TempData["Success"] = "You have dropped the course.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<bool> ValidatePrerequisites(string studentId, Course course)
        {
            if (!course.Prerequisites.Any())
            {
                return true;
            }

            var completedCourses = await _context.Enrollments
                .Where(e => e.StudentId == studentId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed))
                .Select(e => e.CourseId)
                .ToListAsync();

            var prerequisiteIds = course.Prerequisites.Select(p => p.PrerequisiteCourseId).ToList();

            return prerequisiteIds.All(prereqId => completedCourses.Contains(prereqId));
        }

        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetAvailableLecturers()
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            var lecturerList = lecturers
                .OrderBy(l => l.LastName)
                .Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = l.Id,
                    Text = $"{l.FirstName} {l.LastName} ({l.Email})"
                })
                .ToList();
            
            lecturerList.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "-- Select Lecturer --" });
            return lecturerList;
        }
    }
}
