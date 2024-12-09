using Profile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Models.Blog
{
    public class BlogModel
    {
        public Guid Id { get; }
        public string Name { get; }
        public string? Description { get; }
        public DateTimeOffset CreatedAt { get; }
        public string? PhotoUrl { get; }
        public Guid ProfileId { get; }

        public BlogModel(Guid id, string name, string? description, DateTimeOffset createdAt, string? photoUrl, Guid profileId)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            PhotoUrl = photoUrl;
            ProfileId = profileId;
        }
    }

    public static class BlogModelMapper
    {
        public static BlogModel ToBlogModel(this Profile.Domain.Entities.Blog blog)
        {
            return new BlogModel(blog.Id, blog.Name, blog.Description, blog.CreatedAt, blog.PhotoUrl, blog.ProfileId);
        }
    }
}
