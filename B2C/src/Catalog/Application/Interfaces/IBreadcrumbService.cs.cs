using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IBreadcrumbService
    {
        Task<BreadcrumbDto> BuildAsync(
            Guid? categoryId,
            Guid? productId,
            string lang,
            CancellationToken ct = default);
    }
}
