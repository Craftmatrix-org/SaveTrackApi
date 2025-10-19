using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly MySQLService _db;

        public WishlistController(MySQLService db)
        {
            _db = db;
        }

        private async Task<Guid> GetUserIdAsync()
        {
            // Try Subject claim first (this is what the debug token contains)
            var subjectClaim = User.FindFirst("sub")?.Value; // Use literal string instead of constant
            if (!string.IsNullOrEmpty(subjectClaim))
            {
                // If it's an email, look up the user in the database
                if (subjectClaim.Contains("@"))
                {
                    var users = await _db.GetDataAsync<UserDto>("Users");
                    var user = users.FirstOrDefault(u => u.Email == subjectClaim);
                    if (user != null)
                    {
                        return user.Id;
                    }
                }
                // If it's a GUID string, parse it
                else if (Guid.TryParse(subjectClaim, out var subjectGuid))
                {
                    return subjectGuid;
                }
            }

            // Try to get user ID from NameIdentifier claim as fallback
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Try email claim as fallback
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                var users = await _db.GetDataAsync<UserDto>("Users");
                var user = users.FirstOrDefault(u => u.Email == emailClaim);
                if (user != null)
                {
                    return user.Id;
                }
            }

            throw new UnauthorizedAccessException("Unable to determine user identity");
        }

        // WISHLIST PARENT ENDPOINTS

        /// <summary>
        /// Get all wishlist parents for the authenticated user
        /// </summary>
        [HttpGet("parents")]
        public async Task<ActionResult<List<WishListParentDto>>> GetWishlistParents()
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var userParents = parents.Where(p => p.UserID == userId)
                                        .OrderBy(p => p.Name)
                                        .ToList();

                return Ok(userParents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving wishlist parents", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific wishlist parent by ID
        /// </summary>
        [HttpGet("parents/{id}")]
        public async Task<ActionResult<WishListParentDto>> GetWishlistParent(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var parent = parents.FirstOrDefault(p => p.Id == id && p.UserID == userId);

                if (parent == null)
                {
                    return NotFound(new { message = "Wishlist parent not found" });
                }

                return Ok(parent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the wishlist parent", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new wishlist parent
        /// </summary>
        [HttpPost("parents")]
        public async Task<ActionResult<WishListParentDto>> CreateWishlistParent([FromBody] CreateWishlistParentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                // Check for duplicate names
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var existingParent = parents.FirstOrDefault(p => p.UserID == userId && p.Name.ToLower() == request.Name.ToLower());

                if (existingParent != null)
                {
                    return Conflict(new { message = "A wishlist parent with this name already exists" });
                }

                var parent = new WishListParentDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("WishListParents", parent);

                return CreatedAtAction(nameof(GetWishlistParent), new { id = parent.Id }, parent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the wishlist parent", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing wishlist parent
        /// </summary>
        [HttpPut("parents/{id}")]
        public async Task<ActionResult<WishListParentDto>> UpdateWishlistParent(Guid id, [FromBody] UpdateWishlistParentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();
                
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var parent = parents.FirstOrDefault(p => p.Id == id && p.UserID == userId);

                if (parent == null)
                {
                    return NotFound(new { message = "Wishlist parent not found" });
                }

                // Check for duplicate names (excluding current parent)
                var existingParent = parents.FirstOrDefault(p => p.UserID == userId && p.Id != id && p.Name.ToLower() == request.Name.ToLower());

                if (existingParent != null)
                {
                    return Conflict(new { message = "A wishlist parent with this name already exists" });
                }

                parent.Name = request.Name.Trim();
                parent.Description = request.Description?.Trim() ?? "";
                parent.UpdatedAt = DateTime.UtcNow;

                await _db.PutDataAsync("WishListParents", parent.Id, parent);

                return Ok(parent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the wishlist parent", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a wishlist parent and all its associated wishlists
        /// </summary>
        [HttpDelete("parents/{id}")]
        public async Task<ActionResult> DeleteWishlistParent(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var parent = parents.FirstOrDefault(p => p.Id == id && p.UserID == userId);

                if (parent == null)
                {
                    return NotFound(new { message = "Wishlist parent not found" });
                }

                // Delete all associated wishlists first
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var associatedWishlists = wishlists.Where(w => w.ParentId == id && w.UserID == userId).ToList();

                foreach (var wishlist in associatedWishlists)
                {
                    await _db.DeleteDataAsync("WishLists", wishlist.Id);
                }

                await _db.DeleteDataAsync("WishListParents", parent.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the wishlist parent", error = ex.Message });
            }
        }

        // WISHLIST ITEM ENDPOINTS

        /// <summary>
        /// Get all wishlists for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<WishListDto>>> GetWishlists([FromQuery] Guid? parentId = null)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var userWishlists = wishlists.Where(w => w.UserID == userId);

                if (parentId.HasValue)
                {
                    userWishlists = userWishlists.Where(w => w.ParentId == parentId.Value);
                }

                var result = userWishlists
                    .OrderBy(w => w.Label)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving wishlists", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific wishlist by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WishListDto>> GetWishlist(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var wishlist = wishlists.FirstOrDefault(w => w.Id == id && w.UserID == userId);

                if (wishlist == null)
                {
                    return NotFound(new { message = "Wishlist not found" });
                }

                return Ok(wishlist);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the wishlist", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new wishlist item
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WishListDto>> CreateWishlist([FromBody] CreateWishlistRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                // Verify the parent exists and belongs to the user
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var parent = parents.FirstOrDefault(p => p.Id == request.ParentId && p.UserID == userId);

                if (parent == null)
                {
                    return BadRequest(new { message = "Invalid parent ID. The specified wishlist parent does not exist or does not belong to you." });
                }

                var wishlist = new WishListDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    ParentId = request.ParentId,
                    Label = request.Label.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    Url = request.Url?.Trim() ?? "",
                    Price = request.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("WishLists", wishlist);

                return CreatedAtAction(nameof(GetWishlist), new { id = wishlist.Id }, wishlist);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the wishlist", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing wishlist item
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<WishListDto>> UpdateWishlist(Guid id, [FromBody] UpdateWishlistRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();
                
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var wishlist = wishlists.FirstOrDefault(w => w.Id == id && w.UserID == userId);

                if (wishlist == null)
                {
                    return NotFound(new { message = "Wishlist not found" });
                }

                // Verify the parent exists and belongs to the user
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                var parent = parents.FirstOrDefault(p => p.Id == request.ParentId && p.UserID == userId);

                if (parent == null)
                {
                    return BadRequest(new { message = "Invalid parent ID. The specified wishlist parent does not exist or does not belong to you." });
                }

                wishlist.ParentId = request.ParentId;
                wishlist.Label = request.Label.Trim();
                wishlist.Description = request.Description?.Trim() ?? "";
                wishlist.Url = request.Url?.Trim() ?? "";
                wishlist.Price = request.Price;
                wishlist.UpdatedAt = DateTime.UtcNow;

                await _db.PutDataAsync("WishLists", wishlist.Id, wishlist);

                return Ok(wishlist);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the wishlist", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a wishlist item
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWishlist(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var wishlist = wishlists.FirstOrDefault(w => w.Id == id && w.UserID == userId);

                if (wishlist == null)
                {
                    return NotFound(new { message = "Wishlist not found" });
                }

                await _db.DeleteDataAsync("WishLists", wishlist.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the wishlist", error = ex.Message });
            }
        }

        /// <summary>
        /// Get wishlists with their parent information
        /// </summary>
        [HttpGet("with-parents")]
        public async Task<ActionResult<List<WishlistWithParentDto>>> GetWishlistsWithParents()
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                // Fetch both tables
                var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
                var parents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
                
                // Perform the join in memory using LINQ
                var wishlistsWithParents = (from w in wishlists
                                          join p in parents on w.ParentId equals p.Id
                                          where w.UserID == userId
                                          select new WishlistWithParentDto
                                          {
                                              Id = w.Id,
                                              Label = w.Label,
                                              Description = w.Description,
                                              Url = w.Url,
                                              Price = w.Price,
                                              CreatedAt = w.CreatedAt,
                                              UpdatedAt = w.UpdatedAt,
                                              Parent = new WishListParentDto
                                              {
                                                  Id = p.Id,
                                                  Name = p.Name,
                                                  Description = p.Description,
                                                  UserID = p.UserID,
                                                  CreatedAt = p.CreatedAt,
                                                  UpdatedAt = p.UpdatedAt
                                              }
                                          })
                                         .OrderBy(w => w.Parent.Name)
                                         .ThenBy(w => w.Label)
                                         .ToList();

                return Ok(wishlistsWithParents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving wishlists with parents", error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class CreateWishlistParentRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateWishlistParentRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class CreateWishlistRequest
    {
        [Required]
        public Guid ParentId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Label { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Url { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal Price { get; set; }
    }

    public class UpdateWishlistRequest
    {
        [Required]
        public Guid ParentId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Label { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Url { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal Price { get; set; }
    }

    // Response DTOs
    public class WishlistWithParentDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public WishListParentDto Parent { get; set; } = null!;
    }
}