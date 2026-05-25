using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace B2B.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // MediatR — регистрирует все Command/Query Handlers из сборки
            services.AddMediatR(cfg =>
            {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            // ValidationBehavior встраивается в pipeline ПЕРЕД handlers
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // FluentValidation — регистрирует все валидаторы из сборки

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
