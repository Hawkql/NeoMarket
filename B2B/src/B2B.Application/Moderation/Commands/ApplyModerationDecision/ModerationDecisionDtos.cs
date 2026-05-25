using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed record BlockingReasonInputDto(Guid Id, string Title, string Comment);

    public sealed record FieldReportInputDto(
        string FieldName,
        Guid? SkuId,
        string Comment);
}
