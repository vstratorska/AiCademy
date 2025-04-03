﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AiCademy.Repository;
using AiCademy.Domain.Models;
using Microsoft.AspNetCore.Identity;
using AiCademy.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AiCademy.Service.Interface;

namespace AiCademy.Web.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICourseService _courseService;
        private readonly ILessonService _lessonService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICourseService courseService, ILessonService lessonService)
        {
            _context = context;
            _userManager = userManager;
            _courseService = courseService;
            _lessonService = lessonService;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            return View(_courseService.GetCourses());
        }

        // GET: UserCourses
        [Authorize]
        public async Task<IActionResult> UserCourses()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users
                .Include(u => u.UserCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);


            return View(user?.UserCourses.ToList() ?? new List<Course>());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = _courseService.GetCourseById(id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Duration,Id")] Course course)
        {
            if (ModelState.IsValid)
            {
                course.Id = Guid.NewGuid();
                _courseService.CreateNewCourse(course);
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = _courseService.GetCourseById(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Title,Description,Duration,Id")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _courseService.UpdateCourse(course);

                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = _courseService.GetCourseById(id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            _courseService.DeleteCourse(id);
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCourseToUserList(Guid id)
        {

            var course = _courseService.GetCourseById(id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                .Include(u => u.UserCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);


            user.UserCourses ??= new List<Course>();

            if (!user.UserCourses.Any(c => c.Id == id))
            {
                user.UserCourses.Add(course);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(UserCourses));
        }

        public IActionResult AddLesson(Guid courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLesson(Guid courseId, [Bind("Name")] Lesson lesson, IFormFile LessonFile)
        {
            if (ModelState.IsValid)
            {
                if (LessonFile != null && LessonFile.Length > 0)
                {
                    var originalFileName = Path.GetFileName(LessonFile.FileName);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(LessonFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await LessonFile.CopyToAsync(stream);
                    }

                    lesson.FilePath = $"/uploads/{fileName}";
                    lesson.Name += $" ({originalFileName})";
                }

                lesson.Id = Guid.NewGuid();
                lesson.CourseId = courseId;

                
                await _courseService.AddLessonToCourse(courseId, lesson);

                return RedirectToAction("Details", new { id = courseId });
            }

            ViewBag.CourseId = courseId;
            return View(lesson);
        }

        public async Task<IActionResult> EditLesson(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = _lessonService.GetLessonById(id);
            if (lesson == null)
            {
                return NotFound();
            }
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Guid id, [Bind("Id,Name,FilePath,CourseId")] Lesson lesson, IFormFile LessonFile)
        {
            if (id != lesson.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (LessonFile != null && LessonFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(lesson.FilePath))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lesson.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var originalFileName = Path.GetFileName(LessonFile.FileName);

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(LessonFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await LessonFile.CopyToAsync(stream);
                        }

                        lesson.FilePath = $"/uploads/{fileName}";
                        lesson.Name = lesson.Name.Split(" (")[0] + $" ({originalFileName})";
                    }

                    _lessonService.UpdateLesson(lesson);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Lessons.Any(e => e.Id == lesson.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", new { id = lesson.CourseId });
            }
            return View(lesson);
        }

        public async Task<IActionResult> LessonDetails(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = _lessonService.GetLessonById(id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(Guid id)
        {
            var lesson = _lessonService.GetLessonById(id);
            if (lesson != null)
            {
                _lessonService.DeleteLesson(id);
                return RedirectToAction("Details", new { id = lesson.CourseId });
            }
            return NotFound();
        }

    }
}
