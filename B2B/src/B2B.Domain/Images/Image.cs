using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Images.Events;

namespace B2B.Domain.Images
{
    public class Image:AggregateRoot<Guid>,IAuditableEntity
    {
        public Guid EntityId { get; set; }

        /// <summary>Порядок отображения в галерее. 0 = главное фото.</summary>
        public int Ordering {  get; set; }


        /// <summary>URL изображения в file storage. Неизменяемый после создания.</summary>
        public string Url { get; private set; } = null!;
        public ImageEntityType EntityType { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        private Image(Guid id,ImageEntityType entityType,Guid entityId,string url,int ordering):base(id)
        {
            EntityType = entityType;
            EntityId = entityId;
            Url = url;
            Ordering = ordering;
        }
        public Image() { }


        public  static Image Create(ImageEntityType entityType,Guid entityId,string url,int ordering)
        {
            if (entityId == Guid.Empty)
                throw new DomainException("EntityId is required", "INVALID_REQUEST");
            if(string.IsNullOrWhiteSpace(url))
                throw new DomainException("Url is required", "INVALID_REQUEST");
            if (url.Length > 2000)
                throw new DomainException("Url is too long", "INVALID_REQUEST");
            if (ordering < 0)
                throw new DomainException("Ordering must be >= 0", "INVALID_REQUEST");

            var image = new Image(Guid.NewGuid(),entityType, entityId, url, ordering);
            image.RaiseDomainEvent(new ImageCreatedEvent(image.Id, entityType, entityId, url));
            return image ;
        }
       
        /// Помечаем намерение удалить (через RaiseDomainEvent).
        public void MarkAsDeleted()
        {
            RaiseDomainEvent(new ImageDeletedEvent(Id, EntityType, EntityId, Url));
        }
    }
}
