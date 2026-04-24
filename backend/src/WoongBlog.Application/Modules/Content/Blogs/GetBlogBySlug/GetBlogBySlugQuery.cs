using MediatR;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;

public sealed record GetBlogBySlugQuery(string Slug) : IRequest<BlogDetailDto?>;
