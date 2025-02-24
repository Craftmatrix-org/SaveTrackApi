using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        private readonly JwtService _jwtService;


        public CategoryController(JwtService jwtService, MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            return Ok(categories);
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
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            await _mysqlservice.PostDataAsync<CategoryDto>("Categories", category);
            return Ok(category);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryDto category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            await _mysqlservice.PutDataAsync<CategoryDto>("Categories", category.Id, category);
            return Ok(category);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCategory([FromBody] CategoryDto category)
        {
            await _mysqlservice.DeleteDataAsync("Categories", category.Id);
            return Ok(category);
        }
    }
}
