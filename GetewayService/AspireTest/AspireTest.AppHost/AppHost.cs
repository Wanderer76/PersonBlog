using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var authService = builder.AddProject<Projects.AuthenticationApplication>("authapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AuthGateway_API>("authgateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable);

var videProcessingService = builder.AddProject<Projects.VideoProcessing_Cli>("videoProcessing")
    .WithHttpHealthCheck("/health");

var blogService = builder.AddProject<Projects.Blog_API>("blogapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__VideoProcessing", ReferenceExpression.Create($"{videProcessingService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WithReference(videProcessingService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(videProcessingService, WaitBehavior.WaitOnResourceUnavailable);

var profileService = builder.AddProject<Projects.Profile_API>("profileapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Blog", ReferenceExpression.Create($"{blogService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WithReference(blogService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(blogService, WaitBehavior.WaitOnResourceUnavailable);

var recommendationService = builder.AddProject<Projects.Recommendation_Application>("recommendationapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable);

var commentsService = builder.AddProject<Projects.Comments_API>("commentsapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Reacting", ReferenceExpression.Create($"{profileService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WithReference(profileService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(profileService, WaitBehavior.WaitOnResourceUnavailable);

var searchService = builder.AddProject<Projects.SearchService_Application>("searchapi")
    .WithHttpHealthCheck("/health");

var conferenceService = builder.AddProject<Projects.Conference_API>("conferenceapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable);

var playListService = builder.AddProject<Projects.PlayListService_API>("playlistapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Blog", ReferenceExpression.Create($"{blogService.GetEndpoint("http")}/api/"))
    .WithReference(authService)
    .WithReference(blogService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(blogService, WaitBehavior.WaitOnResourceUnavailable);

var notificationService = builder.AddProject<Projects.Notification_API>("notificationapi")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__NotificationBlog", ReferenceExpression.Create($"{blogService.GetEndpoint("http")}/"))
    .WithReference(authService)
    .WithReference(blogService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(blogService, WaitBehavior.WaitOnResourceUnavailable);

builder.AddProject<Projects.Gateway_API>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AppUrls__Auth", ReferenceExpression.Create($"{authService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Profile", ReferenceExpression.Create($"{blogService.GetEndpoint("http")}/"))
    .WithEnvironment("AppUrls__Blog", ReferenceExpression.Create($"{blogService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Recommendation", ReferenceExpression.Create($"{recommendationService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Reacting", ReferenceExpression.Create($"{profileService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Search", ReferenceExpression.Create($"{searchService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Conference", ReferenceExpression.Create($"{conferenceService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Comments", ReferenceExpression.Create($"{commentsService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__PlayList", ReferenceExpression.Create($"{playListService.GetEndpoint("http")}/api/"))
    .WithEnvironment("AppUrls__Notification", ReferenceExpression.Create($"{notificationService.GetEndpoint("http")}/"))
    .WithReference(authService)
    .WithReference(blogService)
    .WithReference(profileService)
    .WithReference(recommendationService)
    .WithReference(searchService)
    .WithReference(conferenceService)
    .WithReference(commentsService)
    .WithReference(playListService)
    .WithReference(notificationService)
    .WaitFor(authService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(profileService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(recommendationService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(commentsService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(searchService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(conferenceService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(playListService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(notificationService, WaitBehavior.WaitOnResourceUnavailable)
    .WaitFor(blogService, WaitBehavior.WaitOnResourceUnavailable);

builder.Build().Run();
