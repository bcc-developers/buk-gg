using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Buk.Gaming.Providers;
using Buk.Gaming.Web.Providers;
using Microsoft.AspNetCore.ResponseCompression;
using Sanity.Linq;
using Microsoft.Extensions.Options;
using Buk.Gaming.Sanity;
using Buk.Gaming.Sanity.Serializers;
using Buk.Gaming.Images;
using Buk.Gaming.Toornament;
using Buk.Gaming.Repositories;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Buk.Gaming
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Env = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression();

            services.AddRazorPages();

            services.AddMvc().AddNewtonsoftJson();

            // Infrastructure
            services.AddMemoryCache();
            services.AddOptions();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Toornament
            services.Configure<ToornamentOptions>(Configuration.GetSection("Toornament"));
            services.AddScoped(s => s.GetService<IOptionsSnapshot<ToornamentOptions>>().Value);
            services.AddScoped(s => new ToornamentClient(s.GetRequiredService<ToornamentOptions>(), ToornamentScopes.Organizer.All));

            // Content
            services.Configure<SanityOptions>(Configuration.GetSection("Sanity"));
            services.AddScoped(s => s.GetService<IOptionsSnapshot<SanityOptions>>().Value);
            services.AddScoped(s =>
            {
                var options = s.GetRequiredService<SanityOptions>();
                var sanity = new SanityDataContext(options);
                sanity.UseSanityBlockSerializer(options, s.GetRequiredService<IImageService>());
                return sanity;
            });
            services.AddScoped<IPlayerRepository, SanityPlayerRepository>();
            services.AddScoped<ITournamentRepository, SanityTournamentRepository>();
            services.AddScoped<IEventRepository, SanityEventRepository>();
            services.AddScoped<IOrganizationRepository, SanityOrganizationRepository>();
            services.AddScoped<ITeamRepository, SanityTeamRepository>();
            services.AddScoped<PlayerService>();
            services.AddHttpClient();

            if (Env.EnvironmentName == "Development")
            {
                services.AddScoped<ILocalizationService, SanityLocalizationService>();
            }
            else
            {
                services.AddScoped<ILocalizationService, CachedSanityLocalizationService>();
            }

            // Images
            services.AddTransient<IImageService, ImageService>();
            services.Configure<ImageOptions>(Configuration.GetSection("Images"));
            services.AddTransient(s => s.GetService<IOptionsSnapshot<ImageOptions>>().Value);
            
            // Mapping



            // Session
            services.AddScoped<ISessionProvider, SessionProvider>();
            services.AddScoped<IDiscordProvider, DiscordProvider>();

            ConfigureAuthentication(services);
        }

        public virtual void ConfigureAuthentication(IServiceCollection services)
        {
            // Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = Configuration["Auth0:Authority"];
                options.Audience = Configuration["Auth0:Audience"];
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Add the access_token as a claim, as we may actually need it
                        var accessToken = context.SecurityToken as JwtSecurityToken;
                        if (accessToken != null)
                        {
                            ClaimsIdentity identity = context.Principal.Identity as ClaimsIdentity;
                            if (identity != null)
                            {
                                identity.AddClaim(new Claim("access_token", accessToken.RawData));
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration config)
        {
            bool isLocal = config.GetValue<bool>("Environment:IsLocal");
            if (env.EnvironmentName == "Development")
            {
                // if (isLocal)
                // {
                //     var options = new WebpackDevMiddlewareOptions() { 
                //         HotModuleReplacement = true,
                //         ConfigFile = "node_modules/@vue/cli-service/webpack.config.js",
                //         EnvironmentVariables = new Dictionary<string, string> { { "IsAspNetServer", "True" } }
                //      };
                //     app.UseWebpackDevMiddleware(options);
                // }
                app.UseDeveloperExceptionPage();
            }
            else
            {
                if (!isLocal) 
                {
                    app.UseHttpsRedirection();
                    //app.UseHsts();
                }
            }

            var supportedCultures = new[]
            {
                new CultureInfo("no"),
                new CultureInfo("en"),
                new CultureInfo("en-US"),
                new CultureInfo("nb-NO"),
                new CultureInfo("en-GB"),
                new CultureInfo("en-AU"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("nb-NO"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures,
                RequestCultureProviders = new List<IRequestCultureProvider> {
                    new AcceptLanguageHeaderRequestCultureProvider()
                }
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapFallbackToController("Index", "Home");
            });
        }
    }
}
