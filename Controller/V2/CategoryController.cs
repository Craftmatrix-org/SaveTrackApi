using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.V2.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly MySQLService _db;

        public CategoryController(MySQLService db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var userCategories = categories.Where(c => c.UserID == userId).OrderBy(c => c.Name).ToList();

                return Ok(userCategories);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving categories", details = ex.Message });
            }
        }

        [HttpGet("positive")]
        public async Task<IActionResult> GetPositiveCategories()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var positiveCategories = categories
                    .Where(c => c.UserID == userId && c.isPositive)
                    .OrderBy(c => c.Name)
                    .ToList();

                return Ok(positiveCategories);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving positive categories", details = ex.Message });
            }
        }

        [HttpGet("negative")]
        public async Task<IActionResult> GetNegativeCategories()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var negativeCategories = categories
                    .Where(c => c.UserID == userId && !c.isPositive)
                    .OrderBy(c => c.Name)
                    .ToList();

                return Ok(negativeCategories);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving negative categories", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var category = categories.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (category == null)
                {
                    return NotFound(new { error = $"Category with ID {id} not found or you don't have permission to access it." });
                }

                return Ok(category);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the category", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateCategoryData(category);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                category.Id = Guid.NewGuid();
                category.UserID = userId;
                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                await _db.PostDataAsync<CategoryDto>("Categories", category);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the category", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryDto category)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateCategoryData(category);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var existingCategory = categories.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (existingCategory == null)
                {
                    return NotFound(new { error = $"Category with ID {id} not found or you don't have permission to access it." });
                }

                category.Id = id;
                category.UserID = userId;
                category.CreatedAt = existingCategory.CreatedAt;
                category.UpdatedAt = DateTime.UtcNow;

                await _db.PutDataAsync<CategoryDto>("Categories", id, category);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the category", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var category = categories.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (category == null)
                {
                    return NotFound(new { error = $"Category with ID {id} not found or you don't have permission to access it." });
                }

                await _db.DeleteDataAsync("Categories", id);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the category", details = ex.Message });
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateCategoryData(CategoryDto category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return (false, "Category name is required and cannot be empty.");
            }

            if (category.Name.Length > 100)
            {
                return (false, "Category name cannot exceed 100 characters.");
            }

            if (!string.IsNullOrEmpty(category.Description) && category.Description.Length > 500)
            {
                return (false, "Description cannot exceed 500 characters.");
            }

            return (true, string.Empty);
        }
    }
}
