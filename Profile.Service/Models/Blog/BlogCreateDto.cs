using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Models.Blog
{
    public class BlogCreateDto
    {
        public Guid ProfileId {  get; set; }
        public required string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }

        public BlogCreateDto(Guid profileId, string title, string description, string photoUrl)
        {
            ProfileId = profileId;
            Title = title;
            Description = description;
            PhotoUrl = photoUrl;
        }
    }
}
