using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ManufacturingSimulation.Database;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingSimulation.WPF.Services
{
    /// <summary>
    /// Service for database administration operations
    /// </summary>
    public interface IAdminService
    {
        Task<List<object>> GetAllAsync(Type entityType, int? studentId = null);
        Task<object> GetByIdAsync(Type entityType, object id);
        Task AddAsync(object entity);
        Task UpdateAsync(object entity);
        Task DeleteAsync(object entity);
        Task<int> SaveChangesAsync();
        Task ExportToCsvAsync(IEnumerable<object> data, string filePath);
        Task<List<T>> GetLookupValuesAsync<T>(string displayProperty) where T : class;
    }

    public class AdminService : IAdminService
    {
        private readonly MesDbContext _context;

        public AdminService(MesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<object>> GetAllAsync(Type entityType, int? studentId = null)
        {
            var dbSet = GetDbSet(entityType);
            var query = (IQueryable<object>)dbSet;

            // Filter by student_id if applicable
            if (studentId.HasValue)
            {
                var studentIdProp = entityType.GetProperty("StudentId") 
                    ?? entityType.GetProperty("student_id");
                
                if (studentIdProp != null)
                {
                    query = query.Where(e => EF.Property<int?>(e, studentIdProp.Name) == studentId.Value);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<object> GetByIdAsync(Type entityType, object id)
        {
            var dbSet = GetDbSet(entityType);
            var findMethod = dbSet.GetType().GetMethod("FindAsync", new[] { typeof(object[]) });
            
            if (findMethod == null)
                throw new InvalidOperationException($"Cannot find FindAsync method for {entityType.Name}");

            var task = (Task)findMethod.Invoke(dbSet, new object[] { new[] { id } });
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        public async Task AddAsync(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _context.Add(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _context.Update(entity);
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            _context.Remove(entity);
            await SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task ExportToCsvAsync(IEnumerable<object> data, string filePath)
        {
            if (data == null || !data.Any())
                throw new ArgumentException("No data to export", nameof(data));

            var firstItem = data.First();
            var type = firstItem.GetType();
            var properties = type.GetProperties()
                .Where(p => p.CanRead && !p.PropertyType.IsClass || p.PropertyType == typeof(string))
                .ToList();

            var csv = new StringBuilder();

            // Header
            csv.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

            // Data rows
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return EscapeCsv(value?.ToString() ?? "");
                });
                csv.AppendLine(string.Join(",", values));
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        public async Task<List<T>> GetLookupValuesAsync<T>(string displayProperty) where T : class
        {
            return await _context.Set<T>().ToListAsync();
        }

        private object GetDbSet(Type entityType)
        {
            var setMethod = typeof(DbContext)
                .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
                ?.MakeGenericMethod(entityType);

            if (setMethod == null)
                throw new InvalidOperationException($"Cannot get DbSet for type {entityType.Name}");

            return setMethod.Invoke(_context, null);
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            
            return value;
        }
    }
}
