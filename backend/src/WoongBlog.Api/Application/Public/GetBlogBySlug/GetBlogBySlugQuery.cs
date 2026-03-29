using MediatR;

namespace WoongBlog.Api.Application.Public.GetBlogBySlug;

public sealed record GetBlogBySlugQuery(string Slug) : IRequest<BlogDetailDto?>;
