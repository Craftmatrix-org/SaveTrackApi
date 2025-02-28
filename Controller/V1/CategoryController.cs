using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;


        public CategoryController(MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            return Ok(categories);
        }

        [HttpGet("specific/{id}")]
        public async Task<IActionResult> GetSpecialCategories(Guid id)
        {
            var categories = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var filtered = categories.Where(c => c.Id == id);
            return Ok(filtered);
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetCategory(Guid uid)
        {
            var category = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var filtered = category.Where(c => c.UserID == uid);
            return Ok(filtered);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto category)
        {
            category.Id = Guid.NewGuid();
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            await _mysqlservice.PostDataAsync<CategoryDto>("Categories", category);
            return Ok(category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryDto category)
        {
            var categoryList = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var checkCategory = categoryList.FirstOrDefault(c => c.Id == category.Id);
            if (checkCategory == null)
            {
                return NotFound();
            }
            category.UpdatedAt = DateTime.UtcNow;
            await _mysqlservice.PutDataAsync<CategoryDto>("Categories", category.Id, category);
            return Ok(category);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var categoryList = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var checkCategory = categoryList.FirstOrDefault(c => c.Id == id);
            if (checkCategory == null)
            {
                return NotFound();
            }
            await _mysqlservice.DeleteDataAsync("Categories", id);
            return Ok("Successfully deleted");
        }
    }
}
