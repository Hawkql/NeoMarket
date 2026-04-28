using B2B.Api.Grpc;
using B2B.Application.common.Behaviors;
using B2B.Application.common.Interface;
using B2B.Application.Common.Behaviors;
using B2B.Application.Common.Interfaces;
using B2B.Application.Products.Commands.CreateProduct;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using B2B.Infrastructure.Messaging;
using B2B.Infrastructure.Persistence;
using B2B.Infrastructure.Persistence.Outbox;
using B2B.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<B2BDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Repositories + UnitOfWork
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// MediatR + behaviors (порядок важен!)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductCommand).Assembly);

// Kafka
builder.Services.AddSingleton<IKafkaProducer>(_ =>
    new KafkaProducer(builder.Configuration["Kafka:BootstrapServers"]!));

// Outbox processor (фоновая задача)
builder.Services.AddHostedService<OutboxProcessor>();

// gRPC + REST
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<ProductQueryGrpcService>();

app.Run();