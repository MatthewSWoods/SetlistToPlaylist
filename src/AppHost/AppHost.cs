var builder = DistributedApplication.CreateBuilder(args);

var redisCache = builder.AddRedis("cache", port: 6379);

var apiService = builder.AddProject<Projects.SetlistToPlaylist_ApiService>("apiservice")
    .WithReference(redisCache)
    .WaitFor(redisCache)
    .WithHttpsEndpoint(port: 5001, name: "apiservice-https")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SetlistToPlaylist_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(redisCache)
    .WaitFor(redisCache)
    .WithReference(apiService)
    .WaitFor(apiService);


builder.Build().Run();
