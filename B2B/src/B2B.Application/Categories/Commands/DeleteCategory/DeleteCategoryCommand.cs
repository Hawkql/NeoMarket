using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Categories.Commands.DeleteCategory
{
    public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest;
}
