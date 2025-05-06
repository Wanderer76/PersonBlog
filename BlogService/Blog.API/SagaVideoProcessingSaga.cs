using Blog.API.Models;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

public class VideoProcessingSaga : MassTransitStateMachine<VideoProcessingSagaState>
{
    public State WaitingForConversion { get; private set; }
    public State WaitingForPublishConfirm { get; private set; }
    public State Completed { get; private set; }

    public Event<CombineFileChunksCommand> CombineChunks { get; private set; }
    public Event<ChunksCombinedResponse> ChunksCombined { get; private set; }
    public Event<VideoConvertedResponse> VideoConverted { get; private set; }

    private readonly IServiceProvider _serviceProvider;

    public VideoProcessingSaga(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        InstanceState(x => x.CurrentState);

        Event(() => CombineChunks, x => x.CorrelateById(m => m.Message.VideoMetadataId));
        Event(() => ChunksCombined, x => x.CorrelateById(m => m.Message.VideoMetadataId));
        Event(() => VideoConverted, x => x.CorrelateById(m => m.Message.VideoMetadataId));
       
        Initially(
    When(CombineChunks)
        .Then(ctx =>
        {
            ctx.Saga.VideoMetadataId = ctx.Message.VideoMetadataId;
            ctx.Saga.PostId = ctx.Message.PostId;
        })
        .TransitionTo(WaitingForConversion));

        During(WaitingForConversion,
            When(ChunksCombined)
                .ThenAsync(async ctx =>
                {
                    ctx.Saga.ObjectName = ctx.Message.ObjectName;

                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();

                    await repository.Get<VideoMetadata>()
                    .Where(x => x.Id == ctx.Message.VideoMetadataId)
                    .ExecuteUpdateAsync(video => video.SetProperty(x => x.ObjectName, ctx.Message.ObjectName));

                    await ctx.Publish(new ConvertVideoCommand
                    {
                        VideoMetadataId = ctx.Saga.VideoMetadataId,
                        ObjectName = ctx.Saga.ObjectName!,
                        PostId = ctx.Saga.PostId,
                    });
                })
                .TransitionTo(WaitingForPublishConfirm)
        );

        During(WaitingForPublishConfirm,
            When(VideoConverted)
                .ThenAsync(async ctx =>
                {
                    ctx.Saga.ObjectName = ctx.Message.ObjectName;
                    await ctx.Publish(new VideoReadyToPublishEvent
                    {
                        PostId = ctx.Message.PostId,
                        VideoMetadataId = ctx.Message.VideoMetadataId,
                        Duration = ctx.Message.Duration,
                        ObjectName = ctx.Message.ObjectName!,
                        PreviewId = ctx.Message.PreviewId,
                        ProcessState = ctx.Message.ProcessState,
                        CreatedAt = DateTimeService.Now()
                    }, publish =>
                    {
                        publish.SetRoutingKey("video.publish");
                    });
                })
                .TransitionTo(Completed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}

