using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using Blog.Domain.Services.Models.Category;
using Shared.Models;

namespace Blog.API.Models;

public record CreatePostModelViewModel(
    IEnumerable<SubscriptionLevelModel> SubscriptionLevels, 
    IEnumerable<SelectItem<PostVisibility>> Visibility, 
    IEnumerable<CategoryModel> CategoryList);
