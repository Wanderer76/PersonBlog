using ChatService.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Contract.HttpClients;
using Profile.Domain.Entities;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace ChatService.Application.Services;

public class ChatService(
    ICurrentUserService currentUserService,
    IReadWriteRepository<IChatEntity> repository,
    ProfileHttpClient profileHttpClient,
    IGuidManager guidManager,
    IDateTimeManager dateTimeManager)
{
    private const int DefaultPageSize = 20;

    public async Task<Result<CreateChatResponse>> CreateChat(CreateUserChatRequest createChatRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createChatRequest.Title))
        {
            return new Error("title", "");
        }

        var creatorResponse = await profileHttpClient.GetMyProfileAsync(cancellationToken);

        if (creatorResponse.IsFailure)
            return Result<CreateChatResponse>.Failure(creatorResponse.Errors);

        var creator = creatorResponse.Value;

        var chatId = guidManager.GetNewGuid();

        var profile = await profileHttpClient.GetProfileByUserIdAsync(createChatRequest.Member.UserId);

        if(profile == null)
        {
            return new Error("member", "");

        }
        var members = new[] { (creator.UserId, creator.Name), (profile.UserId, profile.Name) };

        var chatMembers = members
            .Select(x => new ChatMemberLink(
                chatId,
                x.UserId,
                x.UserId == creator.UserId,
                x.Name))
            .ToList();

        var chat = ChatEntity.Create(
            chatId,
            createChatRequest.Title,
            dateTimeManager.UtcNow(),
            chatMembers
            );

        if (chat.IsSuccess)
        {
            repository.Add(chat.Value);
            await repository.SaveChangesAsync();
            return Result<CreateChatResponse>.Success(new CreateChatResponse(chat.Value.Id));
        }
        else
        {
            return Result<CreateChatResponse>.Failure(chat.Errors);
        }
    }

    public async Task<Result<ChatPageViewModel<ChatListItem>>> GetUserChatListPaged(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var totalCount = await repository.Get<ChatMemberLink>()
            .Where(x => x.ChatMemberId == user.UserId)
            .CountAsync(cancellationToken);

        var chatList = await repository.Get<ChatMemberLink>()
            .Where(x => x.ChatMemberId == user.UserId)
            .Select(x => new
            {
                x.ChatId,
                x.ChatEntity.Title,
                LastMessage = x.ChatEntity.Messages
                .OrderByDescending(x => x.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.CreatedAt,
                    m.Text,
                    HasFiles = m.MessageFiles.Any(),
                    IsReadMessage = m.MessageReader.FirstOrDefault(reader => reader.UserId == user.UserId) != null
                })
                .First(),
            })
            .OrderByDescending(x => x.LastMessage.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = chatList
            .Select(x => new
            ChatListItem(
                x.ChatId,
                x.Title,
                new ChatListItemMessage(x.LastMessage.Id, x.LastMessage.Text ?? "Вложение", x.LastMessage.CreatedAt, x.LastMessage.IsReadMessage)
                )
            );

        return new ChatPageViewModel<ChatListItem>(totalCount, pageSize, page, items.ToList());
    }

    public async Task<Result<ChatItemViewModel>> GetChatItem(Guid id, CancellationToken cancellationToken = default)
    {
        var chat = await repository.Get<ChatEntity>()
            .Where(x => x.Id == id)
            .Include(x => x.ChatMembers)
            .Include(x => x.Messages.OrderBy(x => x.CreatedAt).Take(DefaultPageSize))
            .FirstOrDefaultAsync(cancellationToken);

        if (chat == null)
        {
            return Result<ChatItemViewModel>.Failure(new Error("id", "NotFound"));
        }

        var membersProfile = await Task.WhenAll(chat.ChatMembers.Select(x => profileHttpClient.GetProfileByUserIdAsync(x.ChatMemberId)));

        var result = new ChatItemViewModel(
            chat.Title,
            chat.ChatMembers.Count,
            chat.ChatMembers.Select(x =>
            {
                var profile = membersProfile.FirstOrDefault(m => m.UserId == x.ChatMemberId);
                return new ChatMemberItem(
                                profile?.Name,
                                x.ChatMemberId,
                                profile?.PhotoUrl,
                                profile?.ProfileState
                            );
            }).ToList()
        );
        return result;
    }
}

public sealed record CreateUserChatRequest(string Title, string Message, CreateChatMember Member);
public sealed record CreateGroupChatRequest(string Title, List<CreateChatMember> Members);
public readonly record struct CreateChatResponse(Guid ChatId);
public readonly record struct CreateChatMember(Guid UserId);
public record ChatMemberItem(string? Name, Guid UserId, string? PhotoUrl, ProfileState? ProfileState);
public record ChatItemViewModel(string Title, int MembersCount, List<ChatMemberItem> Members);
public record ChatListItem(Guid Id, string Title, ChatListItemMessage LastMessage);
public record ChatListItemMessage(Guid Id, string Text, DateTimeOffset CreatedAt, bool IsViewed);
public record DetailMessageItem(Guid Id, string Text, DateTimeOffset CreatedAt, bool IsViewed, IEnumerable<BaseFileMetadataEntity> Files);

public record ChatPageViewModel<T>(
    int TotalPageCount,
    int PageSize,
    int Page,
    IReadOnlyList<T> Items
    );