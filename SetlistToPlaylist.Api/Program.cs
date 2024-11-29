using SetlistToPlaylist.Api.RestApiClients;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Services;
using SetlistToPlaylist.Api.Services.Interfaces;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylist.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Add my config
        var config  = builder.Configuration
            .AddJsonFile("ApiSecrets.json")
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        builder.Services.Configure<ApiSecrets>(config.GetRequiredSection("ApiSecrets"));
        builder.Services.Configure<ApiClientSettings>(config.GetRequiredSection("ApiClientSettings"));
        builder.Services.Configure<FrontEndClientSettings>(config.GetRequiredSection("FrontEndClientSettings"));

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMvc()
            .AddSessionStateTempDataProvider();
        builder.Services.AddSession();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp", policy =>
            {
                var frontEndBaseUrl = config.GetValue<string>("FrontEndClientSettings:BaseUrl");
                policy.WithOrigins(frontEndBaseUrl ?? throw new NullReferenceException("Front End Base Url is null."))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        
        // register my services
        builder.Services.AddSingleton<ISetlistFmApiClient, SetlistFmApiClient>();
        builder.Services.AddSingleton<ISpotifyApiClient, SpotifyApiClient>();
        builder.Services.AddSingleton<ISpotifyAuthClient, SpotifyAuthClient>();
        builder.Services.AddSingleton<IPlaylistBuilder, PlayListBuilder>();
        builder.Services.AddSingleton<IAuthTokenFetcher, AuthTokenFetcher>();
        builder.Services.AddHttpClient();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        app.UseCors("AllowReactApp");

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseSession();

        app.MapControllers();

        app.Run();
    }
}