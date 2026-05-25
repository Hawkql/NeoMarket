using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Images;

namespace B2B.Application.Images.Dtos
{
    public sealed record ImageUploadResultDto(
        Guid Id,
        string Url,
        int Ordering,
        ImageEntityType EntityType,
        Guid? EntityId);
}
