
using System;

namespace ProjectName.Types
{
    public class ArticleBlogCategory
    {
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public Guid BlogCategoryId { get; set; }
    }
}
