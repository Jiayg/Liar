using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using Abp.AspNetCore.Mvc.ExceptionHandling;
using Liar.Core.Helper;
using Liar.Core.Microsoft.Extensions.Configuration;
using Liar.Domain.Shared.UserContext;
using Liar.EntityFrameworkCore;
using Liar.HttpApi.Host.Authorize;
using Liar.HttpApi.Shared.Extensions;
using Liar.HttpApi.Shared.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            context.Services.AddHttpContextAccessor();
            context.Services.AddMemoryCache();

            // Ĭ������
            context.Services.AddControllers(options => options.Filters.Add(typeof(CustomExceptionFilterAttribute)))
                            .AddJsonOptions(options =>
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
                            });
            context.Services.Configure<ApiBehaviorOptions>(options =>
            {
                //�ر��Զ���֤
                //options.SuppressModelStateInvalidFilter = true;
                //��ʽ����֤��Ϣ
                options.InvalidModelStateResponseFactory = (context) =>
                {
                    var problemDetails = new ProblemDetails
                    {
                        Detail = context.ModelState.GetValidationSummary("<br>"),
                        Title = "��������",
                        Status = (int)HttpStatusCode.BadRequest,
                        Type = "https://httpstatuses.com/400",
                        Instance = context.HttpContext.Request.Path
                    };

                    return new ObjectResult(problemDetails)
                    {
                        StatusCode = problemDetails.Status
                    };
                };
            });

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
            });

            context.Services.AddAuthenticationSetup(configuration);
            context.Services.AddAuthorization<PermissionHandlerLocal>();

            // ·������
            context.Services.AddRouting(options =>
            {
                // ����URLΪСд
                options.LowercaseUrls = true;
                // �����ɵ�URL�������б��
                options.AppendTrailingSlash = true;
            });

            context.Services.AddSingleton<IUserContext, UserContext>();
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

            app.UseCustomExceptionHandler();

            app.UseRealIp(x =>
            {
                x.HeaderKeys = new string[] { "X-Forwarded-For", "X-Real-IP" };
            });

            app.UseCors(DefaultCorsPolicyName);

            app.UseUnitOfWork();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // ·��ӳ��
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
