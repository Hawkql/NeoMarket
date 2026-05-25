using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Images.Dtos;
using B2B.Domain.Images;
using MediatR;

namespace B2B.Application.Images.Commands.UploadImage
{

    public sealed record UploadImageCommand(
        Guid SellerId,
        Stream FileStream,
        string DeclaredContentType,
        ImageEntityType EntityType,
        Guid? EntityId,
        int Ordering
    ) : IRequest<ImageUploadResultDto>;
}
