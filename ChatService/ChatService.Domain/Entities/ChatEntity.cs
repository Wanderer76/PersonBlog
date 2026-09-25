using Shared;
using Shared.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatService.Domain.Entities;

public sealed class ChatEntity : IChatEntity, ISoftDelete
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }
    public List<ChatMemberLink> ChatMembers { get; private set; } = [];

    public List<ChatMessage> Messages { get; private set; } = [];

    private ChatEntity()
    {

    }

    private ChatEntity(Guid id, string title, DateTimeOffset createdAt, bool isDelete, DateTimeOffset? deleteDateTime, List<ChatMemberLink> chatMembers)
    {
        Id = id;
        Title = title;
        CreatedAt = createdAt;
        IsDelete = isDelete;
        DeleteDateTime = deleteDateTime;
        ChatMembers = chatMembers;
    }

    public static Result<ChatEntity> Create(Guid id, string title, DateTimeOffset createdAt, IEnumerable<ChatMemberLink> chatMembers)
    {
        if (chatMembers == null || chatMembers?.Any() == false)
        {
            return Result<ChatEntity>.Failure(new Shared.Utils.Error("chatMembers", "Невозможно создать чат без участников"));
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<ChatEntity>.Failure(new Shared.Utils.Error("title", "Название не может быть пустым"));

        }
        return new ChatEntity(id, title, createdAt, false, null, chatMembers!.ToList());
    }
}

public class ChatMemberLink : IChatEntity
{
    public Guid ChatId { get; private set; }
    public Guid ChatMemberId { get; private set; }
    public string MemberName { get; private set; }
    public bool IsCreator { get; private set; }

    [ForeignKey(nameof(ChatId))]
    public ChatEntity ChatEntity { get; private set; } = null!;

    private ChatMemberLink()
    {

    }

    public ChatMemberLink(Guid chatId, Guid chatMemberId, bool isCreator, string memberName)
    {
        ChatId = chatId;
        ChatMemberId = chatMemberId;
        IsCreator = isCreator;
        MemberName = memberName;
    }
}

public class ChatMessage : IChatEntity, ISoftDelete
{
    public Guid Id { get; private set; }
    public Guid CreatorUserId { get; private set; }
    public string? Text { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ChangedAt { get; private set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }
    public List<MessageFile> MessageFiles { get; private set; } = [];
    public List<MessageReader> MessageReader { get; private set; } = [];
}

public class MessageReader : IChatEntity
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    [ForeignKey(nameof(MessageId))]
    public ChatMessage Message { get; private set; }

    public MessageReader(Guid messageId, Guid userId, DateTimeOffset createdAt)
    {
        MessageId = messageId;
        UserId = userId;
        CreatedAt = createdAt;
    }
}

public class MessageFile : BaseFileMetadataEntity, IChatEntity, ISoftDelete
{
    public Guid MessageId { get; private set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }

    [ForeignKey(nameof(MessageId))]
    public ChatMessage Message { get; private set; } = null!;
}