using MediatR;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;

public sealed record GetBlogBySlugQuery(string Slug) : IRequest<BlogDetailDto?>;
