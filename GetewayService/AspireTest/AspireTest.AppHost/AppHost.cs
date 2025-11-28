var builder = DistributedApplication.CreateBuilder(args);

var authService = builder.AddProject<Projects.AuthenticationApplication>("authapi")
    .WithHttpHealthCheck("/health");

var blogService = builder.AddProject<Projects.Blog_API>("blogapi")
    .WithHttpHealthCheck("/health");

var videProcessingService = builder.AddProject<Projects.VideoProcessing_Cli>("videoProcessing")
    .WithHttpHealthCheck("/health");

var profileService = builder.AddProject<Projects.Profile_API>("profileapi")
    .WithHttpHealthCheck("/health");

var recommendationService = builder.AddProject<Projects.Recommendation_Application>("recommendationapi")
    .WithHttpHealthCheck("/health");

var commentsService = builder.AddProject<Projects.Comments_API>("commentsapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Gateway_API>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(profileService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(recommendationService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(videProcessingService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(commentsService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(blogService, WaitBehavior.WaitOnResourceUnavailable);



builder.Build().Run();
