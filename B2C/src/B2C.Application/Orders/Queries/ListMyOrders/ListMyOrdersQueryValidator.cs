using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Orders.Queries.ListMyOrders
{
    public sealed class ListMyOrdersQueryValidator : AbstractValidator<ListMyOrdersQuery>
    {
        public ListMyOrdersQueryValidator()
        {
            RuleFor(x => x.Limit).InclusiveBetween(1, 100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }
}
