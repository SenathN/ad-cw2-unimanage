using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniManage.Models;

namespace UniManage.Data
{
    public class ApplicationDbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ApplicationDbInitializer(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedDataAsync()
        {
            // Apply any pending migrations and ensure the database schema is current
            await _context.Database.MigrateAsync();

            // Create roles if they don't exist
            await CreateRolesAsync();

            // Create users if they don't exist
            await CreateUsersAsync();

            // Create sample courses and assignments if they don't exist
            await CreateSampleDataAsync();
        }

        private async Task CreateRolesAsync()
        {
            string[] roleNames = { "Student", "Lecturer", "Administrator" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private async Task CreateUsersAsync()
        {
            // Create admin user
            var adminUser = await _userManager.FindByEmailAsync("admin@unimanage.edu");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@unimanage.edu",
                    Email = "admin@unimanage.edu",
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = UserRole.Administrator,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Create lecturer users
            var lecturers = new[]
            {
                new { Email = "john.smith@unimanage.edu", FirstName = "John", LastName = "Smith", Expertise = "Computer Science" },
                new { Email = "sarah.jones@unimanage.edu", FirstName = "Sarah", LastName = "Jones", Expertise = "Mathematics" },
                new { Email = "michael.brown@unimanage.edu", FirstName = "Michael", LastName = "Brown", Expertise = "Physics" }
            };

            foreach (var lecturer in lecturers)
            {
                var user = await _userManager.FindByEmailAsync(lecturer.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = lecturer.Email,
                        Email = lecturer.Email,
                        FirstName = lecturer.FirstName,
                        LastName = lecturer.LastName,
                        Role = UserRole.Lecturer,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    var result = await _userManager.CreateAsync(user, "Lecturer123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Lecturer");
                    }
                }
            }

            // Create student users
            var students = new[]
            {
                new { Email = "alice.wilson@unimanage.edu", FirstName = "Alice", LastName = "Wilson" },
                new { Email = "bob.davis@unimanage.edu", FirstName = "Bob", LastName = "Davis" },
                new { Email = "charlie.miller@unimanage.edu", FirstName = "Charlie", LastName = "Miller" },
                new { Email = "diana.moore@unimanage.edu", FirstName = "Diana", LastName = "Moore" },
                new { Email = "edward.taylor@unimanage.edu", FirstName = "Edward", LastName = "Taylor" }
            };

            foreach (var student in students)
            {
                var user = await _userManager.FindByEmailAsync(student.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = student.Email,
                        Email = student.Email,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        Role = UserRole.Student,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    var result = await _userManager.CreateAsync(user, "Student123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                    }
                }
            }
        }

        private async Task CreateSampleDataAsync()
        {
            // Check if courses already exist
            if (await _context.Courses.AnyAsync())
            {
                return; // Database already seeded
            }

            // Get lecturers
            var johnSmith = await _userManager.FindByEmailAsync("john.smith@unimanage.edu");
            var sarahJones = await _userManager.FindByEmailAsync("sarah.jones@unimanage.edu");
            var michaelBrown = await _userManager.FindByEmailAsync("michael.brown@unimanage.edu");

            // Create courses
            var courses = new[]
            {
                new Course
                {
                    CourseCode = "CS101",
                    Title = "Introduction to Programming",
                    Description = "Fundamentals of programming using C#. Learn variables, control structures, functions, and object-oriented programming concepts.",
                    Credits = 3,
                    MaxEnrollment = 30,
                    CurrentEnrollment = 0,
                    LecturerId = johnSmith!.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Course
                {
                    CourseCode = "CS201",
                    Title = "Data Structures and Algorithms",
                    Description = "Advanced programming concepts including arrays, linked lists, stacks, queues, trees, and sorting algorithms.",
                    Credits = 4,
                    MaxEnrollment = 25,
                    CurrentEnrollment = 0,
                    LecturerId = johnSmith!.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Course
                {
                    CourseCode = "MATH101",
                    Title = "Calculus I",
                    Description = "Introduction to differential and integral calculus, limits, derivatives, and applications.",
                    Credits = 4,
                    MaxEnrollment = 35,
                    CurrentEnrollment = 0,
                    LecturerId = sarahJones!.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Course
                {
                    CourseCode = "PHYS101",
                    Title = "Introduction to Physics",
                    Description = "Basic concepts in mechanics, thermodynamics, waves, and modern physics.",
                    Credits = 3,
                    MaxEnrollment = 30,
                    CurrentEnrollment = 0,
                    LecturerId = michaelBrown!.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            await _context.Courses.AddRangeAsync(courses);
            await _context.SaveChangesAsync();

            // Add prerequisites (CS201 requires CS101)
            var cs101 = courses.First(c => c.CourseCode == "CS101");
            var cs201 = courses.First(c => c.CourseCode == "CS201");
            
            _context.CoursePrerequisites.Add(new CoursePrerequisite
            {
                CourseId = cs201.Id,
                PrerequisiteCourseId = cs101.Id
            });

            await _context.SaveChangesAsync();

            // Get students
            var alice = await _userManager.FindByEmailAsync("alice.wilson@unimanage.edu");
            var bob = await _userManager.FindByEmailAsync("bob.davis@unimanage.edu");
            var charlie = await _userManager.FindByEmailAsync("charlie.miller@unimanage.edu");
            var diana = await _userManager.FindByEmailAsync("diana.moore@unimanage.edu");
            var edward = await _userManager.FindByEmailAsync("edward.taylor@unimanage.edu");

            // Create enrollments
            var enrollments = new[]
            {
                new Enrollment { StudentId = alice!.Id, CourseId = cs101.Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = bob!.Id, CourseId = cs101.Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = charlie!.Id, CourseId = cs101.Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = diana!.Id, CourseId = cs101.Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = alice!.Id, CourseId = courses.First(c => c.CourseCode == "MATH101").Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = bob!.Id, CourseId = courses.First(c => c.CourseCode == "MATH101").Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = charlie!.Id, CourseId = courses.First(c => c.CourseCode == "PHYS101").Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = diana!.Id, CourseId = courses.First(c => c.CourseCode == "PHYS101").Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active },
                new Enrollment { StudentId = edward!.Id, CourseId = courses.First(c => c.CourseCode == "PHYS101").Id, EnrolledAt = DateTime.UtcNow, Status = EnrollmentStatus.Active }
            };

            await _context.Enrollments.AddRangeAsync(enrollments);
            await _context.SaveChangesAsync();

            // Update course enrollment counts
            cs101.CurrentEnrollment = 4;
            courses.First(c => c.CourseCode == "MATH101").CurrentEnrollment = 2;
            courses.First(c => c.CourseCode == "PHYS101").CurrentEnrollment = 3;
            await _context.SaveChangesAsync();

            // Create assignments
            var assignments = new[]
            {
                new Assignment
                {
                    Title = "Hello World Program",
                    Description = "Write a simple program that displays 'Hello, World!' and your name. Include proper comments and follow coding standards.",
                    CourseId = cs101.Id,
                    LecturerId = johnSmith!.Id,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    MaxPoints = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Assignment
                {
                    Title = "Basic Calculator",
                    Description = "Create a console application that performs basic arithmetic operations (add, subtract, multiply, divide) with user input.",
                    CourseId = cs101.Id,
                    LecturerId = johnSmith!.Id,
                    DueDate = DateTime.UtcNow.AddDays(14),
                    MaxPoints = 150,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Assignment
                {
                    Title = "Limits and Derivatives",
                    Description = "Solve 10 problems involving limits and derivatives. Show all your work and explain your reasoning.",
                    CourseId = courses.First(c => c.CourseCode == "MATH101").Id,
                    LecturerId = sarahJones!.Id,
                    DueDate = DateTime.UtcNow.AddDays(10),
                    MaxPoints = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Assignments.AddRangeAsync(assignments);
            await _context.SaveChangesAsync();

            // Create sample submissions
            var firstAssignment = assignments.First();
            var submissions = new[]
            {
                new Submission
                {
                    AssignmentId = firstAssignment.Id,
                    StudentId = alice!.Id,
                    Content = "Console.WriteLine('Hello, World!');\nConsole.WriteLine('My name is Alice Wilson');",
                    SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    Grade = 95,
                    Feedback = "Excellent work! Clean code and good comments.",
                    Status = SubmissionStatus.Graded
                },
                new Submission
                {
                    AssignmentId = firstAssignment.Id,
                    StudentId = bob!.Id,
                    Content = "print('Hello, World!')\nprint('My name is Bob Davis')",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Status = SubmissionStatus.Submitted
                }
            };

            await _context.Submissions.AddRangeAsync(submissions);
            await _context.SaveChangesAsync();
        }
    }
}
