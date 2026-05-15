using MediatR;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogDetailContext;

public sealed record GetBlogDetailContextQuery(string Slug, int Limit = 9) : IRequest<BlogDetailContextDto?>;
