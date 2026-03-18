using Shared.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayListService.Domain.Entities;
public sealed class PlayListFile : BaseFileMetadataEntity, IPlayListEntity
{
    public Guid PlaylistId { get; set; }

    [ForeignKey(nameof(PlaylistId))]
    public PlayList? PlayList { get; private set; }
}
