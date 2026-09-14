using Infrastructure.Interface;
using Shared.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities;

public class ProfilePictureFile : BaseFileMetadataEntity, IUserEntity, ISoftDelete
{
    public long ProfileId { get; set; }

    [ForeignKey(nameof(ProfileId))]
    public AppProfile AppProfile { get; private set; }

    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }
}
