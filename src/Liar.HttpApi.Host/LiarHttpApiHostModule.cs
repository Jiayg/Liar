using Abp.AspNetCore.Mvc.ExceptionHandling;
using Liar.Core.Helper;
using Liar.Core.Microsoft.Extensions.Configuration;
using Liar.Domain.Shared.UserContext;
using Liar.EntityFrameworkCore;
using Liar.HttpApi.Shared.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Liar
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(LiarApplicationModule),
        typeof(LiarConfigModule),
        typeof(LiarAuthModule),
        typeof(LiarSwaggerModule),
        typeof(LiarEntityFrameworkCoreDbMigrationsModule)
    )]
    public class LiarHttpApiHostModule : AbpModule
    {
        private const string DefaultCorsPolicyName = "Default";

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            // ������Ϣ���͵��ͻ���
            Configure<AbpExceptionHandlingOptions>(options =>
            {
                options.SendExceptionsDetailsToClients = true;
            });

            // Ĭ������
            context.Services.AddControllers(options =>
            {
                var filterMetadata = options.Filters.FirstOrDefault(x => x is ServiceFilterAttribute attribute && attribute.ServiceType.Equals(typeof(AbpExceptionFilter)));

                // �Ƴ� AbpExceptionFilter
                //options.Filters.Remove(abpfilter);
                options.Filters.Remove(filterMetadata);
                options.Filters.Add(typeof(CustomExceptionFilterAttribute));
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new DateTimeNullableConverter());
                options.JsonSerializerOptions.Encoder = SystemTextJsonHelper.GetAdncDefaultEncoder();
                //��ֵָʾ�Ƿ����������������ע�͡�
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                //dynamic�������������л�����
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                //dynamic
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                //��������
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }); ;

            // ��������
            context.Services.AddCors(options =>
            {
                var _corsHosts = configuration.GetAllowCorsHosts().Split(",", StringSplitOptions.RemoveEmptyEntries);
                options.AddPolicy(DefaultCorsPolicyName, builder =>
                {
                    builder
                        .WithOrigins(_corsHosts)
                        .WithAbpExposedHeaders()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            }); ;

            // ·������
            context.Services.AddRouting(options =>
            {
                // ����URLΪСд
                options.LowercaseUrls = true;
                // �����ɵ�URL�������б��
                options.AppendTrailingSlash = true;
            });

            context.Services.AddTransient<IUserContext, UserContext>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            var defaultFilesOptions = new DefaultFilesOptions();
            defaultFilesOptions.DefaultFileNames.Clear();
            defaultFilesOptions.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(defaultFilesOptions);

            app.UseStaticFiles();

            app.UseCors(DefaultCorsPolicyName);

            //app.UseSwagger();
            //app.UseAbpSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Liar API");

            //    var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            //    c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            //    c.OAuthClientSecret(configuration["AuthServer:SwaggerClientSecret"]);
            //    c.OAuthScopes("Liar");
            //});

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseConfiguredEndpoints();
            // ·��ӳ��
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
