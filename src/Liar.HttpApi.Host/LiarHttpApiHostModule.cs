using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Liar.Domain.Shared;
using Liar.Domain.Shared.UserContext;
using Liar.EntityFrameworkCore;
using Liar.HttpApi.Host.Authorize;
using Liar.HttpApi.Host.Filter;
using Liar.HttpApi.Host.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.ExceptionHandling;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace Liar
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(LiarApplicationModule),
        typeof(LiarEntityFrameworkCoreDbMigrationsModule),
        typeof(AbpSwashbuckleModule)
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

            Configure<MvcOptions>(options =>
            {
                var filterMetadata = options.Filters.FirstOrDefault(x => x is ServiceFilterAttribute attribute && attribute.ServiceType.Equals(typeof(AbpExceptionFilter)));

                // �Ƴ� AbpExceptionFilter
                options.Filters.Remove(filterMetadata);

                // ����Լ�ʵ�ֵ� LiarExceptionFilter
                options.Filters.Add(typeof(LiarExceptionFilter));
            });

            // ��������
            context.Services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicyName, builder =>
                {
                    builder
                        .WithOrigins(
                            configuration["App:CorsOrigins"]
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.RemovePostFix("/"))
                                .ToArray()
                        )
                        .WithAbpExposedHeaders()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // ·������
            context.Services.AddRouting(options =>
            {
                // ����URLΪСд
                options.LowercaseUrls = true;
                // �����ɵ�URL�������б��
                options.AppendTrailingSlash = true;
            });

            // �ӿڿ��ӻ�
            context.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Learn API", Version = "v1" });
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Liar.HttpApi.Host.xml"));
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Liar.Application.Contracts.xml"));
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                // ������ȨС��
                options.OperationFilter<AddResponseHeadersFilter>();
                options.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                // ��header�����token�����ݵ���̨
                options.OperationFilter<SecurityRequirementsOperationFilter>();

                // Jwt Bearer ��֤�������� oauth2
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "JWT��Ȩ(���ݽ�������ͷ�н��д���) ֱ�����¿�������Bearer {token}��ע������֮����һ���ո�\"",
                    Name = "Authorization",//jwtĬ�ϵĲ�������
                    In = ParameterLocation.Header,//jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
                    Type = SecuritySchemeType.ApiKey
                });
            });

            var Issuer = AppSettings.JwtAuth.Issuer;
            var Audience = AppSettings.JwtAuth.Audience;

            context.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    //��֤��һЩ���ã������Ƿ���֤�����ߣ������ߣ���Կ���Լ�����ʱ��ȵ�
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AppSettings.JwtAuth.SecurityKey)),
                        ValidateAudience = true,
                        ValidAudience = Audience,//������
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(30)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        //���ܵ���Ϣʱ����
                        OnMessageReceived = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            context.Response.Headers.Add("Token-Error", context.ErrorDescription);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            //����ǹ��ڣ���http heard�м���act����
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Add("act", "expired");
                            }
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var userContext = context.HttpContext.RequestServices.GetService<IUserContext>();
                            var claims = context.Principal.Claims;
                            userContext.Id = long.Parse(claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
                            userContext.Account = claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
                            userContext.Name = claims.First(x => x.Type == ClaimTypes.Name).Value;
                            //userContext.Email = claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                            //string[] roleIds = claims.First(x => x.Type == ClaimTypes.Role).Value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                            userContext.RemoteIpAddress = context.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                            return Task.CompletedTask;
                        }
                    };

                    //��Ϊ��ȡ�����ķ�ʽĬ������΢�����һ��ӳ�䷽ʽ�����������Ҫ��JWTӳ����������ô������Ҫ��Ĭ��ӳ�䷽ʽ���Ƴ���
                    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
                });

            // ��֤��Ȩ
            context.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Permission.Policy, policy => policy.Requirements.Add(new HttpApi.Host.Authorize.PermissionRequirement()));
            });

            //context.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
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

            app.UseSwagger();
            app.UseAbpSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Liar API");

                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                c.OAuthClientSecret(configuration["AuthServer:SwaggerClientSecret"]);
                c.OAuthScopes("Liar");
            });

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseRouting();

            app.UseAuthentication();

            app.UseUnitOfWork();
            app.UseAuthorization();

            app.UseAuditing();

            app.UseConfiguredEndpoints();
        }
    }
}
