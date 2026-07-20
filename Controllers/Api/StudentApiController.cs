using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentRecordSystem.Data;
using StudentRecordSystem.Models;

namespace StudentRecordSystem.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/studentapi — everyone logged in can view the full list
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            return await _context.Students.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return student;
        }

          // POST: any logged-in user can add — but only ONE record per student
        [HttpPost]
        public async Task<ActionResult<Student>> CreateStudent(Student student)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            bool isAdmin = User.IsInRole("Admin");
            string currentUserId = _userManager.GetUserId(User);

            // Students can only have one record. Admins are exempt from this check
            // since they might need to create records on behalf of others later.
            if (!isAdmin)
            {
                bool alreadyHasRecord = await _context.Students.AnyAsync(s => s.UserId == currentUserId);
                if (alreadyHasRecord)
                {
                    return BadRequest(new { message = "You already have a student record. Please edit your existing record instead of creating a new one." });
                }
            }

            student.UserId = isAdmin ? student.UserId : currentUserId;

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.StudentId }, student);
        }

        // PUT: only Admin, or the record's own owner, can edit it
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, Student student)
        {
            if (id != student.StudentId) return BadRequest();

            var existing = await _context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (existing == null) return NotFound();

            bool isAdmin = User.IsInRole("Admin");
            string currentUserId = _userManager.GetUserId(User);

            if (!isAdmin && existing.UserId != currentUserId)
                return Forbid(); // 403 — this record belongs to someone else

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Non-admins can't reassign ownership even if they tamper with the request body
            student.UserId = isAdmin ? student.UserId : existing.UserId;

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Students.Any(s => s.StudentId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: only Admin, or the record's own owner, can delete it
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            bool isAdmin = User.IsInRole("Admin");
            string currentUserId = _userManager.GetUserId(User);

            if (!isAdmin && student.UserId != currentUserId)
                return Forbid();

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}