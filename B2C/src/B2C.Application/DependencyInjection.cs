using System.Reflection;
using B2C.Application.Cart.Dtos;
using B2C.Application.Cart.Queries.GetMyCart;
using B2C.Application.Cart.Services;
using B2C.Application.Common;
using B2C.Application.Common.Abstractions;
using B2C.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace B2C.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);

                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            });

            // Hot-fix: GetMyCartQueryHandler не подхватывается scan'ом MediatR
            // (вероятно из-за коллизии namespace B2C.Application.Cart с типом
            // B2C.Domain.Carts.Cart). Регистрируем явно — теперь все using есть наверху,
            // компилятор находит и GetMyCartQuery, и CartDto, и сам handler.
            services.AddTransient<IRequestHandler<GetMyCartQuery, CartDto>, GetMyCartQueryHandler>();

            services.AddValidatorsFromAssembly(assembly);

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<ICartContextResolver, CartContextResolver>();

            return services;
        }
    }
}