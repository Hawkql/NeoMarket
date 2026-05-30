using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Favorites.Commands.RemoveFavorite
{
    public sealed record RemoveFavoriteCommand(Guid ProductId) : IRequest;
}
