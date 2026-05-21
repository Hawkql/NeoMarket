using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Images;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class ImageRepository : IImageRepository
    {
        private readonly B2BDbContext _dbContext;

        public ImageRepository(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Image?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            // Tracking нужен для Update/Delete сценариев
            return await _dbContext.Images
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<IReadOnlyCollection<Image>> GetByEntityAsync(
            ImageEntityType entityType,
            Guid entityId,
            CancellationToken ct)
        {
            // Использует ix_images_owner (entity_type, entity_id, ordering).
            // OrderBy идёт по индексу — без отдельного sort step.
            return await _dbContext.Images
                .AsNoTracking()
                .Where(i => i.EntityType == entityType && i.EntityId == entityId)
                .OrderBy(i => i.Ordering)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Image>>> GetByEntitiesAsync(
            ImageEntityType entityType,
            IEnumerable<Guid> entityIds,
            CancellationToken ct)
        {
            var idArray = entityIds.ToArray();

            // Один запрос для всех entities — никаких N+1
            var images = await _dbContext.Images
                .AsNoTracking()
                .Where(i => i.EntityType == entityType && idArray.Contains(i.EntityId))
                .OrderBy(i => i.Ordering)
                .ToListAsync(ct);

            // Группируем в Dictionary — Application слой получает готовое разбиение.
            // Сохраняем порядок (OrderBy уже отсортировал).
            return images
                .GroupBy(i => i.EntityId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyCollection<Image>)g.ToList());
        }

        public async Task AddAsync(Image image, CancellationToken ct)
        {
            await _dbContext.Images.AddAsync(image, ct);
        }

        public void Remove(Image image)
        {
            _dbContext.Images.Remove(image);
        }
    }
}
