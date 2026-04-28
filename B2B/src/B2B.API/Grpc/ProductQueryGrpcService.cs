using Google.Protobuf.Collections;
using Grpc.Core;
using MediatR;

namespace B2B.Api.Grpc
{
    public class ProductQueryGrpcService : ProductQuery.ProductQueryBase
    {
        private readonly IMediator _mediator;
        public ProductQueryGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ProductResponse> GetProduct(
            GetProductRequest request, ServerCallContext context)

        {
            if (!Guid.TryParse(request.ProductId, out var id))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product id"));

            var product = await _mediator.Send(new GetProductByIdQuery(id), context.CancellationToken);
            if (product == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Product not found"));

            var response = new ProductResponse
            {
                Id = product.Id.ToString(),
                Title = product.Title,
                Description = product.Description,
                Status = (int)product.Status
            };
            foreach (var sku in product.Skus)
            {
                response.Skus.Add(new SkuMessage
                {
                    Id = sku.Id.ToString(),
                    Name = sku.Name,
                    Price = (double)sku.Price,
                    Quantity = sku.Quantity
                });
            }
            return response;
        }

        public override async Task<ProductsBatchResponse> GetProductsBatch(
            GetProductsBatchRequest request, ServerCallContext context)
        {
            // ... аналогично, через batch query
            throw new NotImplementedException();
        }
    }
}
}
