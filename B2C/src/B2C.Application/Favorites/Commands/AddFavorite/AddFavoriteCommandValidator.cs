using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Favorites.Commands.AddFavorite
{
    public sealed class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
    {
        public AddFavoriteCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
        }
    }
}
