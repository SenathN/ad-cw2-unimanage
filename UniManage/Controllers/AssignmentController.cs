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
    public class AssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Assignment/Index?courseId=5
        [HttpGet]
        public async Task<IActionResult> Index(int? courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            IQueryable<Assignment> assignments;

            if (user.Role == UserRole.Student)
            {
                // Students see assignments for courses they're enrolled in
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == user.Id && e.Status == EnrollmentStatus.Active)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                assignments = _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.Submissions)
                    .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.IsActive);
            }
            else if (user.Role == UserRole.Lecturer)
            {
                // Lecturers see assignments for courses they teach
                assignments = _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                    .Where(a => a.LecturerId == user.Id);
            }
            else // Administrator
            {
                // Administrators see all assignments
                assignments = _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student);
            }

            if (courseId.HasValue)
            {
                assignments = assignments.Where(a => a.CourseId == courseId.Value);
            }

            var assignmentList = await assignments.OrderByDescending(a => a.CreatedAt).ToListAsync();

            ViewBag.CourseId = courseId;
            return View(assignmentList);
        }

        // GET: Assignment/Create?courseId=5
        [HttpGet]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Create(int? courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            var courses = await GetAvailableCourses(user!);

            ViewBag.CourseId = courseId;
            ViewBag.Courses = courses.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.CourseCode} - {c.Title}"
            }).ToList();

            var model = new CreateAssignmentViewModel();
            if (courseId.HasValue)
            {
                model.CourseId = courseId.Value;
            }

            return View(model);
        }

        // POST: Assignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Create(CreateAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var course = await _context.Courses.FindAsync(model.CourseId);

                if (course == null || (user!.Role != UserRole.Administrator && course.LecturerId != user.Id))
                {
                    return Forbid();
                }

                var assignment = new Assignment
                {
                    Title = model.Title,
                    Description = model.Description,
                    CourseId = model.CourseId,
                    LecturerId = user.Id,
                    DueDate = model.DueDate,
                    MaxPoints = model.MaxPoints,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var courses = await GetAvailableCourses(currentUser!);
            ViewBag.Courses = courses.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.CourseCode} - {c.Title}"
            }).ToList();
            return View(model);
        }

        // GET: Assignment/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Lecturer)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var hasAccess = await HasAssignmentAccess(user!, assignment);

            if (!hasAccess)
            {
                return Forbid();
            }

            var userSubmission = assignment.Submissions.FirstOrDefault(s => s.StudentId == user!.Id);
            ViewBag.UserSubmission = userSubmission;

            return View(assignment);
        }

        // GET: Assignment/Submit/5
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == user!.Id && e.CourseId == assignment.CourseId && e.Status == EnrollmentStatus.Active);

            if (!isEnrolled)
            {
                return Forbid();
            }

            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == user!.Id);

            var model = new SubmitAssignmentViewModel
            {
                AssignmentId = id,
                AssignmentTitle = assignment.Title,
                CourseTitle = assignment.Course.Title,
                DueDate = assignment.DueDate,
                MaxPoints = assignment.MaxPoints,
                Content = existingSubmission?.Content ?? string.Empty,
                IsLate = assignment.DueDate < DateTime.Now
            };

            return View(model);
        }

        // POST: Assignment/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(SubmitAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var assignment = await _context.Assignments.FindAsync(model.AssignmentId);
                if (assignment == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                var existingSubmission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == model.AssignmentId && s.StudentId == user!.Id);

                var status = assignment.DueDate < DateTime.Now ? SubmissionStatus.Late : SubmissionStatus.Submitted;

                if (existingSubmission != null)
                {
                    existingSubmission.Content = model.Content;
                    existingSubmission.SubmittedAt = DateTime.UtcNow;
                    existingSubmission.Status = status;
                }
                else
                {
                    existingSubmission = new Submission
                    {
                        AssignmentId = model.AssignmentId,
                        StudentId = user!.Id,
                        Content = model.Content,
                        SubmittedAt = DateTime.UtcNow,
                        Status = status
                    };
                    _context.Submissions.Add(existingSubmission);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
            }

            return View(model);
        }

        // GET: Assignment/Grade/5
        [HttpGet]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Grade(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var hasAccess = await HasAssignmentAccess(user!, submission.Assignment);

            if (!hasAccess)
            {
                return Forbid();
            }

            var model = new GradeSubmissionViewModel
            {
                SubmissionId = id,
                StudentName = $"{submission.Student.FirstName} {submission.Student.LastName}",
                AssignmentTitle = submission.Assignment.Title,
                SubmittedContent = submission.Content,
                SubmittedAt = submission.SubmittedAt,
                MaxPoints = submission.Assignment.MaxPoints,
                Grade = submission.Grade,
                Feedback = submission.Feedback ?? string.Empty
            };

            ViewBag.AssignmentId = submission.AssignmentId;
            return View(model);
        }

        // POST: Assignment/Grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Grade(GradeSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = await _context.Submissions.FindAsync(model.SubmissionId);
                if (submission == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                var assignment = await _context.Assignments.FindAsync(submission.AssignmentId);
                var hasAccess = await HasAssignmentAccess(user!, assignment!);

                if (!hasAccess)
                {
                    return Forbid();
                }

                submission.Grade = model.Grade;
                submission.Feedback = model.Feedback;
                submission.Status = SubmissionStatus.Graded;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = submission.AssignmentId });
            }

            return View(model);
        }

        private async Task<List<Course>> GetAvailableCourses(ApplicationUser user)
        {
            if (user.Role == UserRole.Administrator)
            {
                return await _context.Courses.Where(c => c.IsActive).ToListAsync();
            }
            else // Lecturer
            {
                return await _context.Courses.Where(c => c.LecturerId == user.Id).ToListAsync();
            }
        }

        // GET: Assignment/Edit/5
        [HttpGet]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var hasAccess = await HasAssignmentAccess(user!, assignment);

            if (!hasAccess)
            {
                return Forbid();
            }

            var model = new CreateAssignmentViewModel
            {
                CourseId = assignment.CourseId,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxPoints = assignment.MaxPoints
            };

            ViewBag.AssignmentId = id;
            ViewBag.CourseTitle = assignment.Course.Title;

            return View(model);
        }

        // POST: Assignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer,Administrator")]
        public async Task<IActionResult> Edit(int id, CreateAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var assignment = await _context.Assignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                var hasAccess = await HasAssignmentAccess(user!, assignment);

                if (!hasAccess)
                {
                    return Forbid();
                }

                assignment.Title = model.Title;
                assignment.Description = model.Description;
                assignment.DueDate = model.DueDate;
                assignment.MaxPoints = model.MaxPoints;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = id });
            }

            ViewBag.AssignmentId = id;
            ViewBag.CourseTitle = (await _context.Courses.FindAsync(model.CourseId))?.Title ?? "Unknown";
            return View(model);
        }

        private async Task<bool> HasAssignmentAccess(ApplicationUser user, Assignment assignment)
        {
            return user.Role switch
            {
                UserRole.Student => await _context.Enrollments
                    .AnyAsync(e => e.StudentId == user.Id && e.CourseId == assignment.CourseId && e.Status == EnrollmentStatus.Active),
                UserRole.Lecturer => assignment.LecturerId == user.Id,
                UserRole.Administrator => true,
                _ => false
            };
        }
    }
}
