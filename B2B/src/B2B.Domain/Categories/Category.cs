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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Category() { }
        private Category(Guid id,Guid? parentId,string name,int ordering):base(id) 
        {
            ParentId = parentId;
            Name = name;
            Ordering = ordering;
            Deleted = false;
             
        }

        public static Category Create(Guid? parentId, string name, int ordering)
        {
            ValidName(name);
            if (ordering < 0)
                throw new DomainException("Ordering must be >= 0", "INVALID_REQUEST");
            var category = new Category(Guid.NewGuid(),parentId,name,ordering);
            category.RaiseDomainEvent(new CategoryCreatedEvent(category.Id, category.ParentId, category.Name));
            return category;
        }
        public void Rename(string newName)
        {
            EnsureNotDeleted();
            ValidName(newName);
            if (Name == newName)
                return;
            var oldname = Name;
            Name = newName;
            RaiseDomainEvent(new CategoryRenamedEvent(Id, oldname, Name));
        }
        ///<summary>
        ///Перемещение категории в дереве.
        /// </summary>
        public void MoveTo(Guid? NewParentId)
        {
            EnsureNotDeleted();

            if(NewParentId == Id)
                throw new DomainException(
                "Category cannot be its own parent",
                "INVALID_REQUEST");

            if(ParentId== NewParentId)
                return;

            var oldparentId = ParentId;
            ParentId = NewParentId;
            RaiseDomainEvent(new CategoryMovedEvent(Id, oldparentId, NewParentId));
        }

        public void Reorder(int newOrdering)
        {
            EnsureNotDeleted();
            if(newOrdering<0)
                throw new DomainException("Ordering must be >= 0", "INVALID_REQUEST");
            Ordering = newOrdering;
            // Нет специального события — Ui
        }
        /// <summary>
        /// Soft delete. Проверка "нет привязанных товаров" — ответственность
        /// Application Handler (требует Query к Products).
        /// </summary>
        public void MarkAsDeleted()
        {
            if(Deleted)
                throw new DomainException("Category already deleted", "INVALID_REQUEST");
            Deleted = true;
            RaiseDomainEvent(new CategoryDeletedEvent(Id));
        }
        public void Restore()
        {
            if (!Deleted)
                throw new DomainException("Category is not deleted", "INVALID_REQUEST");

            Deleted = false;
            // Нет специального события — это admin-операция
        }
        private void EnsureNotDeleted()
        {
            if(Deleted)
                throw new DomainException(
                    "Cannot modify deleted category", "FORBIDDEN");
        }
        private static void ValidName(string name)
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
