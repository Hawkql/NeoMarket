using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Domain.Categories.Events;
using B2B.Domain.Common;

namespace B2B.Domain.Categories
{
    public class Category : AggregateRoot<Guid>, IAuditableEntity
    {
        /// <summary>
        /// ID родительской категории. null = корневая.
        /// Reference by Identity — навигационного свойства нет.
        /// </summary>
        public Guid? ParentId { get;private set; }

        public string Name { get; private set; } = null!;

        /// <summary>Порядок сортировки внутри одного уровня. 0 = первая.</summary>
        public int Ordering {  get; private set; }
        public bool Deleted { get; private set; }
        DateTime IAuditableEntity.CreateAt { get ; set ; }
        DateTime IAuditableEntity.UpdateAt { get ; set ; }

        public DateTime CreateAt;
        public DateTime UpdateAt;

        public Category() { }
        private Category(Guid id,Guid? parentId,string name,int ordering):base(id) 
        {
            ParentId = parentId;
            Name = name;
            Ordering = ordering;
            Deleted = false;
             
        }

        public Category Create(Guid? parentId, string name, int ordering)
        {
            ValidName(name);
            if (Ordering < 0)
                throw new DomainException("Ordering must be >= 0", "INVALID_REQUEST");
            var category = new Category(Guid.NewGuid(),parentId,name,ordering);
            category.RaiseDomainEvent(new CategoryCreatedEvent(category.Id, category.ParentId, category.Name));
            return category;
        }
        private void ValidName(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
                throw new DomainException("Category name is required", "INVALID_REQUEST");
            if (name.Length > 200)
                throw new DomainException(
                    "Category name must be 1-200 characters",
                    "INVALID_REQUEST");
        }

    }
}
