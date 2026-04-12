using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.Models.ViewModels;

namespace UniManage.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == user.Id || m.SenderId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            ViewBag.CurrentUserId = user.Id;
            return View(messages);
        }

        // GET: Message/Inbox
        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Message/Sent
        [HttpGet]
        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Message/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || (message.SenderId != user.Id && message.ReceiverId != user.Id))
            {
                return Forbid();
            }

            // Mark as read if the user is the receiver
            if (message.ReceiverId == user.Id && !message.ReadAt.HasValue)
            {
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // GET: Message/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var recipients = await GetAvailableRecipients(user);
            ViewBag.Recipients = recipients;

            return View();
        }

        // POST: Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                var message = new Message
                {
                    SenderId = user.Id,
                    ReceiverId = model.ReceiverId,
                    Subject = model.Subject,
                    Content = model.Content,
                    SentAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Message sent successfully!";
                return RedirectToAction(nameof(Sent));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var recipients = await GetAvailableRecipients(currentUser!);
            ViewBag.Recipients = recipients;
            return View(model);
        }

        // GET: Message/Reply/5
        [HttpGet]
        public async Task<IActionResult> Reply(int id)
        {
            var originalMessage = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (originalMessage == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || originalMessage.ReceiverId != user.Id)
            {
                return Forbid();
            }

            var model = new CreateMessageViewModel
            {
                ReceiverId = originalMessage.SenderId,
                Subject = $"Re: {originalMessage.Subject}",
                Content = $"\n\n--- Original Message ---\nFrom: {originalMessage.Sender.FirstName} {originalMessage.Sender.LastName}\nDate: {originalMessage.SentAt:MMM dd, yyyy HH:mm}\n\n{originalMessage.Content}"
            };

            ViewBag.RecipientName = $"{originalMessage.Sender.FirstName} {originalMessage.Sender.LastName}";
            return View("Create", model);
        }

        // POST: Message/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || (message.SenderId != user.Id && message.ReceiverId != user.Id))
            {
                return Forbid();
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetAvailableRecipients(ApplicationUser user)
        {
            var recipients = new List<SelectListItem>();

            if (user.Role == UserRole.Student)
            {
                // Students can message lecturers of their enrolled courses
                var lecturers = await _context.Enrollments
                    .Where(e => e.StudentId == user.Id && e.Status == EnrollmentStatus.Active)
                    .Select(e => e.Course.Lecturer)
                    .Distinct()
                    .ToListAsync();

                foreach (var lecturer in lecturers)
                {
                    recipients.Add(new SelectListItem
                    {
                        Value = lecturer.Id,
                        Text = $"{lecturer.FirstName} {lecturer.LastName} (Lecturer - {lecturer.Email})"
                    });
                }
            }
            else if (user.Role == UserRole.Lecturer)
            {
                // Lecturers can message students in their courses and other lecturers/administrators
                var students = await _context.Courses
                    .Where(c => c.LecturerId == user.Id)
                    .SelectMany(c => c.Enrollments)
                    .Select(e => e.Student)
                    .ToListAsync();

                foreach (var student in students)
                {
                    recipients.Add(new SelectListItem
                    {
                        Value = student.Id,
                        Text = $"{student.FirstName} {student.LastName} (Student - {student.Email})"
                    });
                }

                // Add other lecturers and administrators
                var otherStaff = await _userManager.Users
                    .Where(u => u.Id != user.Id && (u.Role == UserRole.Lecturer || u.Role == UserRole.Administrator))
                    .ToListAsync();

                foreach (var staff in otherStaff)
                {
                    recipients.Add(new SelectListItem
                    {
                        Value = staff.Id,
                        Text = $"{staff.FirstName} {staff.LastName} ({staff.Role} - {staff.Email})"
                    });
                }
            }
            else // Administrator
            {
                // Administrators can message everyone
                var allUsers = await _userManager.Users
                    .Where(u => u.Id != user.Id)
                    .ToListAsync();

                foreach (var otherUser in allUsers)
                {
                    recipients.Add(new SelectListItem
                    {
                        Value = otherUser.Id,
                        Text = $"{otherUser.FirstName} {otherUser.LastName} ({otherUser.Role} - {otherUser.Email})"
                    });
                }
            }

            return recipients.OrderBy(r => r.Text).ToList();
        }
    }
}
