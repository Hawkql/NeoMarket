using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ISkuService, SkuService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICategoryFilterService, CategoryFilterService>();
            services.AddScoped<IBreadcrumbService, BreadcrumbService>();

            return services;
        }
    }
}
