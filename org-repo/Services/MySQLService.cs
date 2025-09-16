using Craftmatrix.org.Data;
using Microsoft.EntityFrameworkCore;

namespace Craftmatrix.org.Services
{
    public class MySQLService
    {
        private readonly AppDbContext _context;

        public MySQLService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TDto> PostDataAsync<TDto>(string tableName, TDto dto) where TDto : class
        {
            // Find the DbSet for the specified table name dynamically
            var dbSetProperty = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) && p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            if (dbSetProperty == null)
                throw new ArgumentException($"Table '{tableName}' not found in the context.");

            // Get the DbSet for the specific entity type
            var dbSet = dbSetProperty.GetValue(_context);

            // Use reflection to create the corresponding entity type
            var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
            var entity = Activator.CreateInstance(entityType);

            // Map DTO to entity
            _context.Entry(entity).CurrentValues.SetValues(dto);

            // Add the entity to the DbSet and save
            dbSet.GetType().GetMethod("Add")?.Invoke(dbSet, new[] { entity });
            await _context.SaveChangesAsync();

            // Set the ID (assuming it's auto-generated) in the DTO and return
            var idProperty = entityType.GetProperty("ID");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(entity);
                idProperty.SetValue(dto, idValue);
            }

            return dto;
        }

        public async Task<IEnumerable<TDto>> GetDataAsync<TDto>(string tableName) where TDto : class
        {
            var dbSetProperty = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) && p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            if (dbSetProperty == null)
                throw new ArgumentException($"Table '{tableName}' not found in the context.");

            var dbSet = dbSetProperty.GetValue(_context);

            // Cast the dbSet to IQueryable<TDto> and return all data
            return await ((IQueryable<TDto>)dbSet).ToListAsync();
        }

        public async Task<TDto> PutDataAsync<TDto>(string tableName, dynamic id, TDto dto) where TDto : class
        {
            var dbSetProperty = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) && p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            if (dbSetProperty == null)
                throw new ArgumentException($"Table '{tableName}' not found in the context.");

            var dbSet = dbSetProperty.GetValue(_context);

            // Find the entity by ID directly using the FindAsync method on the DbSet
            var entity = await (dbSet as dynamic).FindAsync(id);  // Correctly call FindAsync for the DbSet

            if (entity == null)
                throw new ArgumentException($"Record with ID {id} not found in the '{tableName}' table.");

            // Update the entity with the new values from the DTO
            _context.Entry(entity).CurrentValues.SetValues(dto);

            try
            {
                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"Error updating record in table '{tableName}' with ID '{id}': {ex.Message}");
                throw;
            }

            return dto;
        }

        public async Task<bool> DeleteDataAsync(string tableName, object id)
        {
            var dbSetProperty = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) && p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            if (dbSetProperty == null)
                throw new ArgumentException($"Table '{tableName}' not found in the context.");

            var dbSet = dbSetProperty.GetValue(_context);

            // Find the entity by ID directly using the FindAsync method on the DbSet
            var entity = await (dbSet as dynamic).FindAsync(id);  // Correctly call FindAsync for the DbSet

            if (entity == null)
                return false;  // Return false if the record is not found.

            // Remove the entity directly without reflection
            (dbSet as dynamic).Remove(entity);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return true;  // Return true if the record was deleted.
        }
    }
}
